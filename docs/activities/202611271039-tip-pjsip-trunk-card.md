# TIP Brasil inbound PJSIP in the telephony panel

The existing TIP trunk card now groups the exact endpoint
`PJSIP/xep-65f535a8a9b345d6816363ce4266e668` with TIP's legacy peers. The board
matches the endpoint after removing the channel suffix.

Updated the [versioned rules](../../deploy/fop-board-rules.json) and added
the TIP endpoint assertion to the existing FOP catalogue check. The Panel
test runner passed 1,748 checks and the BoardRules verifier accepted the
catalogue. Restore reported the existing NU1903 advisory for
Microsoft.AspNetCore.SignalR.Common 10.0.11.

On Eveo Apps, verified the updated live rules with BoardRules, then stopped
the panel writer, checked that the baseline was current, installed the new
revision atomically, and started the service. Revision
`01a21d3aff4173d8b0c8d98bdc996061` preserves all other live settings and
the file owner and mode. The local health probe and the public `/health`
and `/board` returned successfully. No real TIP call was observed in the
authenticated board for this change.

The pre-change file is at
`/root/tip-pjsip-panel.I6uyZw4I/board-rules.before.json` on Eveo Apps.
If the panel must be reverted, remove only the new TIP pattern from the
current revision to preserve subsequent edits.
