# Telephony monitoring consolidated in Panel

Date: 2026-09-13, America/Sao_Paulo.

## Objective and starting state

The user requested no telephony monitoring in sufficit-blazor and migration of
that responsibility to Panel. Panel started at 0.12.2 with operational board,
call/channel projection, infrastructure history and Google ACD queue snapshots.
The preceding uncommitted Blazor diagnostics implementation was retained as
context, then moved to Panel rather than published twice as competing monitors.
Existing unrelated plans and user changes were preserved. No commit/push requested.

## Delivered changes

- Panel 0.13.0 `/queues`: source/context/node/text/queue URL filters, queue stages,
  caller details, current agent state, contact IP/Via, declared User-Agent and
  bounded registration history. EN default/PT-BR, inherited SUI, responsive tables,
  accessible native disclosures retained during polling, stale/error truth.
- Reader validates context/node, duplicate identities, numeric IPs, contact/history
  bounds and 4 MiB response limit. Configured node identity survives source failure.
- Normal-area navigation preserves filters. `/board` stays mosaic-only. Added
  latest-observed-event-per-resource tab, explicitly not a full raw AMI event log.
- Blazor monitoring pages/widgets and automatic event registrations removed;
  fixed-origin legacy redirects preserve only bounded presentation filters. Queue
  and member management, destinations and SIP configuration stay in Blazor.
- Removed client EventsPanel settings load and public settings export (404).
  Replaced the background registry client with a one-shot explicit SIP reload
  command: authenticated connection, 15-second budget, disposal, normal TLS,
  no observation subscriptions or reconnect. This command was not executed.

## Source rollout and safety

The Panel service user validated all three private sources: Google 7 queues/0
agent observations, Eveo 7 queues/4 observations and Apoint 7 queues/4 observations.
Counts reflect this verification, not a full PBX inventory or future guarantee.
New test-source keys are private root:sufftelpanel 0640 files on Eveo Apps; contents
were not logged, committed or exposed to browsers. Monitoring keys return 200 for
snapshot and 403 for management reads/writes.

Only idle Eveo/Apoint test controllers restarted to enable these read-only keys.
An initial Eveo self-VPN probe hit the existing proxy source ACL and rolled back.
The corrected probe used the private Unix socket; no ACL or AMI privilege was
widened. Final Asterisk PIDs remained Eveo219/Apoint2360674. Google was untouched.
No calls, DIDs, real members, destinations, queue policies or routes were changed.

## Validation

- `dotnet run --project tests/Panel.Tests.csproj`: 766 checks pass.
- Blazor `dotnet test tests/Sufficit.Blazor.Acd.Tests.csproj --no-restore --nologo -m:1`:
  95 pass; full Blazor Server build passes.
- Panel Release publish succeeds; scoped source probe succeeds as sufftelpanel.
- `tests/monitoring-migration.browser.cjs`: PASS including URL search/reload,
  disclosure retention through polling, EN/PT, desktop1440/mobile390, unavailable
  GUID-filtered source, scoped area navigation and mosaic-only board.
- Opened complete light desktop/dark mobile fixture captures. First capture set
  raced the disclosure layout; recaptures waited for settled full-page geometry.
  Detector ran once with no findings. Independent review requested three material
  fixes (settings/client removal, unavailable node identity, URL preservation),
  cleared all three and returned ship. No general redesign or global tokens changed.
- Public production browser: `/queues?preview=true` still requires Identity,
  preserves return path and cannot render the Development-only synthetic data.
- Public HTTP: six legacy routes return safe302 Panel links with context/node/q;
  synthetic token/returnUrl parameters are not forwarded. Settings URL returns404.
  Queue management remains on its normal Blazor authentication path, not Panel.
- Artifact SHA256s match local publish and active server files; health checks pass.

## Deployment and rollback

Panel deployed first using `deploy/release-panel.py` to Eveo Apps:

- New `/opt/sufficit-telephony-panel/releases/monitoring-20260913-2130`.
- Previous `/opt/sufficit-telephony-panel/releases/idle-prefix-20260912-2055` retained.
- Manifest `/var/backups/sufficit-telephony-panel/monitoring-20260913-2130/release.json`.
- DLL SHA256 `a68a882147f3ad75c5bcbc82508a2eb2583497ee3efd622a18fdd17901fefb2f`.
- Additional source configuration: `70-acd-labs.conf`; existing Google60 drop-in
  unchanged. Release rollback does not roll back source drop-ins automatically.

Blazor deployed with its official root command `python3 deploy.py eveo-apps --publish`.
Fresh artifact `server/publish-20260913182634`, public Healthy version
`1.26.0913.2126+bd3d9ec1beda328e5d0e7b2503667aeb4fe348e0` on Eveo Apps.
Separate private pre-deploy backup retained at
`/var/backups/sufficit-blazor-monitoring-20260913-2128.tar.gz` (0600), because the
official script removes its transient `.prev` after successful startup.
Server DLL SHA256 `4e06064c3a768dc6abee3b9ad8f08116747b8a072212fcd060905556336b319a`;
UI DLL SHA256 `73010b1980b6cd38e6e04e594e624243363b13aaa463e463791474cc3324e25a`.

Only the Panel and Blazor web services restarted during application deployment.
The prior release files/backup can recover removed portal UI; no source deletion
was committed in this task. Local browser session and preview process were closed.

## Limits and references

- This is a dirty-tree manual deployment: changes are not yet committed/pushed.
  A later clean-main CI deployment can replace it. Do not call it permanent/stable
  until intended changes are committed and the deployed version is reverified.
- No authenticated end-user production session or real SIP reload acceptance test.
- History covers configured test identities, max20 observed changes/7days; not
  exact SIP REGISTER timestamps or retroactive reconstruction. User-Agent is a
  declaration, not verified equipment. Google agent-history rollout remains separate.
- Full raw historical AMI payload browsing is not supplied by the bounded projection.
- Contracts: `docs/monitoring-ownership.md`, `.impeccable/surfaces/route-queues.md`.
  Dedicated diagnostics: https://panel.sufficit.com.br/queues .
