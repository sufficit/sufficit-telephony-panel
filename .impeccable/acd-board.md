# ACD board extension

Mode: Operate. Local extension of the existing SUI resource mosaic; no new visual
world, route, CSS token or component library. Native queues and ACD queues share
the existing queue column but retain distinct data identities and semantics.

An ACD tile shows controller pending/in-service counts and up to two visit flows
with greeting, waiting, announcement, offering or answered labels. The same exact
call relationship gives the extension its queue destination. Pending includes
greeting and offers; channel Up is not a claim of attendant conversation.

Details remain in the incumbent native overlay, including fullscreen. They show
context, queue ID, data source and current phases; no irrelevant registration
labels for ACD queues. Old controllers show a counts-only compatibility message.
Stale sources retain the title but no live counts/callers. Dedicated-board errors
remain behind its compact filter icon, not a new preamble.

EN/default and PT-BR use PanelText. The user's optional `contextid`, node and
resource-scoped text filters retain their existing URL contract. Long names wrap
inside the current cards and mobile popup. Existing orange actions, neutral
surfaces, state colors, density and keyboard controls are unchanged.

Evidence: `.impeccable/review/acd-board-desktop.png` (1988px),
`acd-board-mobile.png` and `acd-board-details-mobile.png` (390px), all synthetic
Development fixtures. Browser checks cover stages, legacy/stale/empty states,
scope isolation, filters, exact-channel details, EN/PT and mobile overflow.
No screenshot proves production publication, real call audio or authorization of
a live user. See `docs/acd-board-integration.md` for operational boundaries.
