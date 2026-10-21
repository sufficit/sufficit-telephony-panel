using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Sufficit.Telephony.Panel.Security;

static class SessionStoreChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "panel-sessions-" + Guid.NewGuid().ToString("N"));
        var keys = new DirectoryInfo(Path.Combine(directory, "keys"));
        // Same persisted key ring across "restarts", like production.
        IDataProtectionProvider Protection() => DataProtectionProvider.Create(keys, b => b.SetApplicationName("panel-tests"));
        var clock = new FakeClock();
        try
        {
            AuthenticationTicket Ticket(DateTimeOffset expires)
            {
                var properties = new AuthenticationProperties { ExpiresUtc = expires };
                properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = "secret-access-token" },
                    new AuthenticationToken { Name = "expires_at", Value = expires.ToString("o") }]);
                return new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice")], "test")), properties, "panel");
            }
            var sessionsDir = Path.Combine(directory, "sessions");
            using (var first = new PanelSessions(new FakeHttp(), clock, new SessionFileStore(sessionsDir, Protection(), clock)))
            {
                var key = await first.StoreAsync(Ticket(clock.GetUtcNow().AddHours(8)));
                check(File.Exists(Path.Combine(sessionsDir, key + ".session")), "Session is persisted on sign-in");
                check(!File.ReadAllText(Path.Combine(sessionsDir, key + ".session")).Contains("secret-access-token"), "Persisted session is encrypted");
                if (!OperatingSystem.IsWindows())
                    check(File.GetUnixFileMode(Path.Combine(sessionsDir, key + ".session")) == (UnixFileMode.UserRead | UnixFileMode.UserWrite), "Session file is owner-only");
                using var restarted = new PanelSessions(new FakeHttp(), clock, new SessionFileStore(sessionsDir, Protection(), clock));
                var restored = await restarted.RetrieveAsync(key);
                check(restored?.Principal.FindFirst("sub")?.Value == "alice" && restored.Properties.GetTokenValue("access_token") == "secret-access-token",
                    "Session survives a restart with the same key ring");
                await restarted.RemoveAsync(key);
                check(!File.Exists(Path.Combine(sessionsDir, key + ".session")), "Sign-out removes the persisted session");
                check(await new PanelSessions(new FakeHttp(), clock, new SessionFileStore(sessionsDir, Protection(), clock)).RetrieveAsync(key) is null, "Removed session never comes back");
            }
            var store = new SessionFileStore(sessionsDir, Protection(), clock);
            var handle = Guid.NewGuid().ToString("N");
            store.Save(handle, Ticket(clock.GetUtcNow().AddHours(1)));
            File.WriteAllBytes(Path.Combine(sessionsDir, handle + ".session"), [1, 2, 3]);
            check(store.Load(handle) is null && !File.Exists(Path.Combine(sessionsDir, handle + ".session")), "Tampered session is discarded");
            store.Save(handle, Ticket(clock.GetUtcNow().AddMinutes(5)));
            clock.Now += TimeSpan.FromMinutes(6);
            check(store.Load(handle) is null, "Expired session is not restored");
            foreach (var unsafeKey in new[] { "../keys/key", "..", "ABCDEF", "", Guid.NewGuid().ToString("D") })
            {
                store.Save(unsafeKey, Ticket(clock.GetUtcNow().AddHours(1)));
                check(store.Load(unsafeKey) is null, "Unsafe session handle never becomes a path: " + unsafeKey);
            }
            check(Directory.GetFiles(sessionsDir).All(f => Path.GetFileName(f).Length == 40), "Only handle-named session files exist");
            var small = new SessionFileStore(Path.Combine(directory, "small"), Protection(), clock, capacity: 3);
            for (var i = 0; i < 5; i++) small.Save(Guid.NewGuid().ToString("N"), Ticket(clock.GetUtcNow().AddHours(1)));
            check(Directory.GetFiles(Path.Combine(directory, "small"), "*.session").Length <= 3, "Session files are bounded by capacity");
            check(await new PanelSessions(new FakeHttp(), clock, new SessionFileStore(sessionsDir, DataProtectionProvider.Create(
                new DirectoryInfo(Path.Combine(directory, "other-keys"))), clock)).RetrieveAsync(handle) is null, "Another key ring cannot read sessions");
        }
        finally { Directory.Delete(directory, true); }
    }
}
