using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Sufficit.Identity.Authorization;
using Sufficit.Telephony.EventsPanel;
using Sufficit.Telephony.Monitor;
using Sufficit.Telephony.Panel.Localization;
using Sufficit.Telephony.Panel.Monitoring;
using Sufficit.Telephony.Panel.Operations;
using Sufficit.Telephony.Panel.Security;

namespace Sufficit.Telephony.Panel.Pages;

// State, URL sync, data loading and supervision logic. Markup and render fragments live in LiveOperations.razor.
public partial class LiveOperations
{
    [Parameter] public bool BoardPage { get; set; }
    // Start in the requested area so the board is never mounted and torn down before the URL is applied.
    protected override void OnInitialized()
    {
        if (BoardPage) return;
        var tab = BoardViewQuery.Parse(Navigation.Uri).Tab;
        Tab = tab == "trunks" ? "extensions" : tab;
    }
    // The operator board always takes the whole viewport below the header; it is never one tab among lists.
    private bool Fill => BoardPage || Tab == "board";
    private bool fitted;
    private string[] TextKinds = BoardViewQuery.Kinds.ToArray();
    private bool UrlReady;
    private bool urlPending, replaceUrl;
    private string lastWrittenUrl = "";
    private string BoardUrl => Navigation.GetUriWithQueryParameters(new Uri(new Uri(Navigation.BaseUri), "board").ToString(), new Dictionary<string, object?>(CurrentQuery.Parameters()) { ["preview"] = Demo ? "true" : null, ["tab"] = null });
    private BoardViewQuery CurrentQuery => new() { ContextId = Context, Text = Filter, Node = Node, State = StatusFilter, TextKinds = TextKinds, Channels = OnlyObservedChannels, Tab = Tab, Page = Page };
    private void WriteUrl(bool replace = false)
    {
        if (!UrlReady) return;
        urlPending = true; replaceUrl = replace;
    }
    private void FlushUrl()
    {
        if (!urlPending || !UrlReady) return;
        urlPending = false;
        var uri = Navigation.GetUriWithQueryParameters(CurrentQuery.Parameters());
        if (uri == Navigation.Uri) return;
        lastWrittenUrl = uri;
        Navigation.NavigateTo(uri, replace: replaceUrl);
    }
    private async void LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        if (!UrlReady || args.Location == lastWrittenUrl) { lastWrittenUrl = ""; return; }
        try { await InvokeAsync(async () => { await ApplyUrl(); if (UrlReady) StateHasChanged(); }); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (!UrlReady) { }
    }
    private async Task ApplyUrl()
    {
        var query = BoardViewQuery.Parse(Navigation.Uri);
        if (query.ContextId != Context) await SelectClient(query.ContextId, query.ContextId?.ToString("N") ?? Text["Entire exchange"], false);
        Filter = query.Text; Node = query.Node; StatusFilter = query.State; TextKinds = query.TextKinds;
        OnlyObservedChannels = query.Channels; Page = query.Page;
        Tab = BoardPage ? "board" : query.Tab;
        if (Tab == "trunks" && !Context.HasValue) Tab = "extensions";
        SettingsOpened |= Tab == "settings";
        SelectedTileId = null; Pending = null;
        WriteUrl(true);
    }
    private bool FullscreenError;
    private async Task ToggleFullscreen() { FullscreenError = !await JS.InvokeAsync<bool>("telephonyPanel.fullscreen", "board-focus"); }
    private void SetTextKind(string kind, bool value)
    {
        TextKinds = value ? TextKinds.Append(kind).Distinct().ToArray() : TextKinds.Where(k => k != kind).ToArray();
        Page = 0; WriteUrl();
    }
    private void SetBoardPage(int value) { Page = value; WriteUrl(); }
    private async Task ResetFilters() { Filter = ""; Node = "all"; StatusFilter = "all"; OnlyObservedChannels = false; TextKinds = BoardViewQuery.Kinds.ToArray(); Page = 0; if (Context.HasValue) await ClearCompany(); WriteUrl(); }
    private bool Demo => Preview && Environment.IsDevelopment();
    private Guid? Context;
    private CurrentAuthorization Permissions = CurrentAuthorization.None;
    private string ClientName = "", SearchText = "", Filter = "", Node = "all", Tab = "board";
    private string? SelectedTileId;
    private OperatorTile[] demoTiles = [];
    private EventsPanelCardInfo[]? boardCards;
    private AuthorizedObservation? boardObservation;
    private OperatorTile[] cachedTiles = [];
    private BoardConfiguration BoardConfiguration = new();
    private OperatorTile[]? inventoryTiles;
    private bool InventoryBusy, ConfirmClearInventory;
    private string? InventoryError, InventoryMessage;
    private string? BoardError;
    private bool SettingsOpened;
    private OperatorTile[]? classifiedSource;
    private string? classifiedRevision;
    private OperatorTile[] classifiedTiles = [];
    private ObservedResource[]? classifiedRows;
    private OperatorTile[] BoardTiles
    {
        get
        {
            var source = BoardConfiguration.RetainSeenResources ? inventoryTiles ?? RawBoardTiles : RawBoardTiles;
            if (!ReferenceEquals(source, classifiedSource) || classifiedRevision != BoardConfiguration.Revision || !ReferenceEquals(classifiedRows, Rows))
            {
                classifiedSource = source; classifiedRevision = BoardConfiguration.Revision;
                classifiedRows = Rows;
                classifiedTiles = BoardCallFlow.Attach(BoardRuleEngine.Apply(source, BoardConfiguration), Rows);
            }
            return classifiedTiles;
        }
    }
    private void SetBoardConfiguration(BoardConfiguration value) { BoardConfiguration = value; BoardError = null; }
    private async Task SetRetention(bool value)
    {
        if (InventoryBusy) return;
        InventoryBusy = true; InventoryError = null; InventoryMessage = null;
        try
        {
            var draft = BoardConfiguration with { RetainSeenResources = value };
            SetBoardConfiguration(Demo ? draft with { Revision = Guid.CreateVersion7().ToString("N") }
                : await BoardAccess.Save(draft, lifetime.Token));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        { InventoryFailure = Diagnostics.Report(e, "retention-save"); InventoryError = Text["Could not save retention. Reload configuration and try again. "] + Text["Check your session and panel storage."]; }
        finally { InventoryBusy = false; }
    }
    private async Task RefreshInventory()
    {
        if (Demo) return;
        var version = selectionVersion;
        try
        {
            var updated = await InventoryAccess.Observe(Context, RawBoardTiles, Cards, true, lifetime.Token);
            if (version == selectionVersion) { inventoryTiles = updated; InventoryError = null; }
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or System.Text.Json.JsonException)
        {
            if (version == selectionVersion) { InventoryFailure = Diagnostics.Report(e, "inventory-read"); inventoryTiles = null; InventoryError = Text["Could not retain resources. The board shows only current observations; inventory was not deleted. Check panel storage."]; }
        }
    }
    private async Task ClearInventory()
    {
        if (InventoryBusy) return;
        InventoryBusy = true; InventoryError = null;
        try
        {
            if (!Demo) await InventoryAccess.Clear(Context, lifetime.Token);
            inventoryTiles = null; ConfirmClearInventory = false;
            await RefreshInventory();
            InventoryMessage = Text["Remembered resources cleared. Current resources remain and new events will be remembered."];
        }
        catch (Exception e) when (e is not OperationCanceledException)
        { InventoryFailure = Diagnostics.Report(e, "inventory-clear"); InventoryError = Text["Cleanup did not complete. Check your session and panel storage, then retry."]; }
        finally { InventoryBusy = false; }
    }
    private EventCallback SimulateCallback => Demo ? EventCallback.Factory.Create(this, SimulateMissingEvents) : default;
    private void SimulateMissingEvents()
    {
        if (!Demo) return;
        inventoryTiles = (inventoryTiles ?? demoTiles).Select(t => t with { LastSeen = t.Updated ?? DateTimeOffset.UtcNow, Rows = [] }).ToArray();
        demoTiles = []; Rows = []; Observation = new(); Cards = [];
    }
    private async Task ReadBoardConfiguration()
    {
        if (Demo) return;
        try { SetBoardConfiguration(await BoardAccess.Read(lifetime.Token)); }
        catch (Exception e) when (e is not OperationCanceledException)
        { BoardFailure = Diagnostics.Report(e, "board-rules-read"); BoardError = Text["Could not load board rules. This session's last configuration was kept; use Reload configuration to try again."]; }
    }
    private OperatorTile[] RawBoardTiles
    {
        get
        {
            if (Demo) return demoTiles;
            if (!ReferenceEquals(boardCards, Cards) || !ReferenceEquals(boardObservation, Observation))
            {
                boardCards = Cards; boardObservation = Observation;
                cachedTiles = OperatorTile.Create(Cards, Observation.ByCard, !Permissions.Manager && !Context.HasValue);
            }
            return cachedTiles;
        }
    }
    private OperatorTile[] VisibleTiles => BoardTiles
        .Where(t => (!OnlyObservedChannels || t.HasObservedActivity) && (Node == "all" || t.Node == Node) && (StatusFilter == "all" || StatusFilter == "unknown" && (t.Rows.Length == 0 || t.Queue is { State: "stale" }) || StatusFilter == "busy" && t.HasObservedActivity || t.Rows.Any(MatchStatus))
            && (!TextKinds.Contains(t.Kind == EventsPanelCardKind.TRUNK ? "trunks" : t.Kind == EventsPanelCardKind.QUEUE ? "queues" : "peers") || MatchText(t.Title + " " + t.Node + " " + string.Join(" ", t.Keys) + " " + string.Join(" ", t.Rows.Select(r => r.Key + " " + r.Caller + " " + r.Destination)))))
        .ToArray();
    private OperatorTile? SelectedTile => VisibleTiles.FirstOrDefault(t => t.Id == SelectedTileId);
    private void SelectTile(OperatorTile tile) { SelectedTileId = tile.Id; Pending = null; }
    private void CloseTile() { SelectedTileId = null; Pending = null; }
    private bool Busy, Searched, ActionBusy, FocusConfirmation;
    private string? Error, SearchError, ActionMessage;
    private string StatusFilter = "all";
    private bool OnlyObservedChannels;
    private void ToggleObservedChannels() { OnlyObservedChannels = !OnlyObservedChannels; Page = 0; SelectedTileId = null; Pending = null; WriteUrl(); }
    private (Guid Id, string Title)[] Clients = [];
    private (Guid Id, string Title)? Supervisor;
    private EventsPanelCardInfo[] Cards = [];
    private EventsPanelCardInfo[] PortalCards = [];
    private bool DiagnosticPreview => Demo && Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query).ContainsKey("diagnosticpreview");
    private string? QueueError;
    private QueueBoardSample[] QueueSamples = [];
    private PanelDiagnostic? Failure, SearchFailure, ActionFailure, BoardFailure, InventoryFailure, SupervisorFailure;
    private static string PhaseLabel(string phase) => phase switch
    {
        "greeting" => "Queue greeting", "announcing" => "Queue announcement", "offering" => "Calling an agent",
        "answered" => "Answered in queue", "waiting" => "Waiting in queue", _ => "Observed call flow"
    };
    private ObservedResource[] rows = [];
    private ObservedCallGroup[] callGroups = [];
    private ObservedResource[] Rows
    {
        get => rows;
        set { rows = value; callGroups = ObservedCallGroup.Create(value); }
    }
    private OperationsConnection? Connection;
    private AuthorizedObservation Observation = new();
    private int selectionVersion;
    private ObservedResource? Pending;
    private TelephonyMonitorActionMode Mode;
    private ElementReference Confirmation;
    private readonly CancellationTokenSource lifetime = new();
    private Task? loop;
    private DateTimeOffset AuthorizationChecked;
    private const int PageSize = 30;
    private int Page;
    private (string Key, string Label)[] Tabs => [("board", Text["Operation board"]), ("extensions", Text["Extensions"]), ("calls", Text["Calls"]), ("channels", Text["Channels"]), ("trunks", Text["Trunks"]), ("queues", Text["Call queues"]), ("settings", Text["Configure board"]), ("events", Text["Events"])];
    private ObservedCallGroup[] CallGroups => callGroups;
    private ObservedCallGroup[] VisibleGroups => ObservedCallGroup.Filter(CallGroups, MatchesChannel);
    private bool Connected => Demo || Connection?.Connected == true;
    private DateTimeOffset? Since => Demo ? DateTimeOffset.UtcNow.AddMinutes(-8) : Connection?.ConnectedAt;
    private DateTimeOffset? LastEvent => Demo ? DateTimeOffset.UtcNow : Connection?.Projection.LastEvent;
    private bool Overflow => Connection?.Projection.Overflow == true;
    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    private bool MatchText(string text) => string.IsNullOrWhiteSpace(Filter) || text.Contains(Filter, StringComparison.OrdinalIgnoreCase);
    private bool MatchNode(ObservedResource row) => Node == "all" || row.Node == Node;
    private EventsPanelCardInfo[] VisibleCards => Cards.Where(c => c.Kind == TabKind(Tab) && MatchText(c.Label + " " + string.Join(" ", c.Channels)) && ((Node == "all" && StatusFilter == "all") || Related(c).Any(MatchStatus))).OrderBy(c => c.Order).ThenBy(c => c.Label).ToArray();
    private bool MatchesChannel(ObservedResource r) => MatchNode(r) && MatchStatus(r) && MatchText(r.Key + " " + r.Caller + " " + r.Destination + " " + r.Account + " " + LabelFor(r) + " " + r.Details.GetValueOrDefault("linkedid"));
    private ObservedResource[] VisibleChannels => Rows.Where(r => r.Kind == "call" && MatchesChannel(r)).OrderByDescending(r => r.FirstSeen).ToArray();
    private bool MatchStatus(ObservedResource row) => StatusFilter switch { "all" => true, "online" or "offline" => row.Peer?.Reachability == StatusFilter, "registered" or "unregistered" => row.Peer?.Registration == StatusFilter, "unknown" => row.Kind == "peer" && (row.Peer is null || row.Peer.Registration == "unknown"), "busy" => row.Kind == "call" || row.Peer?.DeviceLabel is "Em uso" or "Ocupado" or "Tocando" or "Tocando em uso", _ => false };
    private static string CardKey(EventsPanelCardInfo card) => card.Kind + "|" + card.Label + "|" + string.Join(";", card.Channels);
    private string Timestamp(DateTimeOffset? value) => value?.ToLocalTime().ToString("G", Text.Culture) ?? Text["Not reported"];
    private ObservedResource[] VisibleEvents => Observation.Rows.Where(r => MatchNode(r) && MatchStatus(r) && MatchText(r.Key + " " + r.Caller + " " + r.State)).OrderByDescending(r => r.Updated).ToArray();
    private int TotalVisible => Tab == "events" ? VisibleEvents.Length : Tab == "calls" ? VisibleGroups.Length : Tab == "channels" ? VisibleChannels.Length : VisibleCards.Length;
    private int Pages => Math.Max(1, (TotalVisible + PageSize - 1) / PageSize);
    private ObservedResource[] Related(EventsPanelCardInfo card) => Observation.ByCard.TryGetValue(card, out var values) ? values.Where(MatchNode).ToArray() : [];
    private string LabelFor(ObservedResource row) => Observation.Labels.GetValueOrDefault(row.Id, row.Key);
    private int Count(string tab) => tab == "events" ? VisibleEvents.Length : tab == "settings" ? BoardConfiguration.Rules.Length : tab == "board" ? VisibleTiles.Length : tab == "calls" ? VisibleGroups.Count(g => g.Correlated) : tab == "channels" ? VisibleChannels.Length : Cards.Count(c => c.Kind == TabKind(tab));
    private static EventsPanelCardKind TabKind(string tab) => tab switch { "trunks" => EventsPanelCardKind.TRUNK, "queues" => EventsPanelCardKind.QUEUE, _ => EventsPanelCardKind.PEER };
    private string KindLabel(EventsPanelCardKind kind) => kind switch { EventsPanelCardKind.TRUNK => Text["Trunk"], EventsPanelCardKind.QUEUE => Text["Call queue"], _ => Text["Extension"] };
    private static string Duration(DateTimeOffset from) => (DateTimeOffset.UtcNow - from).ToString(@"hh\:mm\:ss");
    private string ResourceSummary(ObservedResource[] related)
    {
        if (related.Length == 0) return Text["No observed state"];
        if (Tab == "queues") return Text.Format("Waiting: {0} · Agents: {1} (observed)", related.Count(r => r.Kind == "waiting"), related.Count(r => r.Kind == "agent"));
        var peer = related.Where(r => r.Kind == "peer").OrderByDescending(r => r.Updated).FirstOrDefault();
        var calls = related.Count(r => r.Kind == "call");
        return (peer is null ? Text["Registration not reported"] + " · " : peer.Node + ": " + Text.Observed(peer.Peer?.RegistrationLabel ?? Text["Registration not reported"]) + " · " + ResourceState(peer) + " · ") + Text.Format("Observed channels: {0}", calls);
    }
    private string DetailValue(string value) => value switch { "true" => Text["yes"], "false" => Text["no"], _ => value };
    private string ResourceState(ObservedResource row) => row.Kind == "agent" ? row.State.ToLowerInvariant() switch
    {
        "1" or "notinuse" or "not_inuse" => Text["Idle in event"], "2" or "inuse" => Text["In use"], "3" or "busy" => Text["Busy"],
        "4" or "invalid" => Text["Invalid"], "5" or "unavailable" => Text["Unavailable"], "6" or "ringing" => Text["Ringing"], "7" or "ringinuse" => Text["Ringing while in use"], "8" or "onhold" => Text["On hold"], "0" => Text["Unknown state"], _ => State(row.State)
    } : Text.Observed(row.Peer?.ReachabilityLabel ?? State(row.State));
    private string State(string value) => value.ToLowerInvariant() switch { "up" or "6" => Text["Channel answered"], "ringing" or "5" => Text["Ringing"], "ring" or "4" => Text["Calling"], "down" or "0" => Text["Inactive in event"], "reachable" or "registered" => Text["Available in event"], "unreachable" or "unregistered" => Text["Unavailable in event"], "unknown" or "" => Text["Not reported"], _ => value };
    private string DetailLabel(string key) => key switch { "paused" => Text["paused"], "calls" => Text["Calls"], "completed" => Text["answered"], "abandoned" => Text["abandoned"], "holdtime" => Text["wait (s)"], "talktime" => Text["talk (s)"], "penalty" => Text["priority"], "strategy" => Text["strategy"], "membername" => Text["agent"], "interface" => "interface", "status" => "status", _ => key };
    protected override async Task OnAfterRenderAsync(bool first)
    {
        FlushUrl();
        if (FocusConfirmation) { FocusConfirmation = false; await Confirmation.FocusAsync(); }
        if (Fill != fitted) { fitted = Fill; await JS.InvokeVoidAsync(fitted ? "telephonyPanel.fitBoard" : "telephonyPanel.stopFitBoard"); }
        if (!first) return;
        Navigation.LocationChanged += LocationChanged;
        var initial = BoardViewQuery.Parse(Navigation.Uri);
        await SelectClient(initial.ContextId, initial.ContextId?.ToString("N") ?? Text["Entire exchange"], false);
        await ApplyUrl();
        UrlReady = true;
        WriteUrl(true);
        await ReadBoardConfiguration();
        StateHasChanged();
        loop = Poll();
        await Task.CompletedTask;
    }
    private async Task Search()
    {
        Busy = true; SearchError = null; Clients = []; Searched = false;
        try
        {
            if (Guid.TryParse(SearchText, out var id) && id != Guid.Empty) { await SelectClient(id, SearchText); return; }
            Clients = Demo ? [(Guid.Parse("11111111-1111-7111-8111-111111111111"), "Cliente demonstração")] : await Access.Search(SearchText, lifetime.Token);
            Searched = true;
        }
        catch (Exception e) { SearchFailure = Diagnostics.Report(e, "company-search"); SearchError = ErrorText(e); }
        finally { Busy = false; }
    }
    private async Task SelectClient(Guid? id, string title, bool writeUrl = true)
    {
        if (ActionBusy || InventoryBusy) return;
        var version = ++selectionVersion;
        inventoryTiles = null; ConfirmClearInventory = false; InventoryMessage = null; InventoryError = null;
        Busy = true; Error = null; Failure = null; SupervisorFailure = null; QueueSamples = []; QueueError = null; SearchError = null; Pending = null; ActionMessage = null; Rows = []; Cards = []; PortalCards = []; Supervisor = null; Clients = []; Searched = false;
        Context = id; ClientName = title; SelectedTileId = null; BoardAction = null;
        if (writeUrl) WriteUrl();
        if (!id.HasValue && Tab == "trunks") Tab = "extensions";
        try
        {
            if (Demo)
            {
                Permissions = CustomerAccessPreview.Read(Navigation.Uri);
                Context = Permissions.SelectContext(id);
                if (Permissions.SingleContext.HasValue) { ClientName = Context!.Value.ToString("D"); WriteUrl(); }
                if (!Permissions.Manager && Tab == "settings") Tab = "board";
                LoadDemo(); return;
            }
            Permissions = await Access.Permissions(lifetime.Token);
            id = Permissions.SelectContext(id);
            Context = id;
            if (Permissions.SingleContext.HasValue) { ClientName = id!.Value.ToString("D"); WriteUrl(); }
            if (!Permissions.Manager && Tab == "settings") Tab = "board";
            var cards = id.HasValue ? await Access.Cards(id, lifetime.Token) : [];
            if (version != selectionVersion) return;
            PortalCards = cards; Cards = cards;
            AuthorizationChecked = DateTimeOffset.UtcNow;
            var connection = await Access.Connection(lifetime.Token);
            if (version != selectionVersion) return;
            Connection = connection;
            await connection.Connect(lifetime.Token);
            if (version != selectionVersion) return;
            try
            {
                var supervisor = Permissions.Manager || Permissions.MonitorContexts.Count > 0 ? await Access.Supervisor(lifetime.Token) : null;
                if (version != selectionVersion) return;
                Supervisor = supervisor;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            { SupervisorFailure = Diagnostics.Report(e, "supervisor-extension-read"); }
            if (version == selectionVersion) await ReadRows();
        }
        catch (Exception e) { if (version == selectionVersion) { ClearAuthorizedState(); Failure = Diagnostics.Report(e, "live-observation"); Error = ErrorText(e); } }
        finally { if (version == selectionVersion) Busy = false; }
    }
    private Task Reload() => SelectClient(Context, ClientName);
    private Task ClearCompany() { SearchText = ""; return SelectClient(null, Text["Entire exchange"]); }
    private async Task Poll()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            while (await timer.WaitForNextTickAsync(lifetime.Token)) await InvokeAsync(async () =>
            {
                if (Busy || Demo || Error is not null) return;
                var version = selectionVersion;
                try
                {
                    var context = Context;
                    var connection = await Access.Connection(lifetime.Token);
                    if (version != selectionVersion) return;
                    Connection = connection;
                    if (context.HasValue && DateTimeOffset.UtcNow - AuthorizationChecked > TimeSpan.FromSeconds(60))
                    {
                        var cards = await Access.Cards(context, lifetime.Token);
                        if (version != selectionVersion) return;
                        PortalCards = cards; Cards = cards;
                        AuthorizationChecked = DateTimeOffset.UtcNow;
                    }
                    await ReadRows();
                    await ReadBoardConfiguration();
                }
                catch (Exception e) { if (version == selectionVersion) { ClearAuthorizedState(); Failure = Diagnostics.Report(e, "live-observation"); Error = ErrorText(e); } }
                Page = Math.Min(Page, Pages - 1); StateHasChanged();
            });
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }
    private async Task ReadRows()
    {
        var version = selectionVersion;
        var selectedContext = Context;
        var samples = await QueueAccess.Read(selectedContext, lifetime.Token);
        if (version != selectionVersion) return;
        QueueSamples = samples;
        QueueError = !QueueAccess.Configured ? Text["QUEUE monitoring is not configured on this panel."]
            : samples.Any(s => QueueSourceHealth.Problem(s, DateTimeOffset.UtcNow) is not null)
                ? Text["Some QUEUE sources are unavailable. Queue activity is shown only with current controller data."] : null;
        Permissions = await Access.Permissions(lifetime.Token);
        Observation = await Access.Observation(selectedContext, PortalCards, lifetime.Token);
        Observation.AddQueue(QueueBoardProjection.Create(samples, Observation.Rows, selectedContext, DateTimeOffset.UtcNow));
        Cards = Observation.Cards;
        Rows = Observation.Rows;
        await RefreshInventory();
    }
    private Guid TargetContext(ObservedResource row) => Context ?? (Guid.TryParse(row.Account, out var id) ? id : Guid.Empty);
    private void ClearAuthorizedState()
    {
        inventoryTiles = null; Rows = []; Cards = []; PortalCards = []; Observation = new(); QueueSamples = [];
        Pending = null; BoardAction = null; SelectedTileId = null; Connection = null; Supervisor = null;
        Permissions = CurrentAuthorization.None; BoardConfiguration = new(); SettingsOpened = false;
    }
    private bool CanSuperviseTile(OperatorTile tile) => (Demo && Permissions.Manager) || tile.Rows.Any(row =>
        Permissions.CanRead(TargetContext(row)) && Permissions.CanMonitor(TargetContext(row)));
    private BoardSupervisionAction? BoardAction;
    private bool DemoEndpointResolution => Demo && Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query)
        .TryGetValue("supervisionpreview", out var scenario) && scenario == "idle";
    private void PrepareBoardSupervision(BoardSupervisionAction action)
    { CloseTile(); Pending = null; BoardAction = action; }
    private string? SupervisionBlockReason(ObservedResource row)
    {
        if (ActionBusy) return "A supervision request is already in progress.";
        if (!Supervisor.HasValue) return SupervisorFailure is not null
            ? "Could not load your supervision extension. Reconnect and refresh to try again."
            : "Set your preferred extension in the portal to enable supervision.";
        if (Demo && !DiagnosticPreview) return null;
        var reason = MonitorActionGuard.BlockReason(row, TargetContext(row), Connected, DateTimeOffset.UtcNow);
        if (reason is not null) return reason;
        return Permissions.CanRead(TargetContext(row)) && Permissions.CanMonitor(TargetContext(row)) ? null
            : "Your account does not have supervision permission for this company.";
    }
    private void Prepare(ObservedResource row, TelephonyMonitorActionMode mode)
    {
        Pending = row; Mode = mode; ActionMessage = null; FocusConfirmation = true;
    }
    private void CancelAction() => Pending = null;
    private async Task Confirm()
    {
        if (ActionBusy || Pending is null) return;
        ActionBusy = true; ActionMessage = null; ActionFailure = null;
        var target = Pending; Pending = null;
        try
        {
            if (Demo) { ActionMessage = Text["Demonstration: confirmation validated. No call was sent."]; return; }
            await Access.Monitor(TargetContext(target), target.Id, Mode, lifetime.Token);
            ActionMessage = Text["Request accepted by the exchange. Answer your extension. This does not confirm audio is connected yet."];
        }
        catch (Exception e) { ActionFailure = Diagnostics.Report(e, "channel-supervision"); ActionMessage = ErrorText(e) + Text[" No automatic retry. If the response was lost, check your extension before retrying."]; }
        finally { ActionBusy = false; }
    }
    private string ErrorText(Exception e) => Text[PanelDiagnostics.UserMessage(e)];
    private void SetFilter(string? value) { Filter = value ?? ""; Page = 0; WriteUrl(true); }
    private void SetNode(string value) { Node = value; Page = 0; WriteUrl(); }
    private void SetStatus(string value) { StatusFilter = value; Page = 0; Pending = null; WriteUrl(); }
    private void SetTab(string value) { Tab = value; Page = 0; Pending = null; SettingsOpened |= value == "settings"; WriteUrl(); }
    private void Previous() { Page = Math.Max(0, Page - 1); WriteUrl(); }
    private void Next() { Page = Math.Min(Pages - 1, Page + 1); WriteUrl(); }
    private void LoadDemo()
    {
        Cards = [new() { Label = "Hugo · atendimento", Kind = EventsPanelCardKind.PEER, Channels = ["PJSIP/6007"] }, new() { Label = "Recepção", Kind = EventsPanelCardKind.PEER, Channels = ["PJSIP/1001"] }, new() { Label = "Comercial", Kind = EventsPanelCardKind.PEER, Channels = ["PJSIP/1002"] }, new() { Label = "Operadora · saída", Kind = EventsPanelCardKind.TRUNK, Channels = ["PJSIP/trunk-demo"] }, new() { Label = "Atendimento geral", Kind = EventsPanelCardKind.QUEUE, Channels = ["fila-demo"] }];
        var now = DateTimeOffset.UtcNow;
        var tenant = Guid.Parse("11111111-1111-7111-8111-111111111111");
        Rows = [new("demo-call", "eveo-voip", "call", "PJSIP/6007-000000ab", "Up", "Cliente demonstração", "6007", tenant.ToString("N"), now.AddMinutes(-3), now, new Dictionary<string,string> { ["linkedid"] = "demo-linked-1" }),
            new("demo-local-1", "eveo-voip", "call", "Local/6007@from-queue-0001;1", "Down", "Cliente demonstração", "6007", tenant.ToString("N"), now.AddMinutes(-3), now, new Dictionary<string,string> { ["linkedid"] = "demo-linked-1" }),
            new("demo-local-2", "eveo-voip", "call", "Local/6007@from-queue-0001;2", "Ring", "Cliente demonstração", "6007", tenant.ToString("N"), now.AddMinutes(-3), now, new Dictionary<string,string> { ["linkedid"] = "demo-linked-1" }),
            new("demo-uncorrelated", "google-voip", "call", "SIP/exemplo-sem-correlacao-000000cd", "Down", "Origem não informada", "", null, now.AddMinutes(-1), now, new Dictionary<string,string>()),
            new("demo-peer", "apoint-voip", "peer", "PJSIP/1001", "Registered", "", "", null, now, now, new Dictionary<string,string>()) { Peer = new() { Registration = "registered", RegistrationObservedAt = now, LastRegisteredObservedAt = now, LastEventType = "PeerStatusEvent" } },
            new("demo-trunk", "google-voip", "peer", "PJSIP/trunk-demo", "Reachable", "", "", null, now, now, new Dictionary<string,string>()) { Peer = new() { Reachability = "online", ReachabilityObservedAt = now, LastEventType = "ContactStatusEvent", LastReachabilityIsContact = true } },
            new("demo-agent", "eveo-voip", "agent", "fila-demo/PJSIP/6007", Text["In conversation"], "", "", null, now, now, new Dictionary<string,string> { ["queue"] = "fila-demo", ["paused"] = "false", ["membername"] = "Hugo" }),
            new("demo-wait", "eveo-voip", "waiting", "PJSIP/6007-000000ab", "waiting", "Cliente demonstração", "", tenant.ToString("N"), now.AddSeconds(-43), now, new Dictionary<string,string> { ["queue"] = "fila-demo", ["linkedid"] = "demo-linked-1" }),
            new("demo-hugo", "eveo-voip", "peer", "PJSIP/6007", "Reachable", "", "", null, now.AddMinutes(-8), now, new Dictionary<string,string>()) { Peer = new() { Registration = "registered", Reachability = "online", Device = "2", RegistrationObservedAt = now.AddMinutes(-7), LastRegisteredObservedAt = now.AddMinutes(-7), ReachabilityObservedAt = now, DeviceObservedAt = now, RoundTripMilliseconds = 12.5m, LastEventType = "ContactStatusEvent", LastReachabilityIsContact = true } },
            new("demo-other", "google-voip", "peer", "PJSIP/2003", "Unregistered", "", "", null, now, now, new Dictionary<string,string>()) { Peer = new() { Registration = "unregistered", Reachability = "offline", RegistrationObservedAt = now, LastRegisteredObservedAt = now.AddMinutes(-6), ReachabilityObservedAt = now, LastEventType = "PeerStatusEvent" } }];
        var demoQuery = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query);
        if (demoQuery.TryGetValue("supervisionpreview", out var supervisionScenario) && supervisionScenario == "multiple")
            Rows = Rows.Append(Rows[0] with { Id = "demo-second-call", Key = "PJSIP/6007-000000ef" }).ToArray();
        if (DemoEndpointResolution) Rows = Rows.Where(row => row.Kind is not ("call" or "waiting")).ToArray();
        var demoObservation = AuthorizedObservation.Create(Cards, Rows, tenant);
        demoTiles = OperatorTile.Create(Cards, demoObservation.ByCard);
        // Dense operator-board fixture only: never mixed into production observations
        // or the call/channel counting fixture above.
        demoTiles = demoTiles.Concat(Enumerable.Range(1, 72).Select(i =>
        {
            var node = i % 3 == 0 ? "google-voip" : i % 3 == 1 ? "eveo-voip" : "apoint-voip";
            var number = (6100 + i).ToString();
            var resource = new ObservedResource("board-demo-" + i, node, "peer", "PJSIP/" + number,
                "unknown", "", "", null, now, now, new Dictionary<string, string>())
            { Peer = new PeerEvidence { Registration = i % 9 == 0 ? "unregistered" : i % 5 == 0 ? "unknown" : "registered",
                Reachability = i % 9 == 0 ? "offline" : i % 5 == 0 ? "unknown" : "online",
                Device = i % 11 == 0 ? "6" : i % 7 == 0 ? "2" : "unknown",
                RegistrationObservedAt = now, ReachabilityObservedAt = now } };
            return new OperatorTile(resource.Id, $"{number} · {new[] { "Atendimento", "Comercial", "Suporte", "Recepção" }[i % 4]}", node, EventsPanelCardKind.PEER, [resource]);
        })).ToArray();
        Supervisor = (Guid.Parse("22222222-2222-7222-8222-222222222222"), "Hugo · demonstração");
        Observation = Context is { } context ? AuthorizedObservation.Create(Cards, Rows, context) : AuthorizedObservation.ForManager(Rows);
        if (!Context.HasValue) Cards = Observation.Cards;
        Rows = Observation.Rows;
        if (DiagnosticPreview)
        {
            var seed = Rows.First(r => r.Kind == "call");
            Rows = [seed, seed with { Id = "missing-context", Account = null },
                seed with { Id = "local-channel", Key = "Local/6007@demo-000000ab;1" },
                seed with { Id = "stale-channel", Updated = now.AddSeconds(-60) }];
            Error = Text["Could not query the telephony service. Try refreshing; if it persists, check the connection and permissions."];
            Failure = new("demo-error-reference", now, "FileNotFoundException");
            QueueError = Text["Some QUEUE sources are unavailable. Queue activity is shown only with current controller data."];
            var scope = Guid.Parse("11111111-1111-7111-8111-111111111111");
            QueueSamples = [new("google-voip", scope, new(scope.ToString("N"), Guid.NewGuid().ToString("N"), default, false, false, []), true),
                new("eveo-voip", scope, new(scope.ToString("N"), Guid.NewGuid().ToString("N"), now, true, false, []), true)];
        }
        LoadQueueDemo();
    }
    private void LoadQueueDemo()
    {
        // Development-only fixture: exercise the real merge, filters and renderer without PBX access.
        if (!Demo) return;
        var values = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query);
        if (!values.TryGetValue("acdpreview", out var scenario)) return;
        var now = DateTimeOffset.UtcNow;
        var tenant = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var queueId = "11111111111171118111111111116599";
        var channel = new ObservedResource("queue-demo-call", "google-voip", "call", "SIP/0000006007-000000ef", "Up", "6007", "6599", tenant.ToString("N"),
            now.AddSeconds(-10), now, new Dictionary<string, string> { ["uniqueid"] = "queue-demo-1", ["linkedid"] = "queue-demo-1" });
        var phase = scenario.ToString() is "greeting" or "offering" or "connected" ? scenario.ToString() : "waiting";
        var ended = scenario == "empty";
        var snapshot = new QueueBoardSnapshot(tenant.ToString("N"), "22222222222272228222222222222222", now, true, false,
            [new(queueId, "Homologação Sufficit — 6599", ended || phase == "connected" ? 0 : 1, 2, ended ? null : now.AddSeconds(-10),
                Offering: phase == "offering" ? 1 : 0, Connected: phase == "connected" ? 1 : 0, Greeting: phase == "greeting" ? 1 : 0)],
            ended ? [] : [new("33333333333373338333333333333333", queueId, phase, now.AddSeconds(-10), channel.Details["uniqueid"], channel.Key, "6007")]);
        if (scenario == "legacy") snapshot = snapshot with { Visits = null };
        var sample = new QueueBoardSample("google-voip", tenant, snapshot, scenario != "stale");
        var native = new EventsPanelCardInfo { Label = "Hugo · piloto 6599", Kind = EventsPanelCardKind.PEER, Channels = ["SIP/0000006007"] };
        var projection = QueueBoardProjection.Create([sample], ended ? [] : [channel], Context, now);
        var fixture = Context is { } context ? AuthorizedObservation.Create([native], ended ? [] : [channel], context)
            : AuthorizedObservation.ForManager(ended ? [] : [channel]);
        fixture.AddQueue(projection);
        demoTiles = demoTiles.Concat(OperatorTile.Create(fixture.Cards, fixture.ByCard)).ToArray();
        Cards = Cards.Concat(fixture.Cards).ToArray();
        Rows = Rows.Concat(fixture.Rows).ToArray();
    }
    public async ValueTask DisposeAsync()
    {
        UrlReady = false; Navigation.LocationChanged -= LocationChanged; lifetime.Cancel();
        if (loop is not null) await loop;
        if (fitted) { try { await JS.InvokeVoidAsync("telephonyPanel.stopFitBoard"); } catch (JSDisconnectedException) { } catch (TaskCanceledException) { } }
        lifetime.Dispose();
    }
}
