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

Google now supplies history for approved Sufficit extensions6001/6007 through an
isolated private snapshot facade; Eveo/Apoint each supply four test identities.
Coverage is explicit, not every PBX endpoint. Google remains on its original0.9.0
controller without restart or dispatch changes. The facade merges diagnostic agents
without altering its queue snapshot. SIP registration may differ from PJSIP aliases:
6007 was observed registered using chan_sip, with a declared CiscoCP8941 User-Agent.
Unknown busy state is not equivalent to available. Missing registration evidence
for6001 remains unknown, not a fabricated offline state.

For legacy SIP, contact IP is the peer's current socket address; expiry/Via may be
absent. Its internal scheduler identifier is NOT renewal evidence and is excluded
from history. Only observed state/IP/device changes are retained; identical SIP
renewals cannot be reconstructed. PJSIP expiration changes remain observable.

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

Google diagnostics use `deploy/80-google-registration.conf`; its private key path
is `/etc/sufficit/telephony-panel/google-registration-monitor.key`. The guarded
`deploy/enable-google-registration.py` checks the authenticated fixed snapshot,
denied writes/resources, and all three sources as the actual Panel Unix user before
changing its drop-in. Only Panel restarts to load this configuration. On health
failure the new drop-in is renamed and the previous source restored.
