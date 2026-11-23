# Do not disturb likely on extension cards

Date: 2026-11-23 17:30 -03.

## Motivation

Customers enable "Do not disturb" on the phone or softphone without noticing and
then open support tickets because queues do not ring. On 2026-11-23, on
google-voip, the agents of one customer's queues answered about 68 thousand queue
offers with busy in a single day. The agents were free, and a short SIP capture
showed MicroSIP answering `486` with `Reason: SIP ;cause=486 ;text="Do Not Disturb"`.
Server-side DND (FreePBX `*78`, AstDB `DND/*`) is not in use: `database show DND`
returned no results.

## Change

- `sufficit-ami-events` 1877f94: `LiveProjection` records a `call-refusal` row
  per endpoint. The row is created when an endpoint leg hangs up with cause 17
  within 3 s, never rang, and the endpoint had no other call on that node. The
  row counts repeats and keeps only the accountcode. It lasts 15 minutes after
  the last refusal and clears when the endpoint rings again on the same node.
- `sufficit-telephony-panel` 2bb8ba5: the extension card turns red with
  "Não perturbe provável". It shows "Recusou chamadas estando livre (n)", the time
  of the last refusal and a hint to check the phone or app. An active call takes
  precedence. The row has the same tenant ownership as dial failures.
  Documented in [operator-board.md](../operator-board.md#não-perturbe-provável).

The AMI exposes only the cause, so the card states likely DND, not a device read.

## Validation

- Panel checks: `PASS: 1718` (new `CallRefusalChecks`: ownership, counting,
  expiry, clearing on ringing, and the non-refusal cases). Streaming checks: `PASS: 42`.
- Visual: `?preview=true&refusalpreview=1` rendered the red card with count,
  time and hint, with no page errors.

## Delivery

Release `dnd-refusal-20261123-2bb8ba5` on eveo-apps.
DLL SHA256 `37d86a8a570f6cf82e2215115ba61020ecb954a9aa7990d77ad2490e649b18d4`.
The `appsettings.json` from the previous release was copied on the host and
confirmed identical.

The first activation failed. Staging ran `chown -R` with the release directory as
the reference, which changed `appsettings.json` from `root:sufftelpanel 0640` to
`root:root`, so the service could not read it. `release-panel.py` rolled back
automatically, and the panel was unavailable for about 20 s (17:25). The fix
copied each file's owner and mode from the previous release. New files got
`root:root` 0644 or 0755. The leftover backup manifest of the failed attempt was
removed.

The second activation passed: `current` points to the new release, NRestarts=0
and public health returns 200. The published CSS contains `tone-refusing`. No
errors were logged after activation. Nginx, Identity, PBXs and collectors were
not changed.

When staging a release, preserve per-file ownership: `appsettings.json` must stay
`root:sufftelpanel 0640`. Do not use `chown -R --reference`.

## Compact card (3b91044)

The owner confirmed the warning on real cards in production: the agents from
0006676005 to 0006676009 on google-voip. The first layout added three lines and
broke the mosaic. Release `dnd-compact-20261123-3b91044` keeps the standard card
height (62 px, the same as neighboring cards in the preview). It shows a red
border, a do-not-disturb glyph in place of the dot and a `DND` tag on the title
row. The status line reads "Não perturbe provável · n recusas", and the tooltip
carries the count, the time of the last refusal and the hint. Staging copied
each file's owner and mode from the running release before activation.
Activation passed: NRestarts=0, public health 200, the published CSS contains
`tile-tag`, and no errors were logged.
DLL SHA256 `272552de4ea803bd552770e335d7aee07d1fa9886859bc6bca14dd7e7ea1fcad`.

## Open

- Queue retry load caused by DND phones is a separate `sufficit-ari` item
  (per-member backoff after busy). It is not part of this change.
