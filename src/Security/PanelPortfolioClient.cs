using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Sufficit.Identity.Authorization;

namespace Sufficit.Telephony.Panel.Security;

/// <summary>Application-owned portfolio from Endpoints; never changes Identity monitor grants.</summary>
public sealed class PanelPortfolioClient(IHttpClientFactory clients, TimeProvider clock) : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 200 });
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task<CurrentAuthorization> Expand(CurrentAuthorization permissions, string token, CancellationToken ct)
    {
        if (!permissions.PanelAllowed || permissions.GlobalPanel) return permissions;
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))) + ":"
            + string.Join(",", permissions.PanelContexts.Order());
        await gate.WaitAsync(ct);
        try
        {
            if (cache.Get<Entry>(key) is { } existing && existing.Expires > clock.GetUtcNow())
                return permissions with { PanelContexts = existing.Contexts };
            cache.Remove(key);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://endpoints.sufficit.com.br/telephony/panel/contexts");
            request.Headers.Authorization = new("Bearer", token);
            using var response = await clients.CreateClient("operations").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync(64 * 1024, timeout.Token);
            var ids = await response.Content.ReadFromJsonAsync<Guid[]>(timeout.Token) ?? throw new InvalidOperationException("Missing portfolio.");
            if (ids.Length > 1000 || ids.Contains(Guid.Empty) || !permissions.PanelContexts.IsSubsetOf(ids))
                throw new InvalidOperationException("Invalid panel portfolio.");
            var contexts = ids.ToImmutableHashSet();
            cache.Set(key, new Entry(contexts, clock.GetUtcNow().AddSeconds(45)), new MemoryCacheEntryOptions
                { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45) });
            return permissions with { PanelContexts = contexts };
        }
        finally { gate.Release(); }
    }
    public void Dispose() { cache.Dispose(); gate.Dispose(); }
    private sealed record Entry(ImmutableHashSet<Guid> Contexts, DateTimeOffset Expires);
}
