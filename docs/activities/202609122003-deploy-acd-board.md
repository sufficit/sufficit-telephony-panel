# ACD monitoring deployment

## Objective and starting state
Publish the approved ACD source integration so the Google 6599 pilot appears in
the Sufficit context on the operations board. The panel was still running
queue-flow-20260911-1503; Google controller 0.9.0 exported queue totals without
visits. Asterisk, dialplan, DIDs and real agent dispatch must remain untouched.

## Delivered
- Panel 0.12.0 on Eveo Apps, release
  `/opt/sufficit-telephony-panel/releases/acd-board-20260912-2010`, PID 938163.
  DLL SHA256 `e56b9ab4deb8d34bace7ea4c07abe773d774c1cbdecec36838a1d855a92b1e66`.
- Google controller 0.10.1, .NET 7, release
  `/opt/sufficit/acd-google-pilot/releases/acd-board-20260912-2010`, PID 28057.
  DLL SHA256 `453b3e0aa0ff387ca206e4f3183d5ab403a21db092824854862153cc6735f976`.
- Distinct snapshot-only key, root-owned 0640 with the respective service group:
  Google `/etc/sufficit-acd-google-pilot/monitoring-key`; Eveo Apps
  `/etc/sufficit/telephony-panel/acd-google-monitor.key`. No secret in Git, output,
  browser or command-line arguments. Local transport copy removed after transfer.
- Panel source confined to Google VPN 172.19.254.250:18189, context
  `d21cfb049d37473b837c67591a26feed`, node `01a091d2251c7b128c3ff0529ed1a192`.
  Configured by panel-only `60-acd-board.conf`; manager authorization unchanged.
- Additive schema 007 on **sufficit_acd_shared**, with protected SQL backup on
  Eveo VoIP. Existing queue configuration hashes matched before/after. No member
  UPDATE permission expansion. Old Apoint/Eveo runtime binaries remain unchanged.
- Google `ACD_ENABLE_AGENT_DISPATCH=0`, management read-only=1, queue options and
  fallback unchanged. Pilot remains 30-second wait, capacity two, options `{}`.
  No presence/media file was enabled and no agent or audio acceptance was performed.

## Decision and recovery notes
The exact deployed 0.9 source checkpoint was not available; binaries were not
decompiled or patched. The complete tested 0.10.1 runtime was published with its
required additive schema, preserving existing disabled dispatch and queue options.
This does not constitute homologation of the new dispatch/media capabilities.

Backup preflight first found no dump tool on PATH. The existing extracted tool at
`/var/tmp/sufficit-acd-options.vF2THV/backup-tools/usr/bin/mariadb-dump` was used;
no schema change occurred before the successful backup. Google's old systemd does
not accept `--value`; the helper was corrected before any service replacement.
Both failed preflight directories were retained with explanatory suffixes.

## Verification
- Panel tests: **681 checks passed**. Full synthetic ACD integration suite passed.
- Release publishes: panel net10 and controller net7 succeeded; known net7 EOL
  warning remains. Local/remote primary DLL hashes match.
- Before Google replacement: snapshot had zero active visits and the pilot ARI
  application had no channels or bridges. Rechecked idle immediately before restart.
- Only isolated Google controller and Eveo Apps panel restarted. Actual Asterisk PID
  remained **23098**; AMI observation process remained **3700714**.
- Monitor key snapshot 200, management GET/PUT 403, wrong context 404, invalid key 401.
  Seven queue definitions, including pilot `01a091d2251c7f30a962045bef466dcc`,
  and a visits collection are exported.
- Published AcdSourceProbe, compiled with the real AcdBoardReader, ran as
  **sufftelpanel**, bound source settings from the actual drop-in and accepted the
  fresh live Google snapshot and pilot identity. No caller data printed.
- Public `/health` 200, anonymous `/api/fleet` 401, `/board` 200. Panel NRestarts 0;
  both updated services active; no error-priority journal entries in the check window.
- Old Eveo/Apoint controller snapshots both connected, seven Sufficit queues.

## Rollback
Protected baseline directory on all three affected hosts:
`/var/backups/sufficit-acd-board/acd-board-20260912-2010`.
The versioned helper `deploy/deploy-acd-board.py` has explicit host-checked
`google-rollback` and `panel-rollback` phases. Those revert only the corresponding
service override/release and restart that service. Google old `/node` binary is
preserved; panel prior release is queue-flow-20260911-1503. Board rules, remembered
resources, Identity configuration and credentials remain outside releases.

Do not drop schema 007 or restore SQL blindly after new writes: its additions are
backward-compatible with the retained binaries. The helper is a one-time rollout,
not idempotent; a new deployment requires new release/baseline identifiers.

## Boundaries
No real telephone call was originated and no manager session was impersonated.
The user still needs to repeat 6007 -> 6599 to confirm the live UI during their
call; backend export, source binding and local browser fixtures were verified.
Only Google is configured as an ACD board source in this release. Real audio,
agent availability and global dispatch remain separate acceptance work. No new
Standard API deployment or live supervision acceptance was included. No commit or
push was requested/performed.
