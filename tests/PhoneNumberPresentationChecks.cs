using Sufficit.Telephony.Panel.Operations;

internal static class PhoneNumberPresentationChecks
{
    public static void Run(Action<bool, string> check)
    {
        check(PhoneNumberPresentation.Normalize("5508000000697") == "558000000697", "Malformed Brazilian tollfree number is normalized");
        check(PhoneNumberPresentation.Normalize("+5508000000697") == "+558000000697", "Explicit international plus is preserved");
        foreach (var value in new[] { "", "6007", "08000000697", "558000000697", "+558000000697", "5511999999999", "550800000697", "55080000000697", "550800000069x", "550800000069٧", " 5508000000697", "SIP/5508000000697", "Atendimento" })
            check(PhoneNumberPresentation.Normalize(value) == value, "Other identifiers remain unchanged: " + value);
        check(PhoneNumberPresentation.Normalize(null) is null, "Missing identifiers remain missing");
        var now = DateTimeOffset.UtcNow;
        var row = new ObservedResource("test", "google-voip", "call", "SIP/TIP-test", "Up", "5508000000697", "+5508000000697", null, now, now, new Dictionary<string, string>());
        check(PhoneNumberPresentation.Normalize(row.Caller) == "558000000697" && PhoneNumberPresentation.Normalize(row.Destination) == "+558000000697", "Both channel ends can be presented normalized");
        check(row.Caller == "5508000000697" && row.Destination == "+5508000000697", "Original observation remains available for diagnosis");
    }
}
