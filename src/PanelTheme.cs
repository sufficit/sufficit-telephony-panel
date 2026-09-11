using Sufficit.Blazor.UI.Themes;
namespace Sufficit.Telephony.Panel;
public sealed class PanelTheme(bool dark) : ISUITheme
{
    public bool IsDark => dark;
    public SUITypography Typography { get; } = new();
    public SUILayout Layout { get; } = new();
    public SUIPalette Palette { get; } = new()
    {
        Primary = dark ? "#ff9c60" : "#a94210", PrimaryContrast = dark ? "#231e1b" : "#ffffff",
        PrimaryAction = dark ? "#ff9c60" : "#a94210", PrimaryActionContrast = dark ? "#231e1b" : "#ffffff",
        Surface = dark ? "#1f2226" : "#ffffff", Surface2 = dark ? "#272b30" : "#f5f6f7",
        Surface3 = dark ? "#353b42" : "#e7eaed", TextPrimary = dark ? "#f2f4f5" : "#20252b",
        TextSecondary = dark ? "#bdc7d0" : "#515e6a", Border = dark ? "#434b55" : "#d3dae1",
        BorderStrong = dark ? "#6a7885" : "#8998a5", Success = dark ? "#80d5a4" : "#146139",
        Warning = dark ? "#f6cf76" : "#805400", Error = dark ? "#ffa3a3" : "#b02020",
        Info = dark ? "#a3d5f5" : "#215a80"
    };
}
