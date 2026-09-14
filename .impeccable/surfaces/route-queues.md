# Queues and registration surface contract

## Overview

Route: `/queues`. Mode: Operate. Status: shipped intent, with the local UI review
disposition **ship**. This surface consolidates operational queue and extension
registration diagnostics in Telephony Panel. It extends the incumbent Sufficit
SUI interface; it does not establish a new visual identity or replace
`PRODUCT.md`, `DESIGN.md` or the token sidecar.

The manager starts with one overview across all configured authorized sources,
then optionally filters and expands source diagnostics. Server/context selection
is never required before viewing queue stages or registration history.
Configuration remains in the Blazor management portal, reached through the
explicit Manage queues link in a new tab. Monitoring lives in Panel; management
does not migrate into this diagnostic screen.

## Colors

Inherit SUI orange actions, neutral light/dark surfaces, semantic alerts and text
tokens. A written Current collection / No current data label accompanies the
source diagnostic text. Warning and error messages explain unavailable observation;
color alone never asserts registration, reachability or a healthy collection.

## Typography

Keep the incumbent system/SUI font and heading hierarchy. Supporting explanations
use secondary text; source identifiers and history dates use tabular numerals.
Channel and extension identifiers remain distinguishable as code. Long context
IDs, contact values and device declarations wrap instead of widening the page.

## Layout

Use the normal Panel shell, header and monitoring-area navigation. The page
heading pairs its diagnostic purpose with the management link. Labeled Context,
Server and search controls precede unified lists; Refresh is explicit
and disabled during loading. Filters wrap with SUI spacing and shrinkable fields.

One dense queue table, observed-call disclosure and extension diagnostic table
replace the repeated per-source sections. Source/context details are collapsed;
each historical entry retains its source. Shared catalog queue capacity is not
added across replicas. Partial fresh workload has an explicit lower-bound marker.
SUI tables stack into labeled rows on mobile. The inherited narrow-screen layout
stacks the heading and keeps controls, long data and expanded histories readable.
Do not add these diagnostic tables or explanatory panels to `/board`: that route
remains mosaic-only, with details in its existing popup.

## Components

### Source scope and URL

Source identity remains bounded by server and authorized context. Matching resource
identities inside a context share a presentation row, with source-tagged histories
ordered together. Different contexts must never be merged merely by name. Canonical URL state uses
`contextid`, `node`, `q` and the optional `queueid` deep-link filter. Server/source
ID aliases resolve to the corresponding displayed server. Filters update the URL
with history replacement; context changes clear the queue selection. Search
covers queue, extension, IP or declared device. Server selection changes the
observation scope, not queue execution placement or authorization.

### Queue and registration truth

Queue stages distinguish greeting, waiting, offering and in-conversation counts,
with announcement context where reported. A stale or unavailable source must not
look like a live zero: counters become dashes without fresh observations, or explicit
partial lower bounds when some sources are missing. Conflicting fresh endpoint
states show Different readings, not invented global availability. Retained details remain historical evidence, with
source age and unavailability made visible. Loading, access expiration, source
failure and no matching observations have distinct explanatory states.

Registration, queue membership, reachability and device usage are independent
observations. Contact IP and Via IP may differ. User-Agent is device-declared,
not verified hardware identity. Dates in the history are observed-change times,
not exact SIP message timestamps.

### Retained history and contacts

Native `details` / `summary` reveals contacts and history per extension. Expanded
state survives the automatic refresh, so investigation is not interrupted by
polling. Keep visible keyboard focus and source-local identity. Contacts show
endpoint, IP, Via, declaration and expiry when reported; missing values are
explicitly unreported rather than invented.

History is source-bounded to up to 20 observed changes over seven days, ordered
newest first, with its collection-start time when present. An older source with
no history states that earlier registrations were not reconstructed. Do not
present this bounded record as a complete SIP audit trail.

### Language

English is the default; Brazilian Portuguese uses the existing Panel localization
and language preference. Labels, states, diagnostics and errors are localized.
Customer names, source/context IDs, extensions, addresses and User-Agent values
remain data and are not translated.

## Do's and Don'ts

- Do preserve incumbent SUI components, URL filters and native disclosures.
- Do retain the distinction between current observation and historical evidence.
- Do keep management in Blazor and the dedicated board mosaic-only.
- Don't infer endpoint availability from registration or queue membership alone.
- Don't describe declared devices or observation dates as verified SIP facts.
- Don't treat local fixture approval as authenticated production UI evidence.

Evidence: `src/Pages/AcdMonitor.razor`, `AcdMonitor.razor.css`,
`AcdRegistrationTable.razor`, `AcdRegistrationTable.razor.css`, `Home.razor`,
`src/Operations/AcdAgentPresence.cs`, `src/wwwroot/panel.css`, and
`tests/monitoring-migration.browser.cjs`.

The retained captures are `.impeccable/review/monitoring-migration-desktop.png`
and `.impeccable/review/monitoring-migration-mobile.png`. The implementation agent
and reviewer opened the complete captures. They depict labeled synthetic local
Development fixtures, not authenticated production monitoring.

Handoff evidence supplied by the implementation agent: real browser execution
PASS; 766 Panel tests and 95 Blazor tests passed. The final review disposition was
ship after all three requested fixes were cleared. This records local UI and
regression evidence only; it does not establish production deployment, a real
user's authorization, complete live inventory or authenticated production UI
acceptance.

## Unified overview revision — 2026-09-13

The user rejected visible partitioning by node/context. The current implementation
therefore keeps a single queue table and registration list by default, preserving
scope only for internal identity, optional URL filters and collapsed provenance.
Shared history is source-tagged and chronological; capacity is not summed across
replicas. Mobile expanded diagnostics use the full row width. The dedicated board
and infrastructure pages are unchanged. The updated fixture contains matching
resources on two live sources plus an unavailable third source. Current evidence:
787 .NET checks, desktop/mobile browser filters, refresh persistence, merged history
ordering, missing-source selection and no horizontal overflow or console errors.
These are synthetic UI checks, not authenticated production browser evidence.
