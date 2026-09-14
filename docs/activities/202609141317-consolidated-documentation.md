# Canonical telephony documentation consolidation

## Purpose

Consolidate the internal Telephony Panel initiative through 2026-09-14 without
changing application code, PBX configuration, credentials or running services.
Earlier delivery produced detailed contracts and dated evidence, but no single
current start-here document. Two root plans and deployment notes still described
initial authentication gaps already superseded by later pilot use.

## Changes

- Added `docs/system-overview.md` as the canonical current product and operational
  guide. It records ownership, access, routes, URL filters, unified resource model,
  calls versus channel legs, retention, registration history, queue configuration,
  queue stages, supervision, transport, deployment, versions, validation, limits
  and the ordered acceptance sequence.
- Added `docs/README.md` as the documentation index and made the historical status
  of activity reports explicit.
- Updated the root README to point to canonical status, recognize real manager and
  Listen acceptance, and describe bounded supervision instead of the obsolete
  blanket statement that the Panel has no PBX actions.
- Marked both old root plans as historical rather than silently rewriting their
  checkpoint evidence.
- Added a current-state note to `deploy/README.md`; existing rollout and rollback
  evidence remains intact.

## Decisions preserved

- Panel owns all user-facing telephony monitoring; Blazor owns configuration.
- Queue/extension identity is unified by context, not duplicated by PBX. Node is
  optional observation provenance, never queue placement.
- Google is the production focus; Eveo and Apoint remain lab environments.
- A channel is a technical leg, and `Linkedid` correlation is per PBX.
- Customer publication is deferred until real runtime/reliability acceptance.
- Secrets, real contact addresses and temporary tokens are intentionally absent.

## Validation and rollback

Validation was documentation-only:

- all local Markdown targets in the changed documents resolved;
- `git diff --check` passed;
- changed-document scans found none of the temporary tokens supplied during the
  pilot, private-key blocks or bearer credentials;
- public `GET /health` returned `running`;
- anonymous `GET /api/fleet` returned 401 as required.

No service deployment was needed.

Rollback is the revert of the documentation commit. No runtime rollback, PBX
reload or state migration is involved.
