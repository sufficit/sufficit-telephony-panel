using System.Diagnostics.CodeAnalysis;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Presentation only; received identifiers remain unchanged in observations.</summary>
public static class PhoneNumberPresentation
{
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Normalize(string? value)
    {
        if (value is null) return null;
        var offset = value.StartsWith('+') ? 1 : 0;
        var digits = value.AsSpan(offset);
        if (digits.Length != 13 || !digits.StartsWith("550800", StringComparison.Ordinal)) return value;
        foreach (var digit in digits)
            if (digit < '0' || digit > '9') return value;
        return value.Remove(offset + 2, 1);
    }
}
