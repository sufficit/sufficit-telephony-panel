using System.Text.Json;

namespace Sufficit.Telephony.Panel.Operations;

/// <summary>Single-host shared settings. Never store inside a replaceable release directory.</summary>
public sealed class BoardRuleStore(string path)
{
    private readonly SemaphoreSlim gate = new(1);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private BoardConfiguration? current;
    private static BoardConfiguration Copy(BoardConfiguration value) => value with
    { Rules = value.Rules.Select(r => r with { Patterns = r.Patterns.ToArray() }).ToArray() };

    private async Task<BoardConfiguration> Load(CancellationToken ct)
    {
        if (current is not null) return current;
        if (!File.Exists(path)) return current = new();
        if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidOperationException("O arquivo de regras excede o limite de 1 MB.");
        await using var stream = File.OpenRead(path);
        var value = await JsonSerializer.DeserializeAsync<BoardConfiguration>(stream, Json, ct)
            ?? throw new InvalidOperationException("Arquivo de regras inválido. Solicite revisão à equipe técnica.");
        value.Validate();
        return current = value;
    }
    public async Task<BoardConfiguration> Read(CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try { return Copy(await Load(ct)); }
        finally { gate.Release(); }
    }
    public async Task<BoardConfiguration> Save(BoardConfiguration draft, CancellationToken ct = default)
    {
        draft.Validate();
        await gate.WaitAsync(ct);
        string? temporary = null;
        try
        {
            var previous = await Load(ct);
            if (draft.Revision != previous.Revision)
                throw new InvalidOperationException("Outro gerente alterou as regras. Recarregue a configuração antes de salvar novamente.");
            var next = Copy(draft) with { Revision = Guid.CreateVersion7().ToString("N"), UpdatedAt = DateTimeOffset.UtcNow };
            var directory = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(directory);
            temporary = Path.Combine(directory, ".board-rules-" + Guid.CreateVersion7().ToString("N") + ".tmp");
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.WriteThrough
            };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            await using (var file = new FileStream(temporary, options))
            {
                await JsonSerializer.SerializeAsync(file, next, Json, ct);
                await file.FlushAsync(ct);
                file.Flush(true);
            }
            File.Move(temporary, path, overwrite: true);
            temporary = null; current = next;
            return Copy(next);
        }
        finally
        {
            if (temporary is not null && File.Exists(temporary)) File.Delete(temporary);
            gate.Release();
        }
    }
}
