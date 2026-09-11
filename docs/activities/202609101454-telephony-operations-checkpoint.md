# Telephony operational panel checkpoint — 2026-09-10 14:54 -03

## Status

Implementation and Eveo test deployment delivered; **live tenant activation is
not complete**. Keep PLAN-telephony-operations.md open until Identity provisioning,
new user consent and real authorized data acceptance are verified.

## Delivered

Extended the infrastructure-only panel with client search, extension/trunk/queue
resources, observed call details, node/text filtering, pagination, queue members
and callers, and explicit Listen/Whisper confirmation. Preserved SUI, light/dark,
infrastructure/history and internal manager access. No customer notifications.

Backend reuses existing DTOs, API and central event hub; normal TLS, bounded
per-session streams, sanitized responses and tenant-qualified call filtering.
Action requests recheck manager/token, API permission, current observed target,
tenant, preferred supervisor, state freshness and per-session cooldown.

## Verification

- `dotnet run --project tests/Panel.Tests.csproj`: 42 checks passed.
- Release publish succeeded; build zero errors/warnings.
- Browser fixtures: desktop 1440 and mobile 390, light/dark, filters/empty/clear,
  details, confirmation focus/cancel/synthetic send, horizontal table keyboard
  access, no document overflow. No real API call action invoked.
- Impeccable reviewer `ship` verdict resolved its three fixes: placeholder
  contrast (6.65:1 light, 9.31:1 dark), mobile table access, DESIGN documentation.
  This is not a production/authentication approval.
- Runtime readonly checks from Eveo reach API and hub with anonymous HTTP 401.
- Deployment to `/opt/sufficit-telephony-panel/releases/eveo-20260910-1751`;
  archive SHA256 `0d2e8477f034fdee2ce9ca4dadae3ea2b90491488a5ade09d7c3ee1410a9cc3a`.
- Local/remote assembly SHA256
  `6379a21e0a5a8edd9671b106f8a1a9ac5a6d56d8778d079482f3867fb88e2b9b`.
- Only `sufficit-telephony-panel` restarted, PID 2498222. Identity PID 2469487
  and AMI events PID 344 unchanged before/after. No PBX command/restart/reload.
- Public health 200, anonymous fleet 401, login 302. No warning logs after start.
  Initial settled panel memory 65,433,600 bytes, not a loaded-capacity benchmark.
- Public browser check with `?preview=true` still presents only the login page;
  zero synthetic client headings and zero protected operational headings. The
  Development preview cannot bypass Production authentication.
- Documenter updated only the design sidecar (10 component samples, operational
  evidence and access caveats); JSON and git diff whitespace checks pass.

## Activation blocker

Identity's current registration permits openid/profile/roles, not entitlements.
Readonly authorization probe confirmed HTTP 400 / ID2051: application not allowed
to use requested scope. The new manifest is prepared but NOT applied.

Keep `Panel__RequestEntitlements` false so existing infrastructure login works.
Use the complete prefilled operator-token issuance link in deploy/README.md;
request only a private token file path. Preview/apply must change only this client.
Then enable the panel flag, restart only the panel and sign out/in for consent.
Never infer tenant rights from manager or use a shared privileged API token.

Remaining: authenticated authorized-client reception, three-node stream behavior,
reconnection acceptance and separately approved real supervision call. Current
data model is explicitly partial event observation, not reconciled PBX inventory.
