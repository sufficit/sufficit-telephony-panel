# Board supervision

The extension, queue and trunk card menu opens by right-click, the visible action
button, or the keyboard context-menu key/Shift+F10. Details and supervision open
over the board without navigation or changes to its filters. Labels use the
existing English/Portuguese localization and SUI controls.

## Operator flow

1. Select listening or whispering from the card menu. A single eligible listening
   target starts immediately, without another selector or confirmation. Multiple
   candidates still require selection. A single whisper target is preselected but
   retains confirmation because the microphone is transmitted into the call.
2. The service calls the user's linked preferred telephone
   endpoint; audio is not played in the browser.
3. Answer that telephone. Listening does not transmit the supervisor microphone.
   Whispering transmits to the selected channel leg, not automatically to an agent:
   select the agent leg when assistance to that agent is intended.
4. Hang up the supervision telephone to stop. Closing the browser dialog does not
   terminate an accepted telephone session. Requests are not automatically retried
   after an ambiguous failure.

Exact-channel supervision currently accepts fresh SIP/PJSIP channel observations.
With the accompanying Standard change, it stops when that channel ends rather
than advancing to another call. Continuous mode follows matching channels on one
node, one at a time, including future calls. It is not a simultaneous audio mix.
The telephone's `*` key advances to the next matching channel. Sessions have a
30-minute absolute limit. Unbridged channels are eligible, including waiting audio.
These behaviors follow the [Asterisk 18 ChanSpy options](https://docs.asterisk.org/Asterisk_18_Documentation/API_Documentation/Dialplan_Applications/ChanSpy/).

## Protected prefix configuration

Display grouping and filter expressions are **not** authorization for continuous
listening. From 0.12.1, a PEER card with its own fresh SIP/PJSIP channel can resolve
the concrete endpoint prefix automatically: `SIP/0000006001-xxxxxxxx` resolves to
`SIP/0000006001-`. The server re-queries scoped portal cards and requires an exact
PEER endpoint identity before originating. The portal's leading `^` anchor is
removed for literal comparison; wildcard/regex patterns never grant this right.
Related call legs, labels, unknown/stale accounts and shared trunk/queue cards
cannot establish endpoint ownership. Cross-context observations still reject the
prefix. Each automatic submission runs once after the menu click, with existing
authorization, preferred-extension checks, throttling and no automatic retries.

Version 0.12.2 also resolves **idle peers** from the connected live projection;
there does not have to be a call to start continuous listening. A peer event often
has no accountcode, so the selected context supplies the scope and the server
re-queries its authorized PEER catalogue before originating. Without a selected
context, an explicit unambiguous observed account is still required; the context
is never guessed from an extension number. An old last-state-change timestamp on
a current peer is not treated as an expired call. Stream disconnection, future
timestamps, conflicting explicit accounts and remembered-only cards remain blocked.
Exact-channel listening still requires a fresh active channel.

Queues, trunks and other continuous routes still require private configuration
binding each rule to one context, node and exact authorized card key:

```json
{
  "Panel": {
    "SupervisionPrefixes": [
      {
        "Id": "example-extension-6007",
        "ContextId": "11111111-1111-1111-1111-111111111111",
        "Node": "example-test-node",
        "CardKey": "PJSIP/6007",
        "Prefix": "PJSIP/6007-"
      }
    ]
  }
}
```

This is synthetic documentation, not a production mapping. The configuration must
describe routing exclusively belonging to the authorized context. A shared trunk
prefix serving several contexts must not be enabled. The panel rechecks manager
access, telephone entitlements, current authorized API cards, the supervisor
endpoint, fresh observations and per-session throttling before sending a request.
Observed channels with another explicit context block the rule. This observation
check does not prove the ownership of future traffic: exclusive routing remains a
required configuration invariant. There is no new PBX SPYGROUP enforcement or
mid-session entitlement revocation in this change.

Prefixes are concrete technology/endpoint strings with a trailing channel
delimiter, not regular expressions or globs. The delimiter prevents extension
6007 from also matching extension 60070. Queue display names and queue IDs must
never be inferred as channel prefixes. A queue requires a verified dedicated
channel mapping, such as a Local route exclusive to that queue/context. Only
channels actually matching that route are included; a queue's entire call graph
is not automatically covered by one prefix.

## Delivery and validation boundary

Local implementation version: panel 0.11.0. Deploying only the panel is insufficient
for the new exact-channel termination and unbridged-channel behavior: rebuild and
publish the API consumer with the updated `Sufficit.Standard` monitor service.
The monitor dialplan remains unchanged. Existing production services and private
prefix mappings were not changed during this task.

Automated validation covers target-policy rejection, mode option construction,
authorization/concurrency regressions, menu interactions, keyboard behavior,
EN/PT, desktop/mobile, and no telephone API requests from the local demo. Synthetic
preview confirmation deliberately does not originate a call. An accepted API
request is not evidence of connected audio. Real SIP/RTP listening, microphone
direction and channel-to-channel continuation still require isolated telephone
acceptance before enabling production mappings.
