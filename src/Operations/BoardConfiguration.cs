namespace Sufficit.Telephony.Panel.Operations;

public sealed record BoardConfiguration
{
    public int Schema { get; init; } = 1;
    public string Revision { get; init; } = "";
    public bool ShowUnmatched { get; init; } = true;
    public bool RetainSeenResources { get; init; } = true;
    public DateTimeOffset? UpdatedAt { get; init; }
    public BoardRule[] Rules { get; init; } = [];

    public void Validate()
    {
        if (Schema != 1 || Rules is null || Rules.Length > 100)
            throw new InvalidOperationException("São permitidas até 100 regras na versão atual.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (Rules.Sum(r => r?.Patterns?.Length ?? 0) > 512)
            throw new InvalidOperationException("Use até 512 identificadores no conjunto de regras.");
        foreach (var rule in Rules)
        {
            if (rule is null || !Guid.TryParseExact(rule.Id, "N", out _) || rule.Id[12] != '7' || !ids.Add(rule.Id))
                throw new InvalidOperationException("Cada regra deve ter um identificador UUIDv7 único, sem hífens.");
            if (string.IsNullOrWhiteSpace(rule.Label) || rule.Label.Length > 80 || rule.Label.Any(char.IsControl))
                throw new InvalidOperationException("Informe um nome de até 80 caracteres para cada regra.");
            if (rule.Kind is not ("trunk" or "peer" or "hidden"))
                throw new InvalidOperationException("Escolha Tronco, Ramal ou Ocultar da mesa.");
            if (rule.Node is null || rule.Node.Length > 100 || rule.Node.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
                throw new InvalidOperationException("O servidor deve ser seu identificador exato, sem espaços.");
            if (rule.Patterns is null || rule.Patterns.Length is < 1 or > 64)
                throw new InvalidOperationException("Informe de 1 a 64 identificadores por regra, um por linha.");
            foreach (var pattern in rule.Patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern) || pattern.Length > 160 || pattern.Any(char.IsWhiteSpace)
                    || pattern.Any(char.IsControl) || !pattern.Contains('/') || pattern.EndsWith('/')
                    || pattern.Contains('?') || pattern.Contains('[') || pattern.Contains(']')
                    || (pattern.Contains('*') && (pattern.IndexOf('*') != pattern.Length - 1 || pattern.Length < 7 || pattern[^2] == '/')))
                    throw new InvalidOperationException("Use identificadores como PJSIP/ramal ou prefixos explícitos como SIP/in-dtr-*. Somente * no final é permitido.");
            }
        }
    }
}
