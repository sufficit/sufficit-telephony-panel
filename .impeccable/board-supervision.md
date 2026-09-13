# Board supervision surface contract

## Overview

Mode: Operate. Scope: the existing extension, queue and trunk card menus and
their exact-channel/continuous-prefix supervision confirmations. This is a
narrow extension of the existing Sufficit SUI interface, not a new visual world.
`PRODUCT.md` and the incumbent theme remain authoritative; this sidecar does not
replace or revise the global `DESIGN.md`.

English is the default interface language, with Brazilian Portuguese through the
existing localization service. Customer names, card titles and PBX identifiers
remain data. Supervision calls the operator's linked preferred telephone; it does
not play call audio in the browser.

## Colors

The surface inherits SUI orange actions and neutral light/dark surfaces. Menu and
dialog backgrounds use `--sui-surface`, text uses `--sui-text-primary`, and borders
use `--sui-border` / `--sui-border-strong`. Existing SUI warning and information
alerts distinguish unavailable prerequisites, demonstration mode and request
feedback. No additional brand palette or state-color meanings are introduced.

## Typography

Use the incumbent system/SUI font and button/select typography. Menu action text
is start-aligned and wraps; card titles in the menu wrap long identifiers. The
dialog heading is 18px, reducing to 16px at widths of 600px or below. Operational
explanations remain readable paragraphs, not compact tile-label typography.

## Layout

The card retains its main details button and adds a separate visible menu trigger
at its upper inline-end corner. The trigger is 28px square with a 3px inset and
4px padding; the tile reserves 34px of inline-end space. This documents the built
compact target, not a claim that it meets a larger touch-target guideline.

The native popover menu is fixed in the browser top layer. Pointer coordinates
anchor pointer opening; keyboard opening falls back to the focused trigger.
Position is clamped to an 8px viewport gutter. Width is the smaller of 310px and
viewport width minus 16px; height is bounded by viewport height minus 16px with
internal scrolling. It uses 8px padding and 4px between items.

Supervision reuses the existing native resource dialog, including in fullscreen.
On `/board`, opening and closing preserve URL filters and board scroll; nothing
is appended below the mosaic. The centered dialog is at most 1040px wide, with
32px total viewport clearance, a fixed header and internally scrolling body.
At widths of 600px or below, clearance becomes 16px, and header/body padding
becomes 12px. Supervision content is a single column with a 16px gap; its
paragraphs have no extra margin. The close action stays in the header.

## Elevation & Depth

Native top-layer placement separates actions from the board without introducing
a new shadow vocabulary. The resource dialog dims the board with a 48% black
backdrop. Menu and dialog use thin borders and inherited tonal surfaces.

## Shapes

The popover and resource dialog use 8px corners. Buttons, selects and alerts
retain their existing SUI shapes and interaction states.

## Components

### Card action menu

Right-click, the visible action button, ContextMenu or Shift+F10 opens the same
menu. The main tile action continues to open details. The menu shows the resource
title, Open details, exact-channel Listen and Whisper, then a divider followed by
continuous-prefix Listen and Whisper. Opening a supervision option does not send
a telephone request.

The menu has an accessible resource-specific name, `role="menu"`, and menuitem
buttons. Opening focuses its first item. Arrow Up/Down wrap among actions;
Home/End move to the first/last. Escape, outside dismissal and Tab close the menu.
Current Tab behavior consumes that keystroke and restores the trigger; a second
Tab advances normally. The closing path restores focus without scrolling. The
review accepted the Tab behavior as a nonblocking note.

### Supervision confirmation

Both flows show mode, server, target selection, microphone-direction guidance,
the linked extension, an explicit confirm-and-call action and stopping guidance.
Selection starts empty; confirmation requires a selected eligible target, a
linked supervisor endpoint and an active connection. The selector is disabled
while sending. Missing channels or protected mappings display explanatory warning
alerts, not a live-looking empty success state.

Exact-channel mode offers fresh authorized SIP/PJSIP observations and explains
that only the chosen channel is monitored; the session ends with that channel.
Continuous mode offers protected prefix mappings for the card/server/context.
Those private mappings are empty by default: display grouping and filters do not
grant supervision permission, and queue names are not inferred as prefixes.

Continuous-mode copy states that matching channels, including future calls, are
followed one at a time on that server, not mixed simultaneously. It describes
telephone `*` for advancing, hangup for stopping, and a 30-minute session limit.
Whisper copy explicitly warns that speech goes to the selected leg, not
automatically to the agent. Closing the dialog does not stop an accepted session.

Sending has an explicit label. An accepted request is reported as accepted, not
as connected audio. After an accepted request or caught failure, the confirm
action is removed for that dialog instance. Ambiguous failures are not retried
automatically and instruct the operator to check the telephone before retrying.
Feedback uses a status region. Demonstration mode is visibly labeled and never
originates a call.

## Do's and Don'ts

- Do keep this surface in the existing SUI orange/neutral identity and EN/PT flow.
- Do preserve board URL, filters, scroll, native overlay behavior and visible close action.
- Do retain explicit confirmation and distinguish request acceptance from audio connection.
- Don't treat card grouping, queue names or display filters as prefix authorization.
- Don't describe continuous supervision as a simultaneous mix or imply browser audio.
- Don't infer production readiness from synthetic preview screenshots.

Evidence: `src/Pages/OperatorBoard.razor`, `BoardCardMenu.razor`,
`BoardSupervisionDialog.razor`, `BoardResourceDialog.razor`,
`src/wwwroot/board-card-menu.js`, `board-dialog.js`, `panel.css`, and
`docs/board-supervision.md`. The four retained captures are
`.impeccable/review/supervision-menu-desktop.png`,
`.impeccable/review/supervision-channel-desktop.png`,
`.impeccable/review/supervision-menu-mobile.png`, and
`.impeccable/review/supervision-prefix-mobile.png`.

Handoff validation: final reviewer disposition was **ship for UI scope**;
621 local tests and browser validation passed as reported by the implementation
handoff. These captures use local synthetic demonstrations. No production deploy,
real SIP/RTP audio acceptance or private production mapping changes were performed.
Exact-channel termination and unbridged behavior also require publishing the API
consumer with the accompanying updated `Sufficit.Standard` monitor service, as
documented in `docs/board-supervision.md`.
