# Documentation index

Start with the [system overview](system-overview.md). It is the canonical description
of the current product, architecture, security boundary, deployment and known gaps.
When a dated activity report conflicts with it, the system overview wins.

## Current contracts

| Subject | Document |
| --- | --- |
| Product boundary and migration | [Monitoring ownership](monitoring-ownership.md) |
| Normal operational workspace | [Operational monitor](operational-monitor.md) |
| Dedicated television board | [Operator board](operator-board.md) |
| Board classification rules | [Board configuration](board-configuration.md) |
| Listen and Whisper | [Board supervision](board-supervision.md) |
| Calls versus channel legs | [Call correlation](call-channel-correlation.md) |
| Remembered resources | [Retained resources](retained-resources.md) |
| ACD queue projection | [ACD board integration](acd-board-integration.md) |
| Legacy FOP rule import | [FOP rule import](fop-rule-import.md) |

Queue configuration, member operation and runtime controller contracts live in
[`sufficit-telephony-acd`](https://github.com/sufficit/sufficit-telephony-acd/tree/main/docs/contracts).
Queue UI and the reusable telephony destination selector live in
[`sufficit-blazor`](https://github.com/sufficit/sufficit-blazor). The private gRPC
transport is maintained in
[`sufficit-ami-events`](https://github.com/sufficit/sufficit-ami-events). The
separation is intentional: Panel observes; Blazor manages; the ACD service executes.

## Historical evidence

Files under [`activities/`](activities/) are immutable, dated implementation and
deployment evidence. They explain what was known at that checkpoint and preserve
rollback information. They are not a current backlog and may mention versions,
routes or limitations that were superseded later.

The root `PLAN-telephony-*.md` files are also historical execution plans. Remaining
work is maintained in the canonical overview instead of extending those plans.
