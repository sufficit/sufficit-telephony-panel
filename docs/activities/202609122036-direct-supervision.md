# Direct listening for a single supervision target

## Request and starting state
The user reported a missing protected-prefix error for extension cards and asked
to listen directly when a card has one target, e.g. SIP/0000006001-xxxxxxxx.
The dialog always started with an empty selection and continuous mode accepted
only manual mappings. Portal endpoint keys actually contain a leading `^`.

## Changes
- Concrete SIP/PJSIP endpoint-prefix candidates come only from the PEER card's own
  fresh, context-identified channels and exact endpoint key. Related call-flow
  legs, display labels, wildcards, shared trunks and queue names cannot grant them.
- The existing portal `^SIP/<endpoint>` representation is normalized by removing
  only its leading anchor and comparing literal identity, never executing a regex
  as an authorization decision. The channel delimiter remains part of the prefix:
  SIP/0000006001- does not match SIP/00000060010-.
- The server re-queries context-scoped portal cards before originating and requires
  a PEER card with that exact endpoint. Context, fresh observation, conflicting
  accounts, manager/telephone entitlements, preferred supervisor and action gate
  remain enforced. Explicit routing mappings remain necessary for trunks/queues.
- The explicit Listen menu click starts once when exactly one eligible prefix or
  channel exists. No redundant selector/confirmation. Multiple candidates still
  require selection. Whisper preselects a single target but retains confirmation.
- The existing SUI overlay reports the target and request result; no navigation,
  new styles, browser audio or layout redesign. Closing it does not hang up an
  accepted telephone session. Errors/refreshes do not automatically retry.
- Version 0.12.1; software-development and sufficit-frontend guided the plan,
  existing SUI interaction reuse and desktop/mobile browser verification.

## Validation
- `dotnet run --project tests/Panel.Tests.csproj`: **696 passed**.
- Debug build and Release publish passed; existing NU1510 dependency warning only.
- `tests/board-supervision.browser.cjs`: passed on Debug and published Release,
  including unique prefix/channel auto-listen, ambiguous channel selection, whisper
  confirmation, one-shot submission, keyboard, overlays, EN/PT, mobile/dark. Fixtures
  sent **zero** real telephone API requests.
- `tests/board-dialog.browser.cjs`: passed dedicated-board dialog regression.
- Inspected `.impeccable/review/supervision-prefix-mobile.png`; no selector remains
  for the single target, result stays above the board without horizontal overflow.
- `git diff --check`: passed. Local preview stopped by its inspected explicit PID.

## Delivery
Only Eveo Apps panel was restarted. Published release:
`/opt/sufficit-telephony-panel/releases/direct-listen-20260912-2030`.
DLL SHA256 `3c36a181db2c10b0338a67ef1be5efa8873db133f2b8a8c2ecc46869577a7124`;
local/remote hashes match. Panel PID 967083, active, NRestarts 0.
Public health 200, anonymous fleet API 401. The existing ACD reader probe still
accepts the Google snapshot with seven queues and visit export.
AMI observer PID stayed 3700714; Google Asterisk 23098 and ACD controller 28057
remain unchanged. No dialplan, Identity, queue options or PBX changes.

Rollback manifest:
`/var/backups/sufficit-telephony-panel/direct-listen-20260912-2030/release.json`.
Previous release `acd-board-20260912-2010` remains intact. Switch `current` back
atomically and restart only sufficit-telephony-panel; preserve all private drop-ins
and external state. `deploy/release-panel.py` provides expected-current validation
and automatic application rollback when its health check fails.

No real supervisor call, SIP/RTP audio or authenticated user action was performed.
The upstream Standard API was not redeployed; its previously documented exact
termination/unbridged-audio acceptance remains separate. This change removes the
panel's redundant selection and static-only endpoint restriction; an accepted
request still requires answering the preferred telephone. No commit/push.
