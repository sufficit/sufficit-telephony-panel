# Telephony monitoring ownership

Sufficit Telephony Panel owns user-facing telephony observation. Sufficit Blazor
owns configuration and management: queues, members, destinations and SIP settings.
Do not add live telephony widgets, AMI/event subscriptions or diagnostic history
back to Blazor. Billing and business call records are not part of this migration.

## Routes

| Panel route | Responsibility |
| --- | --- |
| `/board` | Dedicated mosaic only; details remain overlays or separate tabs |
| `/?tab=extensions` | Observed endpoints/extensions |
| `/?tab=channels` | Individual channels; not necessarily distinct calls |
| `/?tab=events` | Latest observed event per resource, not a complete raw AMI log |
| `/queues` | Private ACD queue stages, agent presence and registration diagnostics |
| `/?area=infrastructure` | Existing PBX metrics and bounded infrastructure history |

Legacy `/pages/telephony/monitor/*` links on Blazor redirect to fixed Panel routes.
Only bounded `contextid`, `queueid`, `node` and `q` presentation filters are copied;
legacy `context`, `nodeid` and `filter` are translated. Credentials and return URLs
are never forwarded. Manager authorization is rechecked by Panel.

## Queue diagnostics

`/queues` supports optional `contextid`, `node`, `queueid` and `q` filters. Changing
monitoring area preserves context/node/text filters. A node filter limits observed
sources, never where a queue may run. Configured node IDs remain available even
when a source cannot produce a snapshot. English is the default; Portuguese and
theme preferences follow the existing local browser settings.

Snapshots use server-only read credentials, context/node shape validation, a
4 MiB response bound, shared two-second cache and five-second UI refresh. Current
agent readings expire after 15 seconds; stale/unavailable readings are not shown
as available agents. Expanded contact/history details stay open during refresh.

The history is a bounded journal of **observed changes**, not exact SIP REGISTER
message times. Each identity keeps at most 20 changes over seven days. Contact IP,
Via IP, declared User-Agent and expiry may be absent. User-Agent is self-declared;
it does not verify a hardware model. No historical records can be reconstructed
before collection began. No recording or call audio is stored by these diagnostics.

At the initial migration, Google supplies queue snapshots without agent history;
the approved Eveo/Apoint test controllers supply presence/contact/history. Source
coverage is limited to identities configured in their collectors, not every PBX
endpoint. Extending Google history requires a separately validated collector change.

## Service boundaries

Blazor no longer loads or exports EventsPanel settings or starts its event services.
Its former settings URL returns 404. Explicit SIP configuration reload remains a
one-shot authenticated management command with a 15-second cancellation budget,
normal TLS validation, no reconnect loop and no status subscription. It must never
run automatically when opening the configuration page.

Deployment order is Panel first, then Blazor legacy redirects. Roll back the Panel
release pointer if its health fails; source configuration is independently managed.
The additional lab source drop-in is `deploy/70-acd-labs.conf`. Never publish its
private key contents. No Google/Asterisk restart is required for this migration.
