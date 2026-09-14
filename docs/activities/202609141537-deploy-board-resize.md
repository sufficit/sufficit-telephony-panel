# Board resizing deployment

## Objective and isolation

Publish draggable extension/sidebar and trunk/queue separators with browser-local
preferences to Eveo Apps only. The starting service was healthy, running
`unified-monitoring-20260913`, PID 2214560, without automatic restarts.

The working tree also contains unfinished customer entitlement/access work. That
work was explicitly excluded: build source was archived from Panel HEAD
`0ba84b3` and only resizing changes were applied. Mixed customer hunks in
OperatorBoard and Portuguese translations were excluded. Version is **0.13.2**.
Three clean project dependencies were archived independently after symlink-based
restoration failed due to duplicate NuGet assets. The subsequent publish passed.

Source/build staging is `/tmp/panel-resize-release.oZU9LZ`; it is temporary, not
a substitute for committing the approved source. No commit/push was requested
or performed. Pending customer work remains unchanged and undeployed, except
the shared project version now records this release.

## Delivered

- Release: `/opt/sufficit-telephony-panel/releases/board-resize-20260914-1538`.
- Current symlink points to that release; service PID 3038585, NRestarts 0.
- Panel DLL SHA256:
  `aefb6bc294d421eaedadcdd1cab3f2d94df9fc5ee53c1858edd0ddf151197d9d`.
- Entire published file-list/hash digest matched local and remote:
  `a795014a9afbee6c0cbc002e96d3911920e6cd5aa4f40a85a9ed9f120763214b`.
- Fingerprinted public assets: `board-layout.cm6emlxtwp.js` and
  `panel.mlrnjldufw.css`; both public response hashes match the tested files.
- Dragging, keyboard resize, reset, mobile layout and
  `telephony-panel-board-layout-v1` localStorage persistence are included.

## Verification

- Isolated `dotnet publish ... -c Release -m:1`: succeeded.
- Isolated test harness: **790 checks passed**.
- `board-resize.browser.cjs`: passed dragging, persistence, reload, filters,
  keyboard/reset/cancel, minimum sizes, mobile and unavailable storage.
- `board-only.browser.cjs`: passed dedicated layout, keyboard, filters, URL
  reload, fullscreen height and normal-page navigation.
- `board-space-context.browser.cjs`: passed canonical contextid/legacy handling,
  sidebar natural/shared height, crowded layout, resize, mobile and fullscreen.
- Public `/health` 200 and running; `/board` 200; anonymous `/api/fleet` 401.
- No error-priority Panel journal entries in the checked five-minute window.
- Panel configuration and systemd drop-in combined hash unchanged:
  `6aa48cca59c4d925a38d7c04a4ae5c8669eea8a20f0477ad69c08e4355a94848`.
- Nginx PID stayed 394. Only Panel was restarted by the release helper.
  No Asterisk, Identity, telephony backend, nginx or portal update was performed.
- Local published preview process PID 2676183 was stopped after validation.

The browser tests used local development fixtures against the exact published
artifact. No authenticated live customer session or real call was impersonated.

## Rollback

Previous release `unified-monitoring-20260913` is retained. Protected manifest:
`/var/backups/sufficit-telephony-panel/board-resize-20260914-1538/release.json`.
`deploy/release-panel.py` performed an atomic symlink switch, restarted only
`sufficit-telephony-panel`, and passed its Unix-socket health gate; its automatic
rollback was not needed. Manual recovery must verify current/previous paths,
switch the symlink to the retained previous directory and restart only Panel.
Configuration and remembered board resources remain outside the release.

Public board: https://panel.sufficit.com.br/board .
