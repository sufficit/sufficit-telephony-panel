using Microsoft.AspNetCore.WebUtilities;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>URL preferences are bounded presentation state, never authorization.</summary>
public sealed record BoardViewQuery
{
    public Guid? Company { get; init; }
    public string Text { get; init; } = "";
    public string Node { get; init; } = "all";
    public string State { get; init; } = "all";
    public string[] TextKinds { get; init; } = ["peers", "trunks", "queues"];
    public bool Channels { get; init; }
    public string Tab { get; init; } = "board";
    public int Page { get; init; }
    public static readonly string[] Kinds = ["peers", "trunks", "queues"];

    public static BoardViewQuery Parse(string uri)
    {
        var query = QueryHelpers.ParseQuery(new Uri(uri).Query);
        string Get(string key) => query.TryGetValue(key, out var values) && values.Count == 1 ? values[0] ?? "" : "";
        var text = Get("q");
        var node = Get("node");
        var state = Get("state");
        var scope = Get("text");
        var tab = Get("tab");
        var kinds = scope.Split(',').Where(Kinds.Contains).Distinct().ToArray();
        return new()
        {
            Company = Guid.TryParse(Get("company"), out var id) && id != Guid.Empty ? id : null,
            Text = new string(text.Where(c => !char.IsControl(c)).Take(100).ToArray()),
            Node = node.Length is > 0 and <= 128 && !node.Any(char.IsControl) ? node : "all",
            State = new[] { "online", "offline", "registered", "unregistered", "busy", "unknown" }.Contains(state) ? state : "all",
            TextKinds = scope == "none" ? [] : kinds.Length > 0 ? kinds : Kinds.ToArray(),
            Channels = Get("channels") == "true",
            Tab = new[] { "extensions", "calls", "channels", "trunks", "queues", "settings" }.Contains(tab) ? tab : "board",
            Page = int.TryParse(Get("page"), out var page) ? Math.Clamp(page, 0, 1000) : 0
        };
    }

    public Dictionary<string, object?> Parameters() => new()
    {
        ["company"] = Company?.ToString("N"), ["q"] = Text.Length > 0 ? Text : null,
        ["node"] = Node == "all" ? null : Node, ["state"] = State == "all" ? null : State,
        ["text"] = TextKinds.Length == 3 ? null : TextKinds.Length == 0 ? "none" : string.Join(',', Kinds.Where(TextKinds.Contains)),
        ["channels"] = Channels ? "true" : null, ["tab"] = Tab == "board" ? null : Tab,
        ["page"] = Page > 0 ? Page : null
    };
}
