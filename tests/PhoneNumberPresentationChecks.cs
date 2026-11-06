using Sufficit.Telephony.Panel.Operations;

internal static class PhoneNumberPresentationChecks
{
    public static void Run(Action<bool, string> check)
    {
        check(PhoneNumberPresentation.Normalize("5508000000697") == "+558000000697", "Malformed Brazilian tollfree number is normalized");
        check(PhoneNumberPresentation.Normalize("+5508000000697") == "+558000000697", "Explicit international plus is preserved");
        foreach (var value in new[] { "", "6007", "08000000697", "+558000000697", "550800000697", "55080000000697", "550800000069x", "550800000069٧", " 5508000000697", "SIP/5508000000697", "Atendimento" })
            check(PhoneNumberPresentation.Normalize(value) == value, "Other identifiers remain unchanged: " + value);
        foreach (var number in new[] { "558000000697", "5511999999999", "552140627711", "5591981344999", "555133334444" })
        {
            check(PhoneNumberPresentation.Normalize(number) == "+" + number, "Recognized Brazilian international number gets plus: " + number);
            check(PhoneNumberPresentation.Normalize("+" + number) == "+" + number, "Plus is never duplicated: " + number);
        }
        foreach (var value in new[] { "0000006007", "9198431765", "55116007", "5523999999999", "551199999999", "551169999999", "12025550123", "+12025550123", "anonymous", "551199999999٩", "5511999999999\n" })
            check(PhoneNumberPresentation.Normalize(value) == value, "Ambiguous/internal identifier or explicit foreign number preserved: " + value);
        foreach (var number in new[] { "21960185389", "21964312895", "11999999999", "2126551085", "5133334444", "55999999999" })
        {
            check(PhoneNumberPresentation.Normalize(number) == "+55" + number, "Brazilian national source gets country code: " + number);
            check(PhoneNumberPresentation.Normalize(PhoneNumberPresentation.Normalize(number)) == "+55" + number, "National normalization is idempotent: " + number);
        }
        foreach (var value in new[] { "8001234567", "988242905", "000066014", "005626200", "061992053002", "23960185389", "21660185389", "2196018538", "+21960185389", "2196018538٩", "21960185389\n" })
            check(PhoneNumberPresentation.Normalize(value) == value, "Local, explicit international or invalid source preserved: " + value);
        check(PhoneNumberPresentation.Normalize("552126551085") == "+552126551085", "Screenshot destination remains normalized");
        check(PhoneNumberPresentation.Normalize(null) is null, "Missing identifiers remain missing");
        var now = DateTimeOffset.UtcNow;
        var row = new ObservedResource("test", "google-voip", "call", "SIP/TIP-test", "Up", "5508000000697", "+5508000000697", null, now, now, new Dictionary<string, string>());
        check(PhoneNumberPresentation.Normalize(row.Caller) == "+558000000697" && PhoneNumberPresentation.Normalize(row.Destination) == "+558000000697", "Both channel ends can be presented normalized");
        var nationalRow = row with { Caller = "21960185389", Destination = "552126551085" };
        check(PhoneNumberPresentation.Normalize(nationalRow.Caller) == "+5521960185389" && PhoneNumberPresentation.Normalize(nationalRow.Destination) == "+552126551085", "Screenshot source and destination normalize together");
        check(nationalRow.Caller == "21960185389" && nationalRow.Destination == "552126551085", "Screenshot originals remain available for diagnosis");
        check(row.Caller == "5508000000697" && row.Destination == "+5508000000697", "Original observation remains available for diagnosis");
    }
}
