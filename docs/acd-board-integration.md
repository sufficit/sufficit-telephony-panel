# ACD queues on the operations board

## Sources and identity

Native Queue activity still comes from the existing AMI/gRPC stream. ARI ACD queues
come from the controller's private, context/node-scoped snapshot endpoint. This is
an additional source, not a synthesized QueueCallerJoin or another AMI connection.
The board shares the existing manager-only authorization; customer publication is
not enabled. The configured context allowlist also bounds central-view disclosure.

Each ACD card key contains `acd:<context UUID N32>:<queue UUID N32>`. Its observed
node is mapped explicitly from the controller UUID to the AMI node name. No queue
is matched by its title, dial alias, caller number, or cross-server Linkedid.

Snapshot visits optionally include `channelId`, `channelName` and `caller`.
The channel ID is the ARI channel ID/Asterisk unique ID, not the queue ID. A
current visit is attached to a live AMI channel only when node, context, unique ID
and (if supplied) channel name agree uniquely. The AMI channel's existing Linkedid
then supplies the related legs. Ambiguous or absent channel evidence leaves the
visit uncorrelated; it does not manufacture an endpoint or enable supervision.
The generic channel `Up` label now says "Channel answered", not that an attendant
is conversing: a greeting can already have answered the caller.

## States, filtering and failure

Cards remain in the normal queue section and reuse the existing SUI mosaic and
detail overlay. Context, node, text and activity filters continue to affect the
URL; context is optional for managers. No banner is added above the dedicated
board. Source failures appear inside its compact filter/options menu.

`pending` means the controller's non-connected visits, including greeting,
announcement and offers. `in service` means connected visits. Individual flows
show greeting, waiting, announcement, calling an agent or answered. Counts are
calls/visits, never the number of offered agent legs. Termination removes the visit
on the next complete snapshot while keeping the queue definition.

Only controller timestamps establish freshness: at most 20 seconds old and five
seconds ahead, connected and not observer-only. Old or failed snapshots retain
known queue names but hide callers, phases and counts rather than showing stale
activity or invented zero. A forbidden source response discards that cached
snapshot. One failing source does not suppress working nodes or AMI observations.
Existing resource inventory retains identity only, not visits or call parties.

Old controllers without a `visits` collection are supported: the card shows their
actual counts and the popup explicitly states that exact channel correlation needs
the monitoring update. Queue names alone do not identify the 6599 call.

## Private configuration

Panel 0.12.0 adds `Panel:AcdSources`. These settings and keys are server-side;
browser query parameters cannot change source URLs, credentials or allowed nodes.
Example using the verified Google pilot identifiers (the key path is a deployment
placeholder, not a file created by this change):

```json
{
  "Panel": {
    "AcdSources": [
      {
        "Node": "google-voip",
        "NodeId": "01a091d2-251c-7b12-8c3f-f0529ed1a192",
        "ContextIds": ["d21cfb04-9d37-473b-837c-67591a26feed"],
        "BaseUrl": "http://172.19.254.250:18189/",
        "KeyFile": "/etc/sufficit-telephony-panel/acd-google-monitor.key"
      }
    ]
  }
}
```

Only literal loopback or Sufficit VPN IPs are accepted; redirects, public hosts,
userinfo and query strings are rejected. A key file must be private (0600 or a
controlled 0640 group), without world read/write or group write permission. The
reader issues only fixed GET snapshot requests. Maximums: 16 source declarations,
64 context/node slots, four concurrent requests, 1 MiB per response, three seconds
per HTTP request, five seconds overall, two-second shared cache. It uses no
per-tab AMI connection, unbounded historical replay or raw event archive.

## Controller update and credential

ACD 0.10.1 adds visit channel identity and optional `ACD_MONITORING_KEY_FILE`.
Provision a distinct random secret in private files on the controller and panel.
This identity can only GET assigned node/context snapshots; management routes and
all mutations return 403. Management credentials remain separate. When the new
environment variable is absent, existing management authentication is unchanged.
Never put the management key in browser code, logs or this repository.

The additive snapshot changes require no SQL schema change themselves. However,
the local ACD 0.10.1 source includes the previously unshipped 0.10 queue-operation
work, whose migration 007 and runtime prerequisites still apply. Do not replace
Google's older controller with this entire build without the approved rollout and
drain procedure. No Asterisk restart, AMI permission expansion or dialplan changes
are required by this monitor integration.

## Initial local verification

On 2026-09-12, Google's private API returned a connected snapshot for the expected
context/node, seven shared queue definitions including `Homologação Sufficit —
6599`, and zero active visits at sampling. It did not include `visits`, confirming
the legacy compatibility path. This was a read-only authenticated query, not a new
test call and not evidence of the frontend deployment.

Local builds and tests cover controller export, read-only monitoring credentials,
source isolation/cache/failures, queue projection, phases and browser interactions.
No live configuration, credentials, services, routing, deployment, commit or push
was changed. Production activation still needs protected source configuration and
the reviewed controller rollout for full channel correlation. Physical audio and
agent-dispatch acceptance remain outside this monitoring change.

## Published 2026-09-12

Panel 0.12.0 and Google controller 0.10.1 are now deployed. The Google snapshot
source is configured with a separate read-only credential; no Asterisk restart or
dialplan change was performed. The full runtime required additive schema 007 on
the isolated shared ACD database; dispatch remains disabled on Google. The actual
panel reader accepted seven queue definitions, including the 6599 pilot, and the
new visits export while running as the service user. See the
[deployment evidence and rollback](activities/202609122003-deploy-acd-board.md).
