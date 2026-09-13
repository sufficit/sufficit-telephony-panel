# Sufficit Telephony Panel

Independent internal PBX observation console built on .NET 10 and
Sufficit.Blazor.UI. Replaces the former sufficit-telephony-blazorpanel host.

Eveo test entry point: https://panel.sufficit.com.br/ . The dedicated Identity client
was provisioned on 2026-09-10 and the login redirect verified. Real manager login
and live revocation validation remain pending (see PLAN-telephony-panel.md).

## Scope

[Monitoring ownership and migration routes](docs/monitoring-ownership.md): Panel is
the telephony observation UI; Blazor retains configuration and management. Queue
stages and registration diagnostics are available at `/queues`.

- Only users with the current Sufficit Identity `manager` role.
- Three PBXs: Apoint, Eveo and Google, using already deployed central collectors.
- Per-node calls/channels, Asterisk RSS, AMI connectivity and internal work queues.
- Bounded 1/6/24-hour history, explicit unavailable/stale states, local opt-in sound.
- Light/dark SUI themes, responsive view, reduced-motion support.
- No PBX control, customer messaging, tenant self-service or direct browser AMI.

## Develop and test
Requires .NET 10 and the sibling `sufficit-blazor-ui` and `sufficit-ami-events`
checkouts (shared telemetry contract and projection in `streaming/`).

```sh
dotnet build src/Sufficit.Telephony.Panel.csproj -m:1
dotnet run --project tests/Panel.Tests.csproj
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Sufficit.Telephony.Panel.csproj --no-launch-profile --urls http://127.0.0.1:5168
```

Local UI fixture: `http://127.0.0.1:5168/?preview=true`. It is visibly synthetic and
cannot bypass authentication in Production. Never configure real upstream secrets
for visual fixture tests. Old workstation appsettings variants are excluded from
publication. Do not deploy the legacy `published/` directory; it is now untracked,
preserved locally and ignored. Always build a fresh release from `src`.

## Security and deployment

The default [operator board](docs/operator-board.md) presents a compact extension
mosaic, known trunks and observed-channel resources, with keyboard-selectable
details. Colors describe evidence, not guaranteed availability. Company remains
an optional filter; central events do not provide trustworthy trunk classification.
[Board configuration](docs/board-configuration.md) lets managers explicitly group
trunk aliases, classify extensions and hide resources, with shared persistent rules,
draft preview and optimistic concurrency. This never modifies PBX configuration.

The live monitor separates [correlated calls and individual channels](docs/call-channel-correlation.md).
Missing Linkedid is explicitly uncorrelated; per-node groups do not deduplicate
calls across PBXs. Supervision still targets an individually authorized channel.

On Eveo, operations telemetry uses one shared server-side gRPC stream from the
existing three-node observation collector through
`/run/sufficit-ami-observation/telemetry.sock` (`deploy/30-telemetry.conf`).
Snapshots are partial observed state, not full PBX inventory or persistent history.
Manager/session authorization still applies to every user; local machine access
does not bypass it. Removing `Telemetry:Socket` explicitly selects legacy SignalR;
there is no silent fallback on a broken private stream. See the sibling project's
`docs/grpc-telemetry.md` for contract, limits, ACLs, deployment and rollback.
See [deploy/README.md](deploy/README.md). First target is eveo-apps only, with a
dedicated system account, Unix socket, Nginx TLS and systemd credential delivery.
The central source credential is server-only; no tokens in JS/localStorage.
Existing token expiry forces fresh sign-in. Identity role revocation is bounded
by a 60-second permission cache and the 30-second panel refresh.

The app icon is `src/wwwroot/panel-icon.png`; its generation prompt/provenance is
adjacent. Application identity does not replace the corporate Sufficit mark.

Live implementation status: PLAN-telephony-panel.md until delivery is complete.

## Worktrees
Keep worktrees under `.worktrees/` inside this repository.
