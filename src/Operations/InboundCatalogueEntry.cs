namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Non-channel identity: inventory must never match a shared SIP carrier or imply live state.</summary>
public static class InboundCatalogueEntry
{
    public static string? State(IEnumerable<string> keys)
    {
        var values = keys.Take(2).ToArray();
        if (values.Length != 1) return null;
        var parts = values[0].Split(':');
        if (parts.Length != 5 || parts[0] != "entry" || parts[1] is not ("did" or "whatsapp") ||
            !Guid.TryParseExact(parts[2], "N", out var context) || context == Guid.Empty ||
            parts[3].Length != (parts[1] == "did" ? 32 : 64) || !parts[3].All(Uri.IsHexDigit) ||
            parts[4] is not ("configured" or "disabled" or "expired")) return null;
        return parts[4];
    }
    public static string Label(string state) => state switch
    {
        "disabled" => "Entrada desabilitada",
        "expired" => "DID vencido",
        _ => "Entrada cadastrada · sem estado em tempo real"
    };
}
