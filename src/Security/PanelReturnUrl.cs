namespace Sufficit.Telephony.Panel.Security;

public static class PanelReturnUrl
{
    public static string Validate(string value, string pathBase)
    {
        if (value.Length is < 1 or > 2048 || !value.StartsWith('/') || value.StartsWith("//") || value.Contains('\\') || value.Any(char.IsControl))
            return pathBase + "/";
        var path = value.Split('?', '#')[0];
        return path == pathBase + "/" || path == pathBase + "/board" || path == pathBase + "/queues" || path == pathBase + "/mesa" ? value : pathBase + "/";
    }
}
