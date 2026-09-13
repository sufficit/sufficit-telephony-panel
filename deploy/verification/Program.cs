using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sufficit.Telephony.Panel.Operations;

// Run as the actual panel Unix user with only its public source configuration.
// No credentials, caller identities or queue titles are printed.
var config = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var sources = config.GetSection("Panel:AcdSources").Get<AcdBoardSource[]>() ?? [];
if (sources.Length != 1) throw new InvalidOperationException("Expected configured Google source.");
var services = new ServiceCollection();
services.AddHttpClient("acd-board", client => client.Timeout = TimeSpan.FromSeconds(3))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
using var provider = services.BuildServiceProvider();
using var reader = new AcdBoardReader(sources, provider.GetRequiredService<IHttpClientFactory>(), TimeProvider.System);
var samples = await reader.Read(null, CancellationToken.None);
if (samples.Length != 1 || !samples[0].Available || samples[0].Snapshot is not { } snapshot ||
    !snapshot.Queues.Any(q => q.QueueId == "01a091d2251c7f30a962045bef466dcc") || snapshot.Visits is null)
    throw new InvalidOperationException("Live ACD source failed validation or pilot queue is missing.");
Console.WriteLine($"PASS: configured reader accepted live Google snapshot, {snapshot.Queues.Length} queues, visit export present.");
