# Operational telephony panel

## Scope and decisions
User requests the most complete operational view based on the portal monitor:
extensions, trunks, active calls, queue callers/members, client selection and
Listen/Whisper controls. Preserve the existing infrastructure view and SUI theme.
Manager-only application gate does not replace upstream tenant entitlements.
Reuse EventsPanel contracts and existing endpoints/AMI event hub; never expose
tokens or create browser/PBX AMI connections. Actions require explicit confirmation,
fresh target data, tenant authorization and the user's linked supervisor endpoint.
No real call actions in tests, PBX restart, customer messages or data migration.

## Reconnaissance
Portal loads cards via /telephony/eventspanel/cardsbycontext or cardsbyuser and
uses the existing /amihub stream. Existing monitor API owns Listen/Whisper.
Legacy EventsPanel transport disables certificate validation: do not reuse that
transport. Retain DTO contracts but enforce normal TLS in the new adapter.
New panel token currently requests openid/profile/roles; upstream acceptance and
phone-call entitlements must be verified, never inferred from manager alone.

## Checkpoints
Latest request (2026-09-10): central view by default for managers; company is an
optional filter. Distinguish registration, reachability, device use and observation
time. Never invent a historical registration timestamp or widen action permissions.

Current continuation:
A. [completed with activation blocker] Fix/preview Identity manifest with supplied temporary credential;
   verify manager-wide data contracts and available registration evidence.
B. [completed] Implement initial central view, optional company filter and explicit
   peer states/timestamps; regression tests for scope and event semantics.
C. [completed] Browser/reviewer validation, publish only panel on Eveo and document
   evidence. 0.4.0 released as eveo-20260910-1830; health 200, anonymous fleet 401,
   login 302, no warning logs. Panel PID 2534188; Identity 2469487 and AMI 344 unchanged.
D. [completed] Read/preserve the shared scope and provision the panel's entitlements
   permission. Apply succeeded; preview is idempotent; authorization probe now returns
   302 /account/login instead of ID2051. All 14 previous clients and 3 resources remain.
E. [completed] Enabled Panel__RequestEntitlements in the local/remote panel unit;
   systemd validation passed and only panel restarted (PID 2556589). Health 200,
   anonymous fleet 401, login 302 requesting openid/profile/roles/entitlements.
   Identity PID 2538028 and AMI PID 344 remained unchanged; no warning logs.
F. [completed, authorized configuration rollout] Real login exposed an
   access-token format mismatch. Diagnose completed: AMIHub negotiate returns 401;
   the panel's access token minted at 2026-09-10 19:25:21 UTC is Reference, while
   AMIEvents uses AddJwtBearer without introspection. No token values were read.
   The issuer's exact-client JWT map includes SufficitBlazorServer but not this
   panel. Add only sufficit-telephony-panel=Jwt, preserving other rules/resources,
   User authorized the required Identity restart on 2026-09-10.
   AccessTokenFormatPolicy is singleton and cannot consume a config-file reload.
   F1. [completed] Eveo/Apoint/Castrum healthy/ready on c79f5beb; identical JWKS.
       Preserve each node's existing JWT rules, including Eveo-only Blazor rule.
   F2. [completed] Version exact-client overlay and safe atomic config merge;
       test preservation, idempotence, rollback and concurrent-change rejection.
       Three Python tests pass, including file mode retention and stale SHA rejection.
   F3. [completed] Acquire cluster lease, apply on all nodes, restart one Identity
       at a time with readiness gates. Never restart PBX, AMI or panel.
       Exact rule applied on all three nodes under coordinator lease. Eveo restarted
       successfully (PID 2586464), /health and /health/ready passed. Apoint also
       passed after restart (PID 1018743); Castrum passed both readiness gates.
       Active installs are direct /opt/sufficit-identity directories, so the release
       activation helper's release-root check is incompatible. Use configuration-only
       systemd restarts with the same per-host lock and readiness checks, no release switch.
   F4. [completed] Three nodes have the exact panel Jwt rule with previous client
       and resource maps preserved. Health/ready pass; JWKS hash unchanged and uniform.
       Identity PIDs: Eveo 2586464, Apoint 1018743, Castrum 209074. AMI 344 and
       panel 2556589 unchanged. Public Identity/panel health 200; anonymous fleet 401;
       login 302 retains openid/profile/roles/entitlements. 51 panel checks pass.
G. [in progress, awaiting user's new login] After the scoped issuer change, new login/consent and authenticated
   central/company acceptance; confirm successful hub negotiation/event reception.
   Historical registration/snapshot remains an explicit implementation gap.

Incident 2026-09-10: network/TLS probes from Eveo reached both the hub and preferred
endpoint; anonymous 401 is expected. Nginx records four negotiate 401 responses at
16:25 local, matching the real user's new login. Panel swallows the exception without
logging, so its empty warning journal was NOT evidence of successful integration.
Do not change shared AMI credentials, use an id_token as an API token, disable token
validation or give the manager extra grants. The narrow compatible fix is the same
per-client signed-JWT issuance already used by SufficitBlazorServer. Subsequent
diagnostic work should log only operation/category/status, never tokens, response
bodies or URLs with access_token. No diagnostic code or runtime configuration was
changed during that investigation. The subsequent approved configuration rollout
is complete above; old reference tokens require sign-out/sign-in, not page refresh.

Activation continuation (2026-09-10): the new operator credential successfully read
the shared scope detail. All three audiences and the 14 existing clients were
preserved. Verified the legacy "NOT YET ACTIVE" description against the deployed
claim gate and inspected the omitted scope properties before apply. Provisioning,
panel setting, authorization redirect and health checks have now completed.
Baseline for this continuation: panel PID 2534188, Identity PID 2538028, AMI PID 344.
Identity's PID changed independently before this continuation; do not restart it.
Scope properties were NULL (read-only database check), so no implicit scope grants
are removed. The deployed Identity build c79f5bebcbfdcd72d727152119b779ec722ff199
accepts entitlements as a successor of directives. Keep its stale shared description
unchanged here. Provisioning necessarily adopts scope ownership metadata; future
manifests must preserve this shared definition, not treat it as panel-exclusive.
Validation rerun: 51 console checks pass; manifest JSON parses; git diff --check
passes. Local/remote unit SHA256:
4788830644a1f5225db5f5013035195660fecf174147c8524e44d52cfaa8e49c.
Panel memory after activation: 62,763,008 bytes. Token was held only in a private
no-echo helper process, which exited; no token was written into the repository.
F requires the user's own login/consent. Do not use an operator provisioning token
to impersonate the user's telephony session. This plan remains open for acceptance
and the explicitly deferred persistent registration/snapshot work.

Implementation evidence: 51 checks pass, Debug build 0 warnings/errors. Central
view starts automatically through the existing manager ALL group. Company filter
remains opt-in and clearable. Reachability/registration/device evidence is separate;
RTT is milliseconds, registration times are explicitly panel observation times.
Browser: initial central without company; search/select/clear company; text search;
registration filter (1 unregistered / 2 registered synthetic endpoints), light/dark
1440 and 1941px, mobile 390px without page overflow. Immediate binding fixed search
button. Reviewer scored persistence fix resolved and input fix resolved in source;
sidecar verified. Production preview=true shows login only, no protected fixtures.
Final archive SHA256: 961fcfe131fa99fc85ed037be1706029240288dc66440fe31de9f1502c33da85.
Final DLL SHA256: f1a03b2aa219e5bfbb7fa2abf75497bd9422bd1610b3b60bae75dcf48e8cd576.
Local/remote hashes match. Initial panel memory settled to 47,837,184 bytes.

Historical evidence before activation (superseded by D/E above):
AMIHub already grants exact manager users the ALL group; central read-only
observation does not require tenant cards. Tenant filters and monitoring mutations
still use upstream entitlements. Provisioning preview requires declaring the custom
scope, but an existing shared entitlements scope would be updated. Reading its
current definition returned 403 identity.scopes.read missing. Do not overwrite that
shared scope blindly. No Identity changes were applied. Continue central monitoring
independently; scoped API activation remains pending a scope-read credential.

Earlier checkpoints (preserved):
1. [completed] Verified existing cards, Contact/Search, preferred supervisor and
   monitor/actions endpoints; hub broadcasts (server, JSON event) by event type.
   Preserve API phone-call entitlements and expose partial observation explicitly.
2. [completed] Implemented bounded session streams and API delegation. 38 regression
   checks pass, covering isolation, stale targets, permissions, projection limits and accepted responses.
3. [completed] SUI operational view, filters, details and confirmations implemented;
   integrated with preserved infrastructure view. Build passes with zero warnings.
4. [completed] 42 checks pass; Release build/publish passes. Browser desktop/mobile,
   light/dark, filters, details, keyboard focus and synthetic confirmation tested.
   Independent reviewer scored all 3 requested fixes resolved (ship at that scope).
5. [pending live activation; resumed by A–C above] Published panel 0.3.0 to Eveo release
   eveo-20260910-1751. Health 200, anonymous fleet 401, login 302, no new warning
   logs; local/remote DLL SHA256 match. Identity/AMI PIDs unchanged by deployment.
   Identity readonly scope probe returns HTTP 400 ID2051, confirming the missing
   entitlement-scope permission. Live activation needs a new operator credential:
   add entitlements to the client manifest, then enable Panel__RequestEntitlements,
   sign in again and validate real tenant data. No credential is available locally.

Artifact SHA256: 0d2e8477f034fdee2ce9ca4dadae3ea2b90491488a5ade09d7c3ee1410a9cc3a.
Panel PID 2498222; initial settled memory 65,433,600 bytes. No real calls made.
This plan remains open; fixture/review success is not authenticated acceptance.
Public browser also verified Production ignores preview=true and hides protected
operations. Documentation/sidecar verified; local Development server stopped.

Prior PLAN-telephony-panel.md remains the completed provisioning/initial-host
context; its real user acceptance is not silently replaced by this feature plan.
