# Eveo test deployment

> Current-state note (2026-09-14): this document keeps detailed deployment and
> rollback history. The canonical product/runtime status is
> [docs/system-overview.md](../docs/system-overview.md). Manager sign-in and real
> Listen audio have since been exercised; live revocation and Whisper acceptance
> remain open. Later releases supersede the version summary below.

Only eveo-apps is authorized. Public entry:
https://panel.sufficit.com.br/ (served directly, without a path prefix). The original
https://eveo-apps.sufficit.com.br/telephony-panel/ remains available. Kestrel uses a Unix socket;
Nginx handles TLS. Normal panel deployments do not restart PBXs, portal or Identity.
The separately approved token-format correction below restarted only Identity,
sequentially across the three issuer replicas.

## Identity
Provisioned on 2026-09-10 using the supplied operator token: inventory/preview/apply
created only sufficit-telephony-panel. Both documented callback URLs are registered.
Login reaches Identity with authorization-code + PKCE S256. Real manager access has
been exercised; live revocation still needs explicit validation. Anonymous API
access is denied. The request-link instructions below remain the standard for
future changes, not a request to issue another token now.

The application requires the exact current `manager` role. A dedicated public
OIDC client uses authorization code + PKCE and explicit consent, with no client
credentials grant, no shared cookie and no reusable browser client secret.
PAR is explicitly disabled in this client: the current Identity manifest provisioner
grants authorization/token endpoints but not the pushed-authorization endpoint.
The standard authorization-code flow still requires PKCE; this is not an insecure
fallback after a failed request, nor a change to Identity's shared configuration.
Tokens stay in a server-side bounded ticket cache (200 sessions). Access-token
expiry requires signing in again; refresh tokens are deliberately not requested.
Role decisions are refreshed through UserInfo after at most 60 seconds. An open
panel rechecks on its 30-second refresh, so revocation visibility can take up to
90 seconds. Cached allowed decisions are not extended during Identity failure.
No general-purpose tenant entitlement grants or new manager assignments are made.

Use the existing authenticated management API to provision `identity-manifest.json`:
`python3 deploy/identity-provision.py --token-file /secure/operator-token --apply`.
The token must be a short-lived provisioning credential issued to an authorized
operator. Never paste credentials into a command argument or commit a token file.
Anonymous DCR rejects `roles`; never relax that allowlist to deploy this panel.

### Requesting an operator token

Always give the operator a complete, clickable, prefilled link, not the bare token
page followed by manual instructions to locate capabilities. For this panel:

[Prepare panel provisioning token (15 minutes)](https://identity.sufficit.com.br/management/tokens?action=issue&purpose=Provisionar%20sufficit-telephony-panel%20no%20Eveo&lifetimeSeconds=900&capability=identity.provisioning.preview&capability=identity.provisioning.apply)

The supported query contract is `action=issue`, URL-encoded `purpose`,
`lifetimeSeconds=900`, and one repeated `capability` parameter for each exact
permission. Change the purpose and use only the necessary capabilities for future
requests; never add unrelated permissions or embed any token/secret in the URL.
This link requests only `identity.provisioning.preview` and
`identity.provisioning.apply`. It does not include client deletion permissions.
Opening it only prepares the form: login/MFA and the operator's explicit issuance
confirmation are still required. The URL itself is not a credential.
A token cannot
grant more access than the issuing operator already has. Save it outside Git with
mode 0600 and pass only the path to the helper. Role `manager` for viewing the panel
does not itself authorize Identity administration. See PLAN-telephony-panel.md for
remaining authenticated validation and the narrowly scoped DCR cleanup.

## Packaging and service

Current panel release: **0.12.2**, deployed on Eveo Apps. It also resolves idle
extension peers without requiring an active call; see
[idle-peer evidence and rollback](../docs/activities/202609122052-idle-prefix.md).
Version 0.12.1 added direct listening
for a single eligible channel/prefix and server-revalidated concrete endpoint
prefixes; see [validation and rollback](../docs/activities/202609122036-direct-supervision.md).

The ACD monitoring integration introduced in **0.12.0** remains configured, with Google's
private snapshot source and a distinct snapshot-only credential. This deployment
also updated only Google's isolated ACD controller to 0.10.1 and applied the
required additive ACD schema; no Asterisk restart or dialplan change. See
[release evidence and rollback](../docs/activities/202609122003-deploy-acd-board.md).
`deploy-acd-board.py` is an explicit one-time host-checked helper, not a generic
redeploy command. Its `probe` phase runs the real source reader as sufftelpanel;
the probe project is `deploy/verification/AcdSourceProbe.csproj`.

Version 0.10.3 uses `contextid` for company filtering (legacy `company` links are
normalized) and lets trunks/queues share available desktop height before scrolling.
The board stays mosaic-only even after a resource is selected:
details and channel confirmations open in a bounded popup, including fullscreen.
Normal-page/settings links open another tab. Filters and display controls open
from the icon beside the extension count. Closing the popover preserves URL-backed
filters. The normal root page keeps navigation, help, settings and retention.
This is an application-only deployment; no nginx, Identity or PBX change is needed.

Version 0.8 adds the default-on remembered-resource inventory, stored separately at
`/var/lib/sufficit-telephony-panel/known-resources.json` (0600). Never overwrite or
delete this state during deployment. It is created by the first authorized board
observation; no fixtures are seeded. See [retention semantics](../docs/retained-resources.md).

### Board presentation rules

Version 0.7 adds the manager-only **Configurar mesa** editor. Shared presentation
rules are stored in `/var/lib/sufficit-telephony-panel/board-rules.json` (0600),
under the existing 0700 StateDirectory owned by sufftelpanel. Keep this state
outside releases and never overwrite it during deployment. No file or rules are
created until the first explicit save. `Panel:BoardRulesFile` overrides the path
for isolated environments. This is a single-instance Eveo store, not distributed
configuration storage. See [rules and concurrency](../docs/board-configuration.md).

The editor checks the current manager session on reads/writes and uses revision
conflicts rather than overwriting another manager's changes. Browser fixture
saves are memory-only; do not confuse those tests with a real authenticated save.

### Identity access-token format

AMIEvents currently validates signed JWTs, not opaque reference access tokens.
`identity-token-format.json` is the exact-client delta required in each issuer's
`appsettings.Production.json`: only `sufficit-telephony-panel` becomes `Jwt` under
`Sufficit:Identity:Tokens:AccessTokenFormatsByClient`. It is NOT a full replacement
configuration. Keep other clients, resource rules and the reference-token fallback.

The 2026-09-10 rollout applied the delta to Eveo/Apoint/Castrum using
`identity-token-format.py <configuration-path> <preflight-sha256>` on the target
host, under the cluster deployment lease. The helper merges only the exact rule,
rejects stale hashes/symlinks/conflicting values and atomically retains owner/mode;
configuration contents and secrets never pass through the workstation.
`--rollback` removes only this rule; use it only when this rollout added it, then
restart the issuer after explicit approval. No host backup was created.

The Identity format policy is a startup singleton. Restart issuers one at a time
with /health and /health/ready gates; compare signing JWKS before/after. All three
passed the approved rollout without restarting panel, AMI or Asterisk. Existing
panel sessions still hold opaque tokens: sign out/in to obtain the new format.
Authenticated hub/company acceptance remains required; a 302 login is not proof.

### Application release

From 0.10, panel.sufficit.com.br serves `/` and the dedicated board `/board`.
The former `/mesa` route permanently redirects to `/board`, retaining query filters.
This release only requires a panel update: no new nginx, Identity or PBX changes.
The language selector defaults to English and persists `en` or `pt-BR` under
`telephony-panel-language` in localStorage. Fullscreen targets only `board-focus`,
including its filters and resource inspector, never the entire document.

For first-time migration from releases before 0.9:
Install `root-path.conf` as the panel-only systemd drop-in, use the updated
`nginx-panel-domain.conf`, and replace the old shared-host snippet with nginx.conf.
Both preserve old URLs and query strings via redirects to the dedicated host.
The exact `/telephony-panel/signin-oidc` callback remains proxied, NOT redirected:
it is already registered in Identity. The UI/assets use root; no Identity grant
or client edit is needed. Login validates a local returnUrl to restore filters.
Existing path-scoped sessions may need login again after migration.
Run nginx -t before graceful reload; restart only the panel. Keep private host
configuration and external board state. Never overwrite a running release.

`dotnet publish src/Sufficit.Telephony.Panel.csproj -c Release -o artifacts/release -m:1`
excludes workstation appsettings variants. Transfer the resulting directory into
a new `/opt/sufficit-telephony-panel/releases/<release-id>`; never replace DLLs in
a running release. Update the `current` symlink only after checks.

Use the dedicated sufftelpanel system account and deploy the supplied unit. The
metrics password is root-owned, group sufftelpanel, mode 0640 in
`/etc/sufficit/telephony-panel/metrics-password` (directory 0750). Eveo's LXC rejects
systemd LoadCredential mounts with status 243/CREDENTIALS; no sandbox permissions
were broadened to bypass that limitation. Queries connect to 172.19.2.136 via VPN
with TLS/SNI validation for metrics.sufficit.com.br. The source credential currently
belongs to the central metrics service; replace it with a query-only credential
when central per-consumer authentication is available. It never reaches browsers.

The supplied Nginx snippet adds only /telephony-panel to the existing Eveo TLS
server in sufficit-checkout; validate `nginx -t` before a graceful reload. The local
checkout ops/sufficit-checkout.nginx has the matching optional snippet include.
The dedicated nginx-panel-domain.conf site reuses that snippet and the existing
wildcard certificate. Route53 uses an A alias to eveo-apps.sufficit.com.br, created
by sufficit-services-dns/create_panel_dns.py; no independent IP is hardcoded.
The existing service unit/main portal remains untouched. Rollback switches `current`
back to a verified previous release and restarts ONLY sufficit-telephony-panel.

## Validation
Operational monitor activation and limits are in
[operational-monitor.md](../docs/operational-monitor.md). The current manifest
adds `entitlements` to the original client registration. Applied on 2026-09-10:
the second preview reports no changes and Identity accepts the scoped request
(302 to login, formerly ID2051). `Panel__RequestEntitlements=true` is active in
the supplied/local and Eveo unit. Only panel was restarted; sign in again for
new consent and user-claim validation. For future changes requiring a fresh read,
the full issuance link is:

[Prepare telephony authorization update (15 minutes)](https://identity.sufficit.com.br/management/tokens?action=issue&purpose=Habilitar%20entitlements%20no%20sufficit-telephony-panel&lifetimeSeconds=900&capability=identity.scopes.read&capability=identity.provisioning.preview&capability=identity.provisioning.apply)

The scope-read capability is needed to preserve the existing shared entitlements
scope definition. Preview rejects an undeclared custom scope, and declaring guessed
defaults would overwrite it. Do not apply that change blindly. The manager-wide
central AMI view in 0.4 works independently of this optional scope activation.

The manifest now includes the exact shared scope definition read before apply:
three resources, unchanged name/display/description, no scope-granted entitlement
properties. All 14 prior clients remain; the panel is the fifteenth. Provisioning
also adopted the scope's management metadata. This is a shared definition, not
panel-exclusive: re-read and preserve future changes before another apply. Do not
delete the shared scope when removing the panel. Its legacy NOT YET ACTIVE
description was deliberately preserved; the deployed Identity build
`c79f5bebcbfdcd72d727152119b779ec722ff199` accepts the successor scope. Acceptance
of an authorization request does not prove this particular user's phone claims.

The link grants no permission by itself. Request only the private file path from
the operator, never a pasted secret. Do not reuse the previous one-time token.

`dotnet run --project tests/Panel.Tests.csproj`
`GET /health` reports process availability, NOT upstream health (old path redirects).
Anonymous `/telephony-panel/api/fleet` must return 401; ordinary users cannot read
fleet metrics. Real manager login must be checked after Identity provisioning.
Development-only `/?preview=true` renders clearly marked synthetic data; it cannot
be enabled by a query string in Production.
