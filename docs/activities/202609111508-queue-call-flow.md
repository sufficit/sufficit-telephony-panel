# Queue call flow — 2026-09-11

## Request and diagnosis

Extension 6007 successfully called Atendimento, but the board showed no queue
activity and duplicate channels with an unknown destination. Inspection established:

- EventsPanelRuntime generates queue-card patterns `^Local/{extension}`, whereas
  queue events carry the native queue identifier.
- Queue tiles ignored waiting rows and always displayed unknown activity.
- Empty/unknown connected-line values replaced useful destinations.
- Board tiles rendered raw channel legs rather than linked call summaries.

Google's local Asterisk full log was empty and queue_log was old; the completed
reported call cannot be reconstructed from those files. No real test call was made.

## Changes

- Shared LiveProjection: queue identifier compatibility, exact-channel account
  evidence for queue events, preservation of meaningful caller/destination/Linkedid,
  typed NewConnectedLine and AgentConnect/AgentComplete lifecycle support.
- AuthorizedObservation: retain internal related legs only when anchored to an
  authorized resource and carrying the requested tenant AccountCode.
- BoardCallFlow: node-local Linkedid summaries after presentation rules, queue
  labels, one displayed flow per linked group, raw legs in the existing popup.
- Queue tiles: explicit waiting/answered counts; observed-channel filter includes
  waiting callers. No claim of idle state from absent events.
- English/PT-BR copy, development fixture and regression coverage. Existing SUI
  layout, fullscreen, URL filters, board rules and inventory behavior preserved.

The software-development and Sufficit frontend skills guided focused changes,
live checkpoint tracking, bilingual states, and actual browser validation; no
visual redesign or library migration was introduced.

## Validation

- Serial builds/publishes: zero errors. One observer publish used the panel working
  directory and failed with MSB1009; corrected to the observer repository, succeeded.
- Panel: 572 checks passed, including queue join/leave/abandon/hangup, account/node
  isolation, destination preservation, matching and related channel diagnostics.
- Observer: 28 checks passed, including real typed AMI frames → observer → gRPC
  queue join, connected-line update, AgentConnect and AgentComplete.
- Streaming: 16 snapshot/replay/Unix gRPC checks passed.
- Ten browser suites passed: queue flow, dialog, operator board, call grouping,
  sidebar/contextid, board-only, board-page, language, inventory and settings.
- Queue-flow and dialog suites passed again against the published Release build.
  Screenshots: `.impeccable/review/queue-call-flow-en.png` and `queue-call-flow-pt-BR.png`.
- `git diff --check`: passed in both repositories. No commit/push requested or performed.

## Publication and live checks

- Eveo Apps panel 0.10.4:
  `/opt/sufficit-telephony-panel/releases/queue-flow-20260911-1503`.
  DLL SHA256 `608ffa78b760ca5a7db8671bcf2365eb948015b4cf74f342c612feac309de54a`.
- Observer 0.1.1:
  `/opt/sufficit-ami-observation/releases/queue-flow-20260911-1503`.
  DLL SHA256 `89c6dd9882dc2727632ad20385e1fedda1ee1d08669347840721589ed065a2bb`.
- New observer `current` release pointer and `50-current-release.conf` override;
  existing `/etc/sufficit-ami-observation` configuration and private socket ACL helper retained.
- Restarted only observer (PID 3700714) and panel (PID 3700819), both active with
  NRestarts=0. Legacy ami-events PID 2619544 unchanged. Google Asterisk PID 23098
  unchanged; no Asterisk restart/reload, AMI permission, Identity, nginx or dialplan change.
- One initial socket-health attempt failed while panel started; bounded retry
  succeeded. Public `/health` and `/board` HTTP 200; anonymous `/api/fleet` HTTP 401.
- Apoint, Eveo and Google connected/healthy, zero disconnects/receive-limit failures.
  Google had already delivered two QueueCallerJoin events during the first sample.
- Private gRPC probe as sufftelpanel: 1 snapshot, 1491 events/20 seconds, Ready=true,
  sequence 2761. Active resources in this sample came from Google (782); idle nodes
  are not fabricated as active calls.
- Board rules SHA256 unchanged:
  `3714a5062515639e831067c9d6ee159e2657393d07acd6effb82999ab1c75a43`.
  No inventory cleanup, server backup files, customer messages or real calls.

## Limits

The authenticated human session and a new real 6007 → Atendimento call were not
exercised. Synthetic lifecycle/browser tests and the real private event transport
were verified separately. Events occurring during observer replacement are not
historical replay; already-ended calls cannot be recreated. Correlation remains
node-local and requires Linkedid, never caller-number or cross-server guessing.
