# Panel JWT compatibility — approved configuration rollout

## Result

The per-client `Jwt` rule for `sufficit-telephony-panel` is active in the three
Identity issuers: Eveo, Apoint and Castrum. The user explicitly approved the required
Identity restart after the opaque-token/JWT mismatch was diagnosed. Only those
Identity processes restarted, sequentially; panel, AMI collector and PBXs were
not restarted or reloaded. No user roles, tenant grants, global token defaults,
scope audiences, signing keys or other client-format rules were changed.

This closes the configuration correction, not real-user acceptance. Tokens already
issued remain opaque. Sign out/in at https://panel.sufficit.com.br/telephony-panel/
and confirm authenticated hub negotiation, events and optional company filtering.
The live operations plan remains open for that acceptance and historical snapshots.

## Evidence and execution

- Started from Identity build `c79f5bebcbfdcd72d727152119b779ec722ff199` on all nodes.
- Readiness, health and signing JWKS matched before mutation. Preserved the existing
  Eveo-only `SufficitBlazorServer` rule; unrelated drift was not normalized.
- Added the secret-free `deploy/identity-token-format.json` delta, an atomic,
  SHA-guarded merge helper and three regression tests in the panel repository.
- Held the coordinator `/run/lock/sufficit-identity-cluster-deploy.lock` lease;
  restart operations used `/run/lock/sufficit-identity-deploy.lock` on each host.
  Active installs are direct directories, incompatible with the existing release
  helper's release-root guard; no release switch was attempted.
- Configurations were merged in place on their respective hosts, retaining 0640
  and dotnetuser ownership. No secrets were copied locally and no server backup
  was created. Lease released after verification.
- `/health` and `/health/ready` passed after each restart. Zero error-level journal
  records since each node's new startup. Public Identity and panel health: HTTP 200.
- Anonymous panel fleet: HTTP 401. Login: HTTP 302 with unchanged requested scopes
  `openid profile roles entitlements`. These are not authenticated event tests.
- Three Python regression tests and 51 panel checks pass; `git diff --check` passes.
  No build/binary version changed, and no commit/push was performed in this rollout.

## Process and artifact checks

| Identity node | Previous PID | New PID | Configuration SHA-256 |
| --- | ---: | ---: | --- |
| Eveo | 2538028 | 2586464 | `44d3e6fcd488d6e8f745ac2e638f81aa000078c37f7eef7b4e631a68c83be754` |
| Apoint | 566571 | 1018743 | `ac292cc8e95ec01d798cad6be619792cd524839429cc919ce2731df6f9620e0f` |
| Castrum | 179008 | 209074 | `ac292cc8e95ec01d798cad6be619792cd524839429cc919ce2731df6f9620e0f` |

Eveo panel PID `2556589` and AMI PID `344` unchanged. Signing JWKS SHA-256
`85d43862afae2e2eea21c6668aa9ec34fa0c029ac906cd0eaee9122847aed769`
unchanged on all three issuers.

## Rollback and remaining checks

Remove only the exact panel client-format rule using the merge helper's guarded
`--rollback`, then sequentially restart the approved issuers with readiness gates.
This returns new tokens to the existing fallback; already-issued JWTs retain their
format/lifetime. Do not delete shared scopes or alter signing keys as rollback.

JWT validation in the existing AMI service does not provide introspection-based
immediate revocation. The panel's existing server-side user/role revalidation and
token-expiration checks remain in force. No bypass was added, and no ID token or
administrative token was used as a replacement for the user's access token.
