# Focused operator board and bilingual interface

## Request and starting state
The user requested English source/routes, a fullscreen view containing only the
operator board, and English/Portuguese UI with English default and localStorage
preference. Version 0.9.0 served `/mesa`; fullscreen targeted `documentElement`,
including unrelated panels. UI strings were Portuguese-only.

## Delivery
- Version 0.10.0, canonical `/board`. `/mesa` returns 301 preserving query filters.
- Fullscreen targets `board-focus`: compact toolbar, filters, resource mosaic and
  selected-resource inspector. Global navigation, settings, retention controls,
  general notices and help remain outside fullscreen. Exit is inside the target;
  unsupported browsers keep the dedicated page and receive a useful message.
- English source text and translation keys, Portuguese resource dictionary,
  per-circuit `PanelText` service. English default, explicit SUI language picker,
  `telephony-panel-language` localStorage key (`en` / `pt-BR`), HTML lang update.
- Home, live operations, board, settings, state labels, confirmations and errors
  translated. Customer/resource names and PBX identifiers are deliberately not
  translated. Known legacy status labels are adapted at presentation boundaries.
- URL filter behavior, rule drafts, retained inventory and access checks preserved.
- Product, design, development and deployment instructions updated. SUI components
  and theme tokens preserved; no new frontend framework or browser dependency.

## Validation
- `dotnet build tests/Panel.Tests.csproj -m:1 -v:q -nodeReuse:false`: zero errors or warnings.
- `dotnet tests/bin/Debug/net10.0/Panel.Tests.dll`: 534 assertions passed, including
  translation placeholders, circuit isolation, English fallback and safe return URL.
- Browser suites passed: board-language, board-page, operator-board, board-settings,
  board-inventory and call-grouping. Existing suites explicitly select Portuguese;
  the new suite checks English by default, switching, reload persistence, isolated
  sessions, redirect/query preservation, actual fullscreen target and exclusion of
  unrelated panels, filters within fullscreen, desktop/mobile and light/dark.
- Screens inspected under `.impeccable/review/board-focused-{en,pt-dark,mobile}.png`.
  No browser page errors or horizontal mobile overflow.
- Initial harness resize failed because Chromium was still leaving fullscreen.
  Mobile validation now uses a separate window; no product workaround was added.
- `git diff --check` passed for affected tracked files.

## Eveo deployment
Current release: `/opt/sufficit-telephony-panel/releases/board-focus-20260911-1144`.
Assembly SHA256: `9836963d6ad7a43b586a9981bce8beec63eaed890bea50930cae47353e8c45b1`.
Panel PID: 3531214, active, NRestarts 0. Only the panel service was restarted.
The initial 1140 release was superseded by the final header language-selector fix.
Private host appsettings were preserved without reading or printing credentials.
No nginx, Identity, Asterisk or collector changes/restarts were made.

AMI observation PID 2618869 and AMI-events PID 2619544 are unchanged.
Board rules SHA256 remains
`3714a5062515639e831067c9d6ee159e2657393d07acd6effb82999ab1c75a43`.
Known-resource inventory remains mode 0600, owner sufftelpanel (307828 bytes at
verification). Neither rules nor inventory were overwritten or cleared.

External checks: root and board 200; health 200; anonymous fleet API 401; `/mesa`
301 with query preserved; old prefixed links still redirect. Identity login 302,
PKCE S256 and existing `/telephony-panel/signin-oidc` callback unchanged.
Production browser verified English anonymous entry, Portuguese switching and
reload persistence, no fixture tiles and no page errors.

## Limits
Authenticated board interaction was exercised with development fixtures, not a
live manager session. Public production checks do not prove live customer-data
rendering. No telephone actions or customer notifications were performed.
No commit/push requested; unrelated existing worktree changes were preserved.
