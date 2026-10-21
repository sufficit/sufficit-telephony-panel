using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>
/// Encrypted on-disk copy of panel sessions so a restart or deploy does not sign everyone out.
/// Each session is one file named by its random handle, protected with the persisted DataProtection
/// key ring (owner-only 0600). Bounded by count and by the ticket's own expiry; permissions are still
/// re-read from Identity at most every 60 seconds, so revocation is unaffected.
/// </summary>
public sealed partial class SessionFileStore
{
    private const string Purpose = "panel-session-ticket-v1";
    private readonly string directory;
    private readonly IDataProtector protector;
    private readonly TimeProvider clock;
    private readonly int capacity;

    public SessionFileStore(string directory, IDataProtectionProvider protection, TimeProvider clock, int capacity = 200)
    {
        this.directory = directory;
        this.clock = clock;
        this.capacity = capacity;
        protector = protection.CreateProtector(Purpose);
        var info = Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows()) info.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        Prune();
    }

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex Handle();

    // The handle comes from the authenticated cookie, but never trust it as a path component.
    private string? PathFor(string key) => Handle().IsMatch(key) ? Path.Combine(directory, key + ".session") : null;

    public void Save(string key, AuthenticationTicket ticket)
    {
        if (PathFor(key) is not { } path) return;
        var bytes = protector.Protect(TicketSerializer.Default.Serialize(ticket));
        var temporary = path + ".tmp";
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var file = new FileStream(temporary, options)) file.Write(bytes);
        File.Move(temporary, path, true);
        Prune();
    }

    public AuthenticationTicket? Load(string key)
    {
        if (PathFor(key) is not { } path || !File.Exists(path)) return null;
        try
        {
            if (new FileInfo(path).Length > 256 * 1024) { Remove(key); return null; }
            var ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(File.ReadAllBytes(path)));
            if (ticket is null || Expired(ticket)) { Remove(key); return null; }
            return ticket;
        }
        catch (Exception e) when (e is System.Security.Cryptography.CryptographicException or IOException or FormatException or ArgumentException)
        { Remove(key); return null; }
    }

    public void Remove(string key)
    {
        if (PathFor(key) is { } path) try { File.Delete(path); } catch (IOException) { }
    }

    private bool Expired(AuthenticationTicket ticket) =>
        ticket.Properties.ExpiresUtc is { } expires && expires <= clock.GetUtcNow();

    /// <summary>Drops leftovers and keeps only the newest sessions within capacity.</summary>
    private void Prune()
    {
        var files = new DirectoryInfo(directory).GetFiles("*.session").OrderByDescending(f => f.LastWriteTimeUtc).ToArray();
        // Absolute upper bound for any session (cookie lifetime is 8 hours); also removes stale temporaries.
        var stale = clock.GetUtcNow().UtcDateTime - TimeSpan.FromHours(9);
        foreach (var file in files.Skip(capacity).Concat(files.Where(f => f.LastWriteTimeUtc < stale)).Distinct())
            try { file.Delete(); } catch (IOException) { }
        foreach (var leftover in new DirectoryInfo(directory).GetFiles("*.tmp"))
            try { if (leftover.LastWriteTimeUtc < clock.GetUtcNow().UtcDateTime.AddMinutes(-5)) leftover.Delete(); } catch (IOException) { }
    }
}
