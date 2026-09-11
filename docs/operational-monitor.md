# Operational telephony monitor

## Current delivery boundary

The 0.4 panel opens the manager-wide central view automatically, without a company
selection. Company is optional and clearable; central resources come from the
existing AMIHub ALL group, including resources without a configured portal card.
Extensions and trunks are grouped as endpoints in this view because AMI does not
provide the business classification. Company-scoped cards retain that distinction.
The original 0.3 panel added client selection, authorized extension/trunk/queue cards,
observed calls, queue members/callers, node/text filters, pagination and confirmed
Listen/Whisper. Infrastructure/history remain separate and unchanged in purpose.
This is a monitor, not a replacement for the portal's queue administration.

**Entitlement-scope activation completed on 2026-09-10; real-user acceptance remains**.
The dedicated client and the running panel now request openid/profile/roles/entitlements.
Identity maps directive/entitlements claims to directives or its accepted successor
entitlements (verified against the deployed build's ClaimOptions). The existing
AMIHub explicitly permits manager-wide read access independently of this scope.
Company APIs and supervision still require the user's phone-call entitlement claims;
this change creates no user roles, tenant grants or supervisor calls.

The new operator credential permitted reading the shared scope. Its three resources
and all 14 previous clients were preserved; a read-only database check found no
scope entitlement properties to preserve. Provisioning adds management metadata to
that shared scope and updates the panel client; the subsequent preview is idempotent.
Keep the shared scope intact during future panel changes/removal. Its stale description
was not rewritten as part of this narrowly scoped activation.

`Panel__RequestEntitlements=true` is enabled locally and on Eveo. Only the panel
restarted. Health returns 200, anonymous fleet returns 401, and login requests all
four scopes. Identity accepts that request (302 to login, previously ID2051).
Sign out/in for new consent; this unauthenticated check does not establish that a
particular user receives phone-call claims or that authenticated tenant data works.
Never copy Identity secrets or bypass entitlements to complete acceptance.

## Data and authority

### Token-format incident and correction (2026-09-10)

Scope activation is not sufficient for AMIHub compatibility. The real panel login
at 19:25:21 UTC received a reference/opaque access token (verified from token
metadata only), and AMIHub negotiation returned 401. AMIEvents currently configures
JWT Bearer validation without reference-token introspection. The live Identity
issuer has a per-client Jwt rule for SufficitBlazorServer, but none for this panel;
the reference-token default therefore applies.

Applied narrow correction: add `sufficit-telephony-panel: Jwt` under
`Sufficit:Identity:Tokens:AccessTokenFormatsByClient`, preserving every other rule.
Do not change the global default or shared resource formats. The Identity policy is
registered as a singleton, so activation needs an explicitly approved issuer-service
restart and a new panel login. The user approved the restart: the rule is now active
on Eveo, Apoint and Castrum, restarted sequentially with health/readiness gates and
uniform, unchanged JWKS. No PBX, AMI collector or panel restart occurred. Signed JWTs
remain compatible with Identity introspection; verify both hub and company APIs
with the user's new session. Old opaque tokens remain opaque until a new login;
reloading the page does not replace them. Do not substitute ID tokens, shared tokens
or disabled validation. Versioned delta and guarded merge live in
`deploy/identity-token-format.json` and `deploy/identity-token-format.py`.

The panel's generic error currently masks the upstream HTTP status and does not
log the exception. Empty application warning logs do not prove a working stream.
Future diagnostics must report safe operation/category/status only, excluding
credentials, arbitrary upstream response bodies and token-bearing URLs.

- API: `https://endpoints.sufficit.com.br`, delegated user access token,
  server-side only. Requests have a 12-second timeout, 2 MiB response limit, no
  automatic redirects and no automatic mutation retry.
- Read endpoints: contact/search; telephony/eventspanel/cardsbycontext;
  telephony/chromeextension/preferred. The preferred endpoint response is sanitized
  to ID/title only. Config is deliberately not called because it can write defaults.
- Stream: fixed `https://eveo-apps.sufficit.com.br:26507/amihub`, normal TLS
  validation. Do not reuse the legacy transport that disables certificate checks.
- Reuse EventsPanel.Core and the existing monitor request/enum source contracts.
  Do not invent new AMI collection or expose an arbitrary upstream URL proxy.
- One stream per authenticated panel session, shared across tabs; at most 16.
  Inactive sessions expire after 2 minutes, checked every 30 seconds. Role/token
  checks use PanelSessions; failed/revoked identities lose their stream.
- Tenant cards reauthorize against the API at most every 60 seconds. The API's
  token/entitlement revocation policy remains authoritative; re-querying cannot
  promise faster claim revocation than the upstream token policy supports.
- In the selected-company view, call and waiting-caller rows require accountcode matching the selected UUID AND
  an authorized card match. Missing accountcode is not assumed to mean this client.
  Peer/queue metadata requires an authorized resource card.
- In the default central view, the server-validated manager stream is authoritative
  for read-only visibility across companies. It builds endpoint/queue groups in a
  linear pass, without regex scanning. Actions still resolve a concrete accountcode
  and reauthorize against the company API; missing accountcode cannot enable action.

## Registration evidence

The UI separates SIP registration, qualify reachability and device usage. A
Registered PeerStatus event records the panel reception time as **last registration
observed**. Unregistered preserves that last successful observation. Reachable,
Unreachable, RTT and device-state changes cannot overwrite registration evidence.
Static PJSIP contacts can be reachable without registration; missing evidence stays
unknown. RTT microseconds are converted to milliseconds, never interpreted as a date.

Contact reachability describes the last contact event for an endpoint, not an
aggregate of all devices. TCP/WebSocket socket connectivity is not established by
qualify. Dates include their UTC offset. This release does not recover historical
registrations before subscription; a shared persistent snapshot/history collector
is still needed for a complete inventory and last-registration history.

Filters include endpoint text/UUID/channel, node, online/no-response, registration,
device use and missing registration. Unknown is not offline. Manager resource
summaries name the node of their latest peer evidence; details show every observed node.

## Observation, not reconciliation

An AMI event stream is not a complete inventory. The panel does not trigger
GetPeerStatus/AMI bulk refresh for every browser, and cannot claim to include calls
that started before subscription. Reconnect clears previous state. Hangup removes
the matching node/channel, including a waiting row. Old unreconciled entries expire
after 2 hours. Channel IDs are qualified by node.

The projection retains at most 5,000 resources per session and only allowlisted
primitive fields, each capped at 256 characters. UI matching is capped at 100,000
card comparisons per refresh; reaching either cap displays a partial-data warning.
Regexes are bounded, cached per immutable card and non-backtracking. Cards are
limited to 2,000 and 64 patterns each; detailed tables and pages are bounded.
The UI refreshes the in-memory projection every 2 seconds, not the PBX.
Do not read waiting-task metrics as callers; ACD means automatic call distribution.

## Supervision safety

No action occurs on row selection. Operator chooses Ouvir/Sussurrar, reads the
client/channel/node and own preferred extension, then confirms. Focus moves to
the confirmation. The existing API calls that preferred PJSIP/wss-UUID endpoint;
the browser never receives audio or an AMI credential.

Server validates mode, current manager, API tenant authorization, preferred
endpoint, exact observed node/channel and accountcode, connection state and an
event no older than 30 seconds. A quiet long-running call may deliberately have
disabled controls until a new event arrives. Actions are serialized per session,
limited to one every 15 seconds, and never retried automatically. API
`accepted=true` means originate was accepted, not that media is connected.
On uncertain response, check the supervisor endpoint before retrying.

## Validation and remaining acceptance

- Console regression suite covers bounded storage, identity expiry/revocation,
  cross-node isolation, cross-tenant denial, stale targets, protocol injection,
  enum normalization, malformed patterns, rejected actions and HTTP 403.
- Development fixtures explicitly label synthetic data. Browser evidence covers
  desktop/mobile, both themes, empty filtering/clear, details, confirmations and
  no document overflow. Synthetic confirmation never invokes the real API.
- Eveo network checks reach both hub and cards endpoint with HTTP 401 when
  anonymous; TLS works. This is not proof of authenticated authorization.
- Still required after provisioning: real manager login/consent, authorized client
  data, three-node event reception, reconnect behavior and a separately approved
  real supervisor call. Do not describe these as tested from fixture evidence.

No PBX, main portal or AMI collector restart/reload occurred. The separate, explicitly
approved Identity configuration rollout required sequential issuer restarts.
