using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Sufficit.Identity.Authorization;
using Sufficit.Telephony.Panel.Security;

static class SupportChatChecks
{
    public static void Run(Action<bool, string> check)
    {
        var secretFile = Path.GetTempFileName();
        File.WriteAllText(secretFile, "test-secret\n");
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Support:WebsiteToken"] = "public-widget-token", ["Support:HmacSecretFile"] = secretFile }).Build();
            var support = new SupportChat(configuration, new EphemeralDataProtectionProvider());
            check(support.Enabled, "Support chat enabled with token and HMAC secret");
            check(!new SupportChat(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Support:WebsiteToken"] = "x" }).Build(),
                new EphemeralDataProtectionProvider()).Enabled, "Support chat stays off without the HMAC secret");
            // Same value as Ruby OpenSSL::HMAC.hexdigest('sha256', secret, identifier) used by Chatwoot.
            check(support.IdentifierHash("ee277377-f229-4552-b97a-c8f6e73fd36c") == "8559e8e5e30c7af8ae28f84894ccf2efa7428b00a9f483f8e6cb312afe2aa68f",
                "Identifier hash matches Chatwoot HMAC-SHA256 hex");

            var principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("sub", "ee277377-f229-4552-b97a-c8f6e73fd36c"), new Claim("name", "Teste"), new Claim("email", "teste@example.com"),
                new Claim("entitlements", "telephonyclient:026246a851a7485991e0b06e1bebb045"), new Claim("entitlements", "aiuser:026246a8-51a7-4859-91e0-b06e1bebb045"),
                new Claim("entitlements", "global:00000000000000000000000000000000")], "test"));
            var denied = SupportChat.Denied(principal, CurrentAuthorization.None);
            check(denied.Reason == "login-denied" && denied.Context == "026246a851a7485991e0b06e1bebb045", "Denied request lists the known company context once, never the global zero context");
            check(denied.Request == "telephony.panel:026246a851a7485991e0b06e1bebb045", "Denied request asks for the panel grant in preset format");
            check(SupportChat.Describe(new CurrentAuthorization(false, [Guid.Parse("026246a851a7485991e0b06e1bebb045")], [])).Contains("telephony.panel=[026246a851a7485991e0b06e1bebb045]"),
                "Permission summary lists current panel contexts");

            var cookie = support.ProtectDenied(denied);
            check(support.UnprotectDenied(cookie) == denied, "Denied context round-trips through the protected cookie");
            check(support.UnprotectDenied(cookie + "x") is null && support.UnprotectDenied("tampered") is null, "Tampered support cookie is rejected");
            check(new SupportChat(configuration, new EphemeralDataProtectionProvider()).UnprotectDenied(cookie) is null, "Support cookie is bound to the server key ring");

            var json = JsonSerializer.Serialize(support.Payload(denied, "pt-BR", true));
            check(!json.Contains("test-secret") && json.Contains("8559e8e5e30c7af8") && json.Contains("\"locale\":\"pt_BR\""),
                "Browser payload carries the per-user hash, never the HMAC secret");
            check(json.Contains("\"panel.reason\":\"login-denied\"") && json.Contains("\"panel.request\"") && !json.Contains("painel_"),
                "Support attributes use the English panel.* object keys");
            check(JsonSerializer.Serialize(support.Payload(denied with { Subject = "" }, "en", false)).Contains("\"user\":null"),
                "Without a verified subject the chat stays anonymous");
        }
        finally { File.Delete(secretFile); }
    }
}
