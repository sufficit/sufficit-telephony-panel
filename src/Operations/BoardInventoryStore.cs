using System.Text.Json;
using Sufficit.Telephony.EventsPanel;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Single-host durable inventory, separate from the expiring live projection.</summary>
public sealed class BoardInventoryStore(string path)
{
    private readonly SemaphoreSlim gate = new(1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private BoardInventoryState? state;
    private async Task<BoardInventoryState> Load(CancellationToken ct)
    {
        if (state is not null) return state;
        if (!File.Exists(path)) return state = new();
        if (new FileInfo(path).Length > 128 * 1024 * 1024)
            throw new InvalidOperationException("Inventário excedeu 128 MB. Solicite revisão; nada foi apagado.");
        await using var file = File.OpenRead(path);
        var loaded = await JsonSerializer.DeserializeAsync<BoardInventoryState>(file, Json, ct);
        if (loaded is null || loaded.Schema != 1 || loaded.Resources is null || loaded.ClearedAt is null)
            throw new InvalidOperationException("Inventário inválido. Solicite revisão; nada foi apagado.");
        return state = loaded;
    }
    private async Task Save(BoardInventoryState next, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".inventory-" + Guid.CreateVersion7().ToString("N") + ".tmp");
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write,
                Share = FileShare.None, Options = FileOptions.Asynchronous | FileOptions.WriteThrough };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            await using (var file = new FileStream(temporary, options))
            {
                await JsonSerializer.SerializeAsync(file, next, Json, ct);
                await file.FlushAsync(ct); file.Flush(true);
            }
            File.Move(temporary, path, true);
            state = next;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static string Scope(Guid? context) => context?.ToString("N") ?? "central";
    private static bool Stable(OperatorTile tile) => tile.Kind == EventsPanelCardKind.QUEUE
        || tile.Keys.Length > 0 && tile.Keys.All(k => k.StartsWith("PJSIP/", StringComparison.Ordinal)
            || k.StartsWith("SIP/", StringComparison.Ordinal) || k.StartsWith("IAX2/", StringComparison.Ordinal));

    // The caller must provide only currently authorized cards; tenant history never derives from central history.
    public async Task<OperatorTile[]> Observe(Guid? context, OperatorTile[] current,
        EventsPanelCardInfo[] authorizedCards, bool retain, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var loaded = await Load(ct);
            var scope = Scope(context);
            var known = loaded.Resources.ToDictionary(r => (r.Scope, r.Id));
            var changed = false;
            foreach (var tile in current.Where(t => t.Rows.Length > 0 && Stable(t)))
            {
                var seen = tile.Updated!.Value;
                if (seen <= loaded.ClearedAt.GetValueOrDefault(scope)) continue;
                var key = (scope, tile.Id);
                if (!known.TryGetValue(key, out var prior) || seen - prior.LastSeen >= TimeSpan.FromMinutes(1)
                    || prior.Title != tile.Title || !prior.Keys.SequenceEqual(tile.Keys))
                {
                    // Never evict silently: capacity errors preserve the entire previous inventory.
                    if (prior is null && known.Count >= 100_000)
                        throw new InvalidOperationException("Inventário atingiu 100.000 recursos. Nada foi apagado; limpe recursos lembrados ou solicite ampliação.");
                    known[key] = new(scope, tile.Id, tile.Title, tile.Node, tile.Kind, tile.Keys.ToArray(), seen);
                    changed = true;
                }
            }
            if (changed) await Save(loaded with { Resources = known.Values.ToArray() }, ct);
            if (!retain) return current;
            var ids = current.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
            var permitted = authorizedCards.Select(c => (c.Kind, Keys: string.Join(";", c.Channels))).ToHashSet();
            var remembered = known.Values.Where(r => r.Scope == scope && !ids.Contains(r.Id)
                && (!context.HasValue || permitted.Contains((r.Kind, string.Join(";", r.Keys)))))
                .Select(r => r.Tile()).ToArray();
            // Avoid a duplicate unobserved placeholder when a real node is already known.
            var withNodes = remembered.Concat(current.Where(t => t.Rows.Length > 0))
                .Select(t => (t.Kind, Keys: string.Join(";", t.Keys))).ToHashSet();
            return current.Where(t => t.Node != "Sem nó observado" || !withNodes.Contains((t.Kind, string.Join(";", t.Keys))))
                .Concat(remembered).ToArray();
        }
        finally { gate.Release(); }
    }
    public async Task Clear(Guid? context, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var loaded = await Load(ct); var scope = Scope(context);
            var cutoffs = new Dictionary<string, DateTimeOffset>(loaded.ClearedAt) { [scope] = DateTimeOffset.UtcNow };
            await Save(loaded with { Resources = loaded.Resources.Where(r => r.Scope != scope).ToArray(), ClearedAt = cutoffs }, ct);
        }
        finally { gate.Release(); }
    }
}
