using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Presentation only; received identifiers remain unchanged in observations.</summary>
public static partial class PhoneNumberPresentation
{
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Normalize(string? value)
    {
        if (value is null) return null;
        var offset = value.StartsWith('+') ? 1 : 0;
        var digits = value.AsSpan(offset);
        foreach (var digit in digits)
            if (digit < '0' || digit > '9') return value;
        if (digits.Length == 13 && digits.StartsWith("550800", StringComparison.Ordinal))
            value = value.Remove(offset + 2, 1);
        if (value.StartsWith('+')) return value;
        if (BrazilianInternationalNumber().IsMatch(value)) return "+" + value;
        if (value.StartsWith('0') && BrazilianNationalNumber().IsMatch(value.AsSpan(1)))
            return "+55" + value[1..];
        return BrazilianNationalNumber().IsMatch(value) ? "+55" + value : value;
    }

    // Recognize explicit Brazilian country code + allocated DDD shapes and subscriber lengths.
    // This is presentation recognition, not proof of allocation, reachability or carrier origin.
    // National DDD + subscriber numbers use Brazil as the presentation country.
    // Local numbers without DDD and explicit foreign numbers remain unchanged.
    private const string NationalPattern = @"(?:1[1-9]|2[12478]|3[1-578]|4[1-9]|5[1345]|6[1-9]|7[134579]|8[1-9]|9[1-9])(?:[2-5][0-9]{7}|9[0-9]{8})";

    [GeneratedRegex(@"\A" + NationalPattern + @"\z", RegexOptions.CultureInvariant)]
    private static partial Regex BrazilianNationalNumber();

    [GeneratedRegex(@"\A55(?:800[0-9]{7}|" + NationalPattern + @")\z", RegexOptions.CultureInvariant)]
    private static partial Regex BrazilianInternationalNumber();
}
