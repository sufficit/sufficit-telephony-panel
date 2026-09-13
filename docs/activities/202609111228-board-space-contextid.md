# Shared sidebar height and contextid URL filter

## Request and diagnosis
The supplied screenshot showed six queues scrolling while room remained on a
large display, with an empty trunk section. Dedicated CSS independently capped
each sidebar list at half the viewport. Company filtering wrote `company` rather
than the Sufficit `contextid` convention.

## Changes (0.10.3)
- Desktop sidebar now shares a viewport height budget using natural-height flex
  sections. Empty/short trunks leave space for queues; both scroll internally only
  when their combined content exceeds available space. Headers remain visible.
  Mobile lists use natural height rather than the old independent half-screen cap.
- BoardViewQuery uses ContextId; generated URLs write compact `contextid` and remove
  `company`. Legacy links remain readable and normalize with history replacement.
  Canonical contextid takes precedence if both exist. Invalid/duplicate canonical
  values do not fall back to a different legacy company. Existing invalid-filter
  defaults and all server-side access checks remain unchanged.
- Selection, clearing, reload, shared links, back navigation and login return URL
  retain their contracts. No company data, permissions or API payloads were changed.
- Operator-board guide, design, development instructions and deploy guide updated.
  SUI conventions retained; skills guided geometry verification and URL regression
  coverage. Mechanical layout scan returned no findings before and after; source
  and screenshot inspection identified the actual half-height restriction.

## Validation
- Serial dotnet build: zero errors/warnings. Test executable: 544 assertions passed.
- Nine browser suites passed: board-space-context, board-dialog, board-only,
  board-page, board-language, operator-board, board-settings, board-inventory,
  call-grouping. New checks cover legacy normalization, contextid precedence,
  select/clear/reload/share, six queues without scrolling, short-trunk/long-queue
  allocation, both lists crowded, taller monitors, mobile and fullscreen.
- Screens inspected at 1994x800 and 390px:
  .impeccable/review/board-space-{six-queues,crowded,mobile}.png.
  Stress cases clone development fixture tile markup only for layout measurements;
  no fabricated resource was sent to any API or persisted. No browser page errors.
- Release publish and git diff --check passed. No commit/push requested. Unrelated
  worktree edits preserved; local development listener stopped after validation.

## Eveo delivery
Release: /opt/sufficit-telephony-panel/releases/board-space-20260911-1228.
DLL SHA256: 264a3d030851c2da222e2d732bca140c6a6baf8b5a379bb442448e6046b9aa2f.
Panel active, PID 3568589, NRestarts 0. Only this panel service was restarted.
AMI-events PID 2619544 and observation PID 2618869 remain active and unchanged.
Rules SHA256 unchanged:
3714a5062515639e831067c9d6ee159e2657393d07acd6effb82999ab1c75a43.
Known-resource inventory remains 0600, owner sufftelpanel, 313813 bytes at check.
Private appsettings copied inside the host without exposing secrets. No backup
files or writes to rules/inventory. No Asterisk, nginx, Identity or AMI changes.

Production root, contextid board URL and health return 200; anonymous API returns
401. Shared-height stylesheet is served and old dedicated half-height rule is
absent. Identity callback and PKCE unchanged. Browser confirms protected entry and
contextid preserved in the login return URL. Scoped post-deploy log scan found no
fail/crit/unhandled-exception records.

## Limits
Authenticated filtering and layout interactions used development fixtures, not a
live user session. Public production checks prove deployment/access boundary, not
live customer inventory completeness. No telephone action or notification sent.
