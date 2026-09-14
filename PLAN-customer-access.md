# Context-scoped customer access

## Objective and acceptance
Implement `telephony.panel` and `telephony.monitor` as exact contextual grants,
resolved from Identity and cached server-side, never carried in new tokens.
Support multiple contexts, an explicit Guid.Empty wildcard grant, automatic
single-context selection, and independent Listen/Whisper authorization. Preserve
internal manager operations. Customer data must be filtered before rendering.

## Terrain and constraints
Panel currently uses manager-only sessions and a shared fleet stream. Endpoints
currently uses phonecalls for cards and actions. Identity exposes current UserInfo
but no dedicated self entitlement query. Existing role/group claims need explicit
resolution; SCIM groups do not implicitly confer authorization. Repositories were
clean at start. Keep tokens, claims and operational data out of logs and Git.

## Ordered checkpoints
1. [completed] Implement and test Identity self entitlement resolution and token
   exclusion, plus the canonical entitlement definitions.
2. [completed] Implement bounded Panel authorization cache, tenant projection and
   separate supervision checks; align required API consumers.
3. [completed] Adapt single/multiple/global context selection and protect shared
   settings/infrastructure; verify UI and cross-context isolation.
4. [completed] Correct Identity business coupling: remove product keys and GUID
   interpretation, configure generic delivery rules outside the public project,
   and test/document the provider boundary (user's new priority).
5. [pending] Validate builds/regressions, document migration and deployment
   prerequisites, deliver through the existing workflow where available.

## Decisions
Resizable sections completed locally; evidence is in
docs/activities/202609141431-board-resize.md. Customer-access delivery remains
pending without dropping its remaining checkpoint.
Scope requests identify the subject; exact entitlement grants come from current
Identity data. Unknown context evidence is hidden from contextual users. Global
view and global monitor grants are independent. No legacy grants are silently
converted. No arbitrary user identifier is accepted by the self-query endpoint.
Identity is a public, business-agnostic provider. Product entitlement names and
context/wildcard semantics belong to consumers and deployment configuration,
never hard-coded Identity defaults or validation rules. Existing customer-access
delivery remains pending while this architectural correction takes priority.

## Validation
Identity: 13 focused policy checks and the real in-process token/UserInfo flow pass.
The flow proves inherited grants and revocation without replacing the token.
Panel and Endpoints builds pass (Endpoints has pre-existing dependency warnings).
Identity tests use `-p:SufficitUseLocalSui=false` to match restored package assets.
Panel: 803 automated checks pass. New checks cover independent wildcard grants,
cross-context projection and same-named endpoints; anonymous settings denial is preserved.
Browser: customer single/multiple/global fixtures pass, including URL reload,
view-only menus and 390px mobile. No live phone actions were performed.
Test empty/single/multiple/wildcard sets, inherited grants, expired caches,
revocation, subject mismatch, malformed responses, foreign-context URL, retained
resources, ACD data and direct action calls. Existing manager regressions must pass.
