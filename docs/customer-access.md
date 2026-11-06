# Customer access contract

## Application-owned permissions

`telephony.panel:<context-guid>` permits viewing operational data in that context.
`telephony.monitor:<context-guid>` permits Listen and Whisper there, independently
of the Panel UI. The Panel requires both permissions for supervision, while a
different application may use `telephony.monitor` without Panel access.

The all-zero GUID grants all contexts **for that exact permission only**. A user
may have multiple direct or inherited grants. A single nonempty view context is
selected automatically and has no selector. Multiple view contexts offer a filter
whose unfiltered view includes only authorized contexts. `contextid` persists
selection in the URL; changing or clearing it never grants access. Existing
internal managers keep their separately verified global role access.

These meanings belong to consumers, not to the public Identity provider.

## Identity and API boundaries

The existing self endpoint is `/connect/userinfo`, not `/me`. Current access is
read through its generic `?entitlements=current` extension. The user is always the
validated token subject. Both Panel and Endpoints maintain separate bounded
server-side caches: subject plus token hash, maximum 2000 entries, grants valid
for at most 60 seconds, denials for 5 seconds, 5-second fetch timeout and 256 KiB
response limit. Failed refresh does not extend expired permissions. The Panel
also checks access-token expiry and periodically clears revoked UI state.

The existing OAuth `entitlements` scope is still requested to obtain the legacy
Endpoints audience; a scope name is not a business grant. Actual `telephony.panel`
and `telephony.monitor` values must not be in newly issued tokens. To configure
that generic delivery rule, merge
[`deploy/identity-server-resolved-entitlements.json`](../deploy/identity-server-resolved-entitlements.json)
into the **deployment's** Identity configuration. This file is not an Identity
provisioning manifest and must not be sent to the provisioning API. Its keys do
not belong in Identity source defaults.

Unknown or foreign event ownership is excluded before rendering. Exception (2026-10-21):
endpoint status events (registration/qualify, `Kind == "peer"`) carry no AccountCode, so they are
attributed to the **selected** context only when the key exactly equals one of that context's own
extension cards (anchored literal `^SIP/<title>`; extension titles are globally unique in the portal
registry). Wildcards, alternations, unanchored display patterns, trunks, calls and foreign
AccountCodes never qualify, and nothing is attributed without a selected context. Contextual
snapshots are validated and filtered. Customers cannot read fleet diagnostics or
shared manager board rules, or modify shared rules/inventory. Same-named endpoints
in different authorized contexts retain distinct card identities.

`GET /telephony/panel/cards?contextid=...` validates current view permission before
querying cards. `POST /telephony/monitor/actions` independently validates current
supervision permission and, for customers, the user's stored preferred receiving
extension and exclusive ownership of the target endpoint. Display regexes,
queue names and shared trunks are not authority. Arbitrary Local legs and broad
prefixes are rejected; continuous SIP/PJSIP prefixes need an exact endpoint and
the trailing channel separator. Actions remain Listen/Whisper only.

## Current delivery limits

Update 2026-10-21: production Identity now delivers `telephony.panel` and `telephony.monitor`
as server-resolved grants (all three nodes). The first customer test grant exists for
`teste@sufficit.com.br` in context `026246a851a7485991e0b06e1bebb045`; real sign-in and
cross-context denial still need to be verified before enabling customers.

Implementation and local tests are not a customer production rollout. No new
customer grants, issuer configuration or live calls were applied by this change.
Deploy the generic Identity feature/configuration first, then Endpoints, then
Panel; verify real customer sign-in and cross-context denial before enabling users.
Do not change Asterisk, restart PBXs, or apply an old Identity manifest for this.
No automatic conversion of legacy grants is provided.

Multi-context aggregate views combine each authorized company's API catalogue
with its own live evidence, preserving idle extensions and separate company
identities. Retained inventory is read only inside a selected authorized context,
never from the manager's central inventory. Events without verifiable ownership remain hidden;
their absence is not proof of offline state. The existing 16 active monitoring
session limit remains and requires capacity review before a wider rollout.

Tests: `dotnet run --project tests/Panel.Tests.csproj`; the browser suite
`tests/customer-access.browser.cjs` uses Development-only synthetic previews on
localhost, not production credentials or real telephone actions. Generic claim
delivery and inherited-grant revocation are integration-tested in Identity.

## Display pairing (2026-11-06)

A monitoring display never signs in as a person. `GET /display` shows an 8-character code (10 minutes, cookie-bound to that browser).
Staff type it on the Blazor panel access screen (`/pages/telephony/panel-access`, "Adicionar display"), which creates an
Identity system user holding only `telephony.panel:<context>`, issues a one-hour enrollment code for the client
`sufficit-telephony-panel-display` and calls `POST /api/display/pair` with both. The enrollment code is the authorization.
The panel redeems it, refuses any credential that has monitor, the manager role, the all-zero context or more than
one context, and the display (`/display/complete`) gets a 365-day session whose access token is renewed with the refresh
token (`PanelSessions.Refresher`). Three failed enrollments burn a code. Revoking the display in Blazor disables the
system user, which revokes its credential; the next renewal fails and the display leaves the board.
Checks: `DisplayPairingChecks`.

## "Entrar como" (2026-11-06)

A manager reproduces a real customer person: `GET /impersonate?userid=<person>&contextid=<customer>` (opened from
the Blazor panel access screen) exchanges the manager's own panel access token at Identity for a 30-minute token of
the person (`impersonated_subject`, see sufficit-identity `docs/usage/USAGE-IMPERSONATION.md`). The panel signs in
that token, so every permission is the person's real one, resolved by Identity. The session never has monitor or
the manager role, a banner shows who is acting and `POST /impersonate/exit` restores the manager session. Identity
refuses without `identity.users.impersonate`, a recent second-factor sign-in, or for privileged people, and audits
every decision. Checks: `ImpersonationChecks`.

## Sales representatives (2026-11-06)

Endpoints expands each explicit `telephony.panel:<context>` grant with the direct
active customers returned by `SalesRuntime.GetActiveCommissionedCustomers`, the
same authoritative source used for commercial visibility in FOP. It keeps the
representative itself, deduplicates customers, rejects the zero GUID, and does not
traverse a represented customer's portfolio. No monitor or manager permission is
inherited. Identity remains responsible for direct grants; business relationships
are resolved by Endpoints (`GET /telephony/panel/contexts`).

The API caches relationships for at most 10 seconds; the panel caches the resolved
set for 45 seconds. Failed refreshes fail closed. Relationship removal therefore
propagates within 55 seconds plus the existing polling interval. Direct grant
revocation retains the Identity client's 60-second bound. A context no longer
allowed is excluded even if its catalogue is cached.

Landing on the sole directly granted representative's context opens the aggregate
view automatically, including through "Entrar como". A represented company can
still be selected separately; selecting the representative inside the running
panel shows its own resources. Existing impersonation restrictions remain.

Aggregate catalogues preserve idle resources and scope card identity by company.
Unowned peer status is attributed only to an exact extension present in exactly
one authorized catalogue; ambiguous keys and foreign accounts remain hidden.
Calls always require ownership. Limits: 1,000 contexts, 2,000 cards per company,
10,000 aggregate cards; exceeding a limit reports an error, never silently grants
or truncates a portfolio. Tests: `RepresentativePortfolioChecks` in the panel suite.

Portfolio queue reads explicitly enumerate the authorized company set, including
`AnyContext` sources, before projecting the aggregate. They share one five-second
budget and the reader's four-request concurrency limit. Unrelated fixed sources
are not queried. `RepresentativeQueueChecks` covers this through session access.

## Customer ingress inventory (2026-11-06)

The scoped cards API includes owned DIDs and non-deleted WhatsApp ingress routes
as TRUNK cards, including expired DIDs and disabled routes with explicit state.
Their `entry:<kind>:<context>:<opaque-id>:<state>` identifiers are inventory keys,
never patterns for shared carrier channels. WhatsApp session IDs are hashed;
provider tokens and routing internals are not exposed. These cards do not claim
registration, reachability or live call activity: no safe per-ingress correlation
is currently available in the panel's AMI projection. Existing SIP trunk live
state remains unchanged. Ownership is rechecked by the API and portfolio projection.
