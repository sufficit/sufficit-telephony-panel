# Support chat (Hermes)

When access is insufficient, the panel opens the Hermes web chat (Chatwoot SDK, inbox
"Atendimento") with the context support needs, instead of sending the user elsewhere.

| Situation | Where | `painel_motivo` | `painel_pedido` |
|---|---|---|---|
| Identity signed in, panel denied | "Talk to support" on the denial page | `login-denied` | `telephony.panel:<known context>` |
| No supervision permission for the company | "Request access from support" beside the block reason | `monitor-missing` | missing `telephony.panel`/`telephony.monitor` grants |
| No preferred supervision extension | same | `preferred-extension-missing` | register the preferred extension in the portal |

Contact attributes also carry `painel_usuario` (Identity `sub`), `painel_contexto`,
`painel_permissoes` (current grants) and `painel_referencia` (release, UTC time, path and, for
channels, node + endpoint). Contact name/email are set with `setUser` only together with
`identifier_hash` (HMAC-SHA256 of the `sub` with the inbox secret, computed on the server); without a
verified subject the chat stays anonymous.

Security: context is built on the server (Blazor circuit or the protected 10-minute
`__Secure-Sufficit.Telephony.Panel.Support` cookie read by `GET /support/denied`); no tokens, cookies,
claims dumps or call data are sent. The HMAC secret is read from `Support:HmacSecretFile` and never
reaches the browser; without it the buttons are hidden.

Configuration: `deploy/60-support-chat.conf`. Secret source of truth: Identity Vault
`hermes/web-widget/atendimento-hmac` (context `global`). Tests: `tests/SupportChatChecks.cs`.
