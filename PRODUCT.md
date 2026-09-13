# Sufficit Telephony Panel

<!-- impeccable:product-schema 1 -->

## Platform
web

## Users
Confirmed: internal Sufficit staff with the exact `manager` role from Sufficit
Identity. Customer access and customer notification delivery are deferred.

## Product Purpose
Observe the Apoint, Eveo and Google PBXs through existing central monitoring APIs,
independently of the main portal. Distinguish healthy observation, missing data,
stale collection, workload and actionable infrastructure alerts.

## Operating Context
First deployment: eveo-apps test environment. Existing PBXs and main portal must
not be restarted. Aggregate infrastructure metrics are not tenant-scoped call data.
The existing collectors remain responsible for AMI and PBX collection.

## Capabilities and Constraints
- Server-side authentication and role checks; never expose monitoring credentials.
- Bounded token, entitlement and monitoring caches; no per-tab AMI connections.
- History and per-node details, freshness timestamps and clear unavailable states.
- Dedicated `/board`: only the resource mosaic, with selected details in a popup, without the
  normal page header, tabs, tips or legend. A compact filter icon opens persistent
  URL-backed filters and display options; the normal page retains full controls.
  Never append details or confirmations below the dedicated mosaic. Opening or
  closing the popup preserves board scroll and filters; normal-page links open
  another tab so the television-style board keeps running.
- No customer messaging or PBX restart/reload. User-approved Listen/Whisper is
  delegated to the existing monitor API after explicit confirmation, tenant
  authorization and fresh observed-target validation.
- Local Unix socket preferred behind the existing reverse proxy.

## Brand Commitments
Confirmed application/repository name: sufficit-telephony-panel.
Use Sufficit.Blazor.UI (SUI) and the Sufficit identity. Source identifiers, comments
and canonical routes are English. The interface supports English (default) and
Brazilian Portuguese; language preference is stored in browser localStorage.
Customer names and PBX identifiers are data and must not be translated.
The user requests a dedicated icon, rich effects, animation and sound.
Implementation assumption: sounds are opt-in and can be muted; motion respects
reduced-motion preferences and never implies events that were not observed.

## Evidence on Hand
Existing metrics and alerts from AMI observation and operational collectors for
three PBXs. Local SUI library and main portal components provide real UI contracts.
The operational extension reuses authorized portal cards and the central AMI hub
for telephone numbers, call/channel states, trunks and queue resources. Call audio
never enters the browser: supervision calls the operator's linked endpoint through
the existing API. Fixtures are development-only and explicitly labelled.

## Product Principles
- Fail closed on missing identity or authorization evidence.
- Stale data must never look like live zero values.
- Reuse existing monitoring infrastructure and preserve PBX stability.
- Communicate internal task queues separately from waiting callers.
