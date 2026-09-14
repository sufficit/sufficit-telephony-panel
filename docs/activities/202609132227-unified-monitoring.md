# Unified queue and registration overview

2026-09-13 22:27 America/Sao_Paulo

## Request and starting point

Remove visible server/context partitioning from the recently migrated monitoring
screen. `/queues` repeated a complete queue and registration table for each source.
The requested default is one overview, with optional filters and diagnostic origin
details, not mandatory server/context selection or additional granular dashboards.

## Changes

- Added `AcdMonitorOverview`, a presentation-only aggregation of authorized source
  samples. A shared queue/extension is not duplicated merely because multiple
  nodes observe it. Internal context identity prevents unrelated resources with
  similar names being incorrectly combined.
- One queue list, one observed-call disclosure and one registration list replace
  the repeated source sections. Collection/source details are collapsed.
- Queue workload combines current node readings; shared catalog capacity is not
  added. Missing sources produce explicitly partial lower bounds, while entirely
  stale readings show dashes. Cross-server legs are not claimed as unique callers.
- Registration details combine source-tagged history chronologically. Conflicting
  current states show Different readings, not invented global availability. Missing
  and historical evidence remain explicit. Retention remains per source.
- Context, node, text and queue deep-link filters retain their existing URL behavior.
  No selection is required on `/queues`. Management stays in Blazor; the dedicated
  mosaic and infrastructure page are unchanged.
- Reused SUI fields/tables/theme and English/Portuguese localization. Expanded
  mobile diagnostics use the full row width. Version advanced to 0.13.1.
- Updated the source fixture, regression tests, surface brief and ownership docs.

The development skill supplied the checkpoint/report workflow. Sufficit frontend
and Impeccable distill/polish preserved the visual identity while replacing the
repeated information structure and validating responsive diagnostic disclosure.

## Validation

- Debug build and Release publish succeeded with no errors.
- `dotnet run --project tests/Panel.Tests.csproj`: 787 checks pass.
- `NODE_PATH=/tmp/pwrec/node_modules node tests/monitoring-migration.browser.cjs`:
  pass. Synthetic fixture spans two live sources sharing resources and one missing
  source. Assertions cover one queue/registration table, shared-row identity,
  chronological history, refresh expansion, optional server selection, URL reload,
  text/IP search, GUID source selection while unavailable and unchanged board-only
  rendering. English desktop and Portuguese dark mobile captures reviewed; no
  horizontal overflow, mobile detail width above 270px and no console errors.
- An initial new test reused the same node ID for a missing source and a healthy
  source; correcting that fixture verified actual distinct-source partial counts.
- `git diff --check` clean. Local preview processes were explicitly identified and
  stopped; no broad process matching was used to terminate services.

## Deployment

Only Panel deployed on Eveo Apps through the existing guarded release switch:
`deploy/release-panel.py unified-monitoring-20260913 monitoring-20260913-2130`.
Release `/opt/sufficit-telephony-panel/releases/unified-monitoring-20260913`.
Previous release retained for rollback. Local and deployed DLL SHA256:
`cdc958cfd95bd46a48126f38e0c154a983d1180c7a19ff0f737c6deb9121d6a3`.

Panel PID 2214560, NRestarts 0, public health running and anonymous API 401.
All five existing service drop-in hashes were unchanged. The real Panel Unix user
still reads the Google diagnostic facade successfully (two approved identities).
No Asterisk, ACD controller, collector, Identity, nginx, routing or database change
was performed in this task.

## Limits and references

UI validation used visibly synthetic local fixtures, not an authenticated production
browser session. Source configuration and identity coverage are unchanged; this
does not claim discovery of every extension on every PBX. Per-source collection
remains local; its presentation is unified. No production calls were placed.

See [monitoring ownership](../monitoring-ownership.md) and the updated
`.impeccable/surfaces/route-queues.md`. Rollback uses the retained previous Panel
release; no PBX restart is necessary. The unrelated older plans were preserved.
