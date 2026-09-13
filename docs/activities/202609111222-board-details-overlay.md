# Dedicated board resource popup

## Request and diagnosis
The user requires the dedicated /board tab to remain a television-style mosaic,
including after a resource is selected. Version 0.10.1 appended board-inspector
below the grid and FocusAsync scrolled the document to it. Removing the initial
page header had not solved this interaction.

## Changes
- Version 0.10.2. Dedicated resource details use BoardResourceDialog, a local native
  top-layer dialog with SUI buttons/theme tokens. The shared SUI service dialog
  host was inspected; the local wrapper preserves live RenderFragment updates
  and native fullscreen stacking without changing the shared library.
- Extension, trunk and queue clicks keep document geometry, grid scroll and URL
  unchanged. Details scroll inside a bounded popup. Its header/close action stay
  visible. Native modal semantics contain keyboard focus; Escape, close button and
  backdrop dismiss and return focus to the selected resource without scrolling.
- Channel supervision confirmation and action status remain inside the dialog.
  Existing server-side permissions, target validation and explicit confirmation
  are unchanged. No new telephone action or external notification was introduced.
- Normal-page and settings links from dedicated options open a separate tab with
  noopener and a bilingual title. The board tab keeps its URL and observation.
- Normal root page retains inline details. Product/design/deploy docs updated.
- Mobile channel table has local horizontal scrolling and non-wrapping action
  labels; initial screenshot exposed letters wrapping individually, corrected
  before publication and verified in the final confirmation round.
- Software-development, Sufficit Frontend and Impeccable guided the scoped fix,
  preservation of SUI conventions and browser evidence for opened interactions.

## Validation
- dotnet build tests/Panel.Tests.csproj -m:1 -v:q -nodeReuse:false: zero errors or warnings.
- Panel.Tests: 539 security, concurrency, revocation, projection and action assertions.
- Eight Playwright suites passed: board-dialog, board-only, board-page,
  board-language, operator-board, board-settings, board-inventory, call-grouping.
- New dialog suite checks all three resource types, real modal state, no inline
  inspector, geometry/scroll/URL invariance, Enter/Escape/Tab, focus return,
  confirmation containment, backdrop close, fullscreen, mobile Portuguese/dark.
- Existing board-only test now verifies separate-tab normal-page navigation.
- Screens inspected: .impeccable/review/board-trunk-dialog.png,
  board-channel-dialog.png and board-dialog-mobile.png. No browser page errors
  or horizontal page overflow. Interactions use isolated development fixtures.
- Release publish and git diff --check passed. Existing unrelated changes remain;
  no commit/push requested. Local fixture server stopped after validation.

## Deployment and production checks
Only sufficit-telephony-panel on Eveo was restarted.
Release: /opt/sufficit-telephony-panel/releases/board-dialog-20260911-1221.
DLL SHA256: ec5d8511c1476803acfaff98ee0b2e50acb377912a4e585e7450a04bd1bfeb4b.
Dialog JS SHA256: 4098638320b4470737142d6ead9ad8e7cd6c0727ebf47d6480b7ab9d99f1de11.
Panel PID 3562734, active, NRestarts 0; Unix-socket health passed after startup.
AMI-events PID 2619544 and observation PID 2618869 unchanged and active.
Board-rules SHA256 unchanged:
3714a5062515639e831067c9d6ee159e2657393d07acd6effb82999ab1c75a43.
Inventory remains 0600, sufftelpanel, 313813 bytes at verification.
Private appsettings copied within the host without printing credentials. No backup
file created, state cleared or overwritten. No nginx, Asterisk, Identity, AMI or
checkout changes/restarts.

Public root/board/health: 200; anonymous fleet API: 401. Deployed JS hash matches
local artifact and dialog CSS is served. Identity redirect/PKCE and existing
/telephony-panel/signin-oidc callback unchanged. Production browser confirms login
boundary and no demo/resources exposed even with preview=true. Scoped log check
found no fail/crit/unhandled-exception records.

## Limits
Live authenticated customer interaction was not tested: browser resource/confirmation
tests used fixtures. Public checks verify publication and access boundaries, not
actual PBX data. No real call was placed, listened to or modified.
