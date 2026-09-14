# Telephony panel restructuring and Eveo test deployment

> Historical execution plan. Its checkpoints describe what was known during the
> 2026-09-10 delivery and are not the current backlog. See
> [docs/system-overview.md](docs/system-overview.md) for canonical status.

## Objective and acceptance
Restructure this application as `sufficit-telephony-panel`, independently deployed
from the main portal. Deliver an operational SUI interface for the already collected
Apoint, Eveo and Google PBX metrics, authenticated through Sufficit Identity, with
user/group authorization, bounded token/entitlement caching, purposeful animation
and opt-in sounds. Deploy only to eveo-apps initially. Verify real routes, denied
access, data freshness, desktop/mobile behavior and the test deployment.

## Constraints
- No PBX configuration changes or restarts. No main portal restart.
- Customer notifications remain deferred; browser sounds are not customer messages.
- Reuse existing collectors, SUI and Identity contracts. Do not open browser AMI
  sessions, expose upstream secrets or accept arbitrary PromQL/URLs from clients.
- Keep authentication tickets/tokens on the server, bound cache capacity and TTL,
  and explicitly test revocation and user/group isolation. Do not authorize fleet
  visibility from a generic tenant telephony entitlement.
- UUID identifiers follow Sufficit conventions; no new integer identity scheme.
- Rename the application, assembly and deployment artifacts. Inspect consumers
  before moving the checkout or changing a GitHub repository/remote name.
- Inspect existing release/routing conventions; deploy immutable artifacts and
  switch releases safely, never overwrite DLLs loaded by a running process.

## Reconnaissance — 2026-09-10
- Initial repository worktree was clean. Read `.github/copilot-instructions.md`.
- Current host targets net9.0, uses legacy Material UI, EventsPanel references,
  Startup and OIDC/cookie setup requiring modernization.
- `PanelGenerator` primarily loads configuration; API card loading is disabled by
  `if (false && firstRender)`. This is not a finished per-user management surface.
- eveo-apps read-only inspection confirmed ASP.NET/.NET 10.0.11, Nginx and running
  Identity/main portal services. No service matching telephony panel was listed.
  This is not proof that every legacy artifact/consumer is absent: inspect further.
- Existing helper unit runs the old assembly as shared dotnetuser and has an
  inaccurate AMI-events description. Replace with a dedicated hardened service.
- No PRODUCT.md or DESIGN.md found. Impeccable context script ran once with target
  src/Pages/Index.razor. It requires product confirmation before new UI work.
- No source code, Identity records, server files or services changed in this pass.

## Ordered checkpoints
1. [completed] Inspect current host, repository rules and Eveo runtime/service inventory.
2. [completed] Confirm first-release audience and authorization boundary; capture
   PRODUCT.md and the SUI operational surface contract through the required skills.
3. [completed] Inventory Identity provisioning/cache APIs, SUI APIs, consumers and
   deployment/routing conventions; rename and modernize the standalone host.
4. [completed] Implement server-side authentication, user/group permissions, bounded
   token/entitlement caches and fixed read-only monitoring adapters.
5. [completed] Implement overview, node details, history, freshness/error states and
   accessible animation/sound controls; keep tenant and infrastructure data distinct.
6. [completed] Run build and security/cache/failure regressions; validate desktop,
   mobile, light/dark and reduced-motion behavior in the browser; skill finish review.
7. [completed] Dedicated test service deployed to eveo-apps via Nginx/Unix socket.
   Authorized operator token used for inventory/preview/apply on 2026-09-10 14:03 BRT;
   exactly one client Create: sufficit-telephony-panel. No role grants or other changes.
8. [in progress — awaiting real manager sign-in] Verify authenticated and unauthorized test deployment, resource usage,
   secret isolation and routes; document release/rollback and create activity report.

## Current decision
Confirmed by the user: internal operators only, specifically Identity role `manager`.
No customer access. The user also explicitly authorized renaming the GitHub
repository, local folder and VS Code workspace references, and an application icon.
Sufficit.Blazor.UI remains the binding design system; not MudBlazor or legacy
Material. Product facts are captured in PRODUCT.md. Visual implementation follows
the established SUI world with operational fleet-first structure, optional sound,
node transitions and reduced-motion support, not a replacement brand for Sufficit.

## Validation so far
- Build/Release publish: zero errors/warnings. 21 deterministic checks pass.
- Browser fixtures: desktop 1440, mobile 390, light/dark, node selection, details,
  sound opt-in, no mobile overflow, reduced-motion disables chart animation.
- Detector ran once: []. Independent reviewer requested history bounds, isolated
  point visibility and loading distinction; all three fixed and scored resolved.
  Verdict pass `ship` covers those fixes only, not real login or deployment.
- GitHub rename succeeded; origin, local directory and VS Code entry point use
  sufficit-telephony-panel. Old startup/Material/gRPC implementation removed from
  active source (recoverable from Git); new net10 host reuses the local SUI project.
- Identity userinfo implementation returns current persisted roles for the roles
  scope. This is the authoritative permission revalidation endpoint.
- Dedicated OIDC client still needs provision/verification; no new role grant.
- Identity DCR uses camelCase DTO fields rather than RFC snake_case. The first
  request was accepted as an empty generated client (no requested grants or redirect
  URIs); its returned identifier was not captured. It needs administrative inventory
  and targeted cleanup after exact identification. Do not delete other DCR clients.
- Corrected request for the named client was rate-limited, then rejected because
  anonymous registration cannot request `roles`. No role relaxation or fallback
  authorization is permitted. Finish through authenticated management provisioning.
- UI/build/test deployment: implemented and verified as below. Real manager login
  and persisted-role revocation still require the dedicated Identity client.

## Eveo deployment checkpoint — 2026-09-10 12:52 BRT
- Live release: /opt/sufficit-telephony-panel/releases/eveo-20260910-1551.
  SHA256 archive: e3c654a338c33f0301011330696664edf0b12ecd9c052e8fa6b45131a5800e27.
- Dedicated sufftelpanel user, hardened systemd unit, memory maximum 512 MiB,
  Kestrel Unix socket; no new public TCP listener. Nginx www-data socket ACL tested.
- Initial systemd LoadCredential failed (LXC 243/CREDENTIALS), before application
  startup. Replaced with root:sufftelpanel 0640 credential, directory 0750; no
  broadening of container permissions. Initial four retries are historical failures.
- Initial separate Nginx server_name conflicted with existing sufficit-checkout.
  Removed its enabled symlink; added only the panel path to the existing TLS server.
  Updated checkout/ops/sufficit-checkout.nginx with optional snippet include.
  Nginx configuration validates without warnings; graceful reload only.
- HTTPS /health=200, anonymous /api/fleet=401. Production ?preview=true remains the
  login gate, not fixtures. Browser loads SUI/assets and connects Blazor WebSocket.
- Direct metrics read as the service account over VPN/TLS succeeds for Apoint,
  Eveo and Google. This verifies upstream access, not yet an authenticated UI flow.
- Existing PIDs unchanged: portal 1534607, Identity 2297195, observation 1558094.
  No PBX command/reload/restart was performed. Panel observed around 80–100 MiB
  before sustained load; this is not a large-scale load benchmark.
- Real login reaches Identity's PAR endpoint, which reports invalid_client for
  the not-yet-provisioned name. Added fail-closed error handling so /login redirects
  to a useful login-failed screen instead of an unhandled blank 500. All 21 tests
  passed again and Release publish succeeded before the new panel-only restart.
- DESIGN.md and .impeccable/design.json document actual SUI colors, responsive
  layout, accessible animation and opt-in sound; reviewed screenshots are fixtures.

## Provisioning follow-up — 2026-09-10 14:04 BRT
The supplied short-lived token successfully authorized inventory, preview and apply.
Preview and apply each returned exactly one Create for sufficit-telephony-panel.
Token was consumed through a non-echoing process input; no credential file or
source/config entry was created. The process exited after the apply request.
The next browser check returned unauthorized_client ID2183: .NET automatically
used PAR while the manifest provisioner grants only authorization/token/end-session.
Configure this app to use standard code+PKCE (PushedAuthorizationBehavior.Disable),
rebuild and restart only the new panel. No Identity permission expansion is needed.

## Remaining validation / next safe action
DNS/HTTPS follow-up completed: public entry https://panel.sufficit.com.br/ .
Both original Eveo and new panel callback URLs are in the pending client manifest.
See docs/activities/202609101359-panel-dns.md for exact deployment evidence.

Provisioning is now complete; do not request another token for the same create.
Release eveo-20260910-1705 deployed, archive SHA256
c3db132e826eeff2aee31f731bb9e4964aa8bf730011c8ca4869c99ebc79b305.
All 21 regression checks passed again; publish and git diff --check passed.
Browser reached Identity Sign in with client_id=sufficit-telephony-panel,
redirect_uri=https://panel.sufficit.com.br/telephony-panel/signin-oidc,
response_type=code, scopes openid/profile/roles and code_challenge_method=S256.
Anonymous API remains 401 and health 200. Only the new panel was restarted.
Next: user signs in through https://panel.sufficit.com.br/ with a real manager
account; verify full callback/session/metrics path, non-manager denial and live
revocation. No user credentials or manager session are available to the agent.
Keep the plan open and do not claim full authenticated acceptance from a login page.
The accidental empty DCR client still requires precise identification and separate
administrative read/delete capability; the supplied preview/apply token does not
authorize deletion. No other client was modified/deleted. For future credential
requests always give the full prefilled link described in deploy/README.md.
No commit/push was performed. No token was saved in repository/configuration files.

## Continuity
This is the owning implementation plan following the user's explicit selection of
this repository and new service name. The earlier portal plan remains a pointer;
no implementation work is assigned to the main portal in this phase.
