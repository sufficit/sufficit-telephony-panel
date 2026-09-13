# Monitoring consolidation and Google diagnostics

2026-09-13 19:50 America/Sao_Paulo

## Objective / starting point

Commit and push the approved monitoring migration, then expose bounded registration
history from Google without changing production Asterisk or its legacy controller.
Panel0.13.0 was already deployed; Blazor still had uncommitted migration changes.

## Delivery and decisions

- Migration committed/pushed: Panelddab3f2, Blazor1e525d6, prior ACD4ec3a43.
- Initial BlazorCI34786635082 caught an obsolete sidebar expectation. Fixed through
  bd729ff; replacementCI34786992287 completed successfully, including build and all
  three deploys (Eveo, Apoint, Castrum). Shared package34786992269 also succeeded.
- Google source0 now uses a private fixed snapshot facade through
  `deploy/80-google-registration.conf`; sources1/2 remain unchanged.
- `deploy/enable-google-registration.py` prevalidates credentials/resource rejection
  and all source contracts as the real Panel Unix user before switching. Health
  failure rolls back only the new drop-in. Secrets stay outsideGit and output.
- Google legacy0.9.0 controller is untouched: no unrelated current-main upgrade.
  Dedicated collector/facade provide only approved context6001/6007 diagnostics.
- Monitoring remains exclusively in Panel; Blazor keeps queue/destination/member
  configuration. No new UI component or parallel Blazor monitoring was added.

## Evidence

- Panel766 checks; Blazor95 focused checks and4 encoding/sidebar checks passed.
  ReplacementCI validates the final sidebar follow-up.
- Collector11 and facade2 tests pass. Backend integration336 checks each on.NET10
  and real.NET7 pass; that candidate binary was not activated onGoogle.
- Panel Unix user validated Google/Eveo/Apoint snapshots:7 queues each;2/4/4 agent
  records. All histories populated; forbidden resources404, POST501, missingkey401.
- Google6007 registered/reachable, numericIP and device present;6001 unknown. Missing
  busy evidence does not imply an available agent. Corrected false scheduler-ID
  renewals; repeated unchanged polling leaves stable histories.
- Only Panel restarted for the new source:PID2014851 to2081795. Public `/health`
  reports running; unauthenticated `/api/fleet` returns401.
- Public Blazorhealth reportsHealthy, version1.26.0913.2233+bd729ff onEveo.
- Legacy monitor/panel URL returns302 toPanel `/board`, preserving only normalized
  `contextid`. Monitoring ownership and rollout contract updated.
- Google Asterisk23098/controller28091, binary/config hashes unchanged. Backup
  `/var/backups/sufficit-google-registration-20260913224235` preserves invariants and
  the experimental journal corrected during validation.

## Delivered references / limitations

Open `/queues?contextid=d21cfb04-9d37-473b-837c-67591a26feed` onPanel using an authorized
manager session. Expand agent diagnostics for observed contacts/history. API reader
and live service integration were verified; no authenticated browser session or
new production calls were used during this rollout. No claim of audio revalidation.

History is limited to configured identities,20 changes/seven days; no older data
can be recovered. Legacy SIP identical renewals are not observable through this
CLI, unlike PJSIP expiration changes. User-Agent is self-declared, not verified
hardware identity. See [`monitoring-ownership.md`](../monitoring-ownership.md).

Rollback: rename/remove only80drop-in and restartPanel, restoring oldGoogle source.
Do not restart Asterisk or its unchanged controller. Google service keys remain
private; no shared identity tokens were required or changed.
