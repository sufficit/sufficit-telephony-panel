# Board context menus and supervision

## Objective and starting state

Add context actions to extension, queue and trunk cards, supporting listening or
whispering to an exact channel or an authorized continuous prefix. The existing
board exposed details and exact-channel actions through its resource dialog, but
lacked card context menus. Existing monitor options restricted spying to bridged
channels and did not explicitly stop an exact target after hangup. The repository
contained extensive pre-existing work, which was preserved.

## Delivered local changes

- Added right-click, visible-button and keyboard card menus using SUI controls.
- Added overlay target selection and confirmation, localized in English and
  Portuguese; the dedicated board retains its URL, filters and scroll position.
- Added private context/node/card-to-prefix rules, concrete prefix validation,
  fresh-observation and context ownership checks alongside existing manager,
  entitlement, supervisor endpoint and cooldown checks. Empty configuration is
  deny-by-default for continuous mode; visual regex rules never authorize audio.
- Added exact `quES`/`quESw` and prefix `q`/`qw` option construction in Standard,
  with a 30-minute supervisor-channel absolute timeout. Waiting/unbridged audio
  is no longer excluded by the options builder.
- Incremented the local panel version from 0.10.4 to 0.11.0.

## Validation

- `dotnet build tests/Panel.Tests.csproj --no-restore -v quiet`: passed, no warnings
  or errors.
- `dotnet run --project tests/Panel.Tests.csproj`: passed 621 checks, including
  protected-prefix isolation and exact/continuous option construction.
- Standard `dotnet build src/Sufficit.Standard.csproj --no-restore -v quiet`:
  passed, zero errors, 469 existing compiler warnings.
- `tests/board-supervision.browser.cjs`: passed desktop/mobile, all three card
  types, keyboard, EN/PT, explicit target confirmation, no duplicate sends, and
  zero telephone API requests in synthetic preview.
- `tests/board-dialog.browser.cjs`: passed resource dialogs, URL/geometry,
  keyboard/focus, backdrop, fullscreen and mobile/PT/dark regressions.
- `tests/board-only.browser.cjs`: passed board-only layout, filter persistence,
  URL reload, fullscreen height and normal-page navigation regressions.
- Four screenshots were opened and checked. Independent impeccable UI review:
  `ship` at the reviewed UI scope. Nonblocking note: Tab dismisses the menu and
  restores its trigger; the following Tab advances. The single detector run's
  output was truncated, so no clean-detector result is claimed.
- Both repositories passed `git diff --check`. The local preview used only
  127.0.0.1:5169 and was stopped by its explicitly verified PID after testing.

## References and limits

See [Board supervision](../board-supervision.md) for the operator workflow,
synthetic configuration example, authorization invariant and deployment dependency.
The post-review scoped design record is `.impeccable/board-supervision.md`; global
design documents were not changed by this task. The temporary task plan was closed
into this activity report after verification.
Standard details are in
`sufficit-standard/Documentation/202609120916-monitor-supervision-options.md`.

No deployment, package publishing, commit, push, PBX restart or customer telephone
call was performed. The API consumer must receive the Standard update as well as
the panel update. No live continuous-prefix mappings were enabled. Real SIP/RTP,
microphone direction and continuation between actual calls remain unverified;
an accepted Originate response is not proof of audio. Continuous monitoring visits
one matching channel at a time, not all calls mixed together. Closing the dialog
does not stop an accepted telephone session; the supervisor must hang up. Queues
need verified dedicated channel mappings; shared multi-context trunks must not
receive an unrestricted continuous-prefix rule.
