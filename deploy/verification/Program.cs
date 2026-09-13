using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sufficit.Telephony.Panel.Operations;

// Run as the actual panel Unix user with only its public source configuration.
// No credentials, caller identities or queue titles are printed.
var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var sources = config.GetSection("Panel:AcdSources").Get<AcdBoardSource[]>() ?? [];
if (sources.Length is < 1 or > 3) throw new InvalidOperationException("Expected approved sources.");
var services = new ServiceCollection();
services.AddHttpClient("acd-board", client => { client.Timeout = TimeSpan.FromSeconds(3); client.MaxResponseContentBufferSize = 4 * 1024 * 1024; })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
using var provider = services.BuildServiceProvider();
using var reader = new AcdBoardReader(sources, provider.GetRequiredService<IHttpClientFactory>(), TimeProvider.System);
var samples = await reader.Read(null, CancellationToken.None);
if (samples.Length != sources.Length || samples.Any(s => !s.Available || s.Snapshot is not { ControllerConnected: true, Visits: not null }))
    throw new InvalidOperationException("Live ACD source failed validation or pilot queue is missing.");
foreach (var sample in samples)
{
    if (sample.Node != "google-voip" && !sample.Snapshot!.Agents!.Any(a => a.RegistrationHistory is { Length: > 0 }))
        throw new InvalidOperationException("Lab registration history missing.");
    Console.WriteLine($"PASS: {sample.Node} snapshot accepted, {sample.Snapshot!.Queues.Length} queues, {sample.Snapshot.Agents?.Length ?? 0} agent observations.");
}
