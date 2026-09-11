# Central monitor checkpoint — 2026-09-10 15:35 -03:00

This is a deployment checkpoint, not full authenticated acceptance or completion
of the broader operational-monitor plan.

## Delivered

- Panel 0.4.0 starts manager-wide AMI observation without requiring a company.
  The existing hub grants managers ALL; this does not change PBX permissions.
- Optional company selection/clear, endpoint text search, node and evidence-state
  filters. Company search binding updates during typing.
- Registration, qualify reachability and device usage are independent. Last
  registration observed is reception time for an explicit registration event.
  Contact RTT is milliseconds; contact events do not invent registration or prove
  a telephone transport socket is connected. Unknown remains unknown.
- Central groups endpoints/queues without requiring legacy portal cards. Actions
  still require a concrete company and existing API authorization/confirmation.
- Preserved SUI via frontend/Impeccable skills, responsive table access, tokens,
  both themes and independent infrastructure/history view.

## Evidence

51 console checks passed; Debug build and Release publish succeeded. Browser
fixtures verified initial central, text filtering, state filtering, company
search/select/clear, widths 1440/1941/390 and no page overflow. The browser must
await the Blazor render before asserting filtered counts. No real calls made.
Independent reviewer scored the obsolete DESIGN contract resolved and immediate
input binding resolved in source; documentation sidecar updated/validated.

Eveo release `/opt/sufficit-telephony-panel/releases/eveo-20260910-1830` activated
by switching current and restarting only sufficit-telephony-panel. Panel PID
2534188; Identity PID 2469487 and AMI collector PID 344 unchanged. Health 200,
anonymous fleet 401, login 302, no warning logs since deployment. Initial settled
memory 47,837,184 bytes. Production browser ignores preview=true and shows login,
not fixtures or protected central data. Existing previous release retained.

Archive SHA256: `961fcfe131fa99fc85ed037be1706029240288dc66440fe31de9f1502c33da85`.
DLL SHA256 (local and remote): `f1a03b2aa219e5bfbb7fa2abf75497bd9422bd1610b3b60bae75dcf48e8cd576`.

## Pending / protected boundaries

Provisioning preview rejected an undeclared custom entitlements scope. Declaring
guessed defaults would update the existing shared scope, so that change was not
applied. Scope read returned 403 with required permission identity.scopes.read.
The supplied short-lived credential was used in no-echo process input, not saved
to the repo or echoed in output. No Identity mutations were made.

The complete next issuance link in deploy/README.md now includes scopes.read,
provisioning.preview and provisioning.apply. Preserve the existing scope metadata
before any application. Keep Panel__RequestEntitlements false until then.
Manager-wide observation is independent; company-filter API and supervision remain
pending. Real manager data/three-node reception have not been verified from this
agent's anonymous browser. A persistent central snapshot/history is still needed
for extensions silent before subscription and historical registration times.

No PBX restart/reload, Identity restart, collector restart, customer notification,
new user grant, repository commit or push occurred in this checkpoint.
