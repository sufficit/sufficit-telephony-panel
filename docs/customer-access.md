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

Unknown or foreign event ownership is excluded before rendering. Contextual
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

Implementation and local tests are not a customer production rollout. No new
customer grants, issuer configuration or live calls were applied by this change.
Deploy the generic Identity feature/configuration first, then Endpoints, then
Panel; verify real customer sign-in and cross-context denial before enabling users.
Do not change Asterisk, restart PBXs, or apply an old Identity manifest for this.
No automatic conversion of legacy grants is provided.

Multi-context aggregate views currently use live authorized evidence. Retained
inventory is read only inside a selected authorized context, never from the
manager's central inventory. Events without verifiable ownership remain hidden;
their absence is not proof of offline state. The existing 16 active monitoring
session limit remains and requires capacity review before a wider rollout.

Tests: `dotnet run --project tests/Panel.Tests.csproj`; the browser suite
`tests/customer-access.browser.cjs` uses Development-only synthetic previews on
localhost, not production credentials or real telephone actions. Generic claim
delivery and inherited-grant revocation are integration-tested in Identity.
