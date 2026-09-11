# Prefilled Identity token link

User requested a complete link and durable guidance for future credential requests.
Inspected OperatorTokenRequestUrl.cs and OperatorTokens.razor in sufficit-identity.
Updated deploy/README.md with the canonical query contract and ready-to-use link,
900-second validity, purpose and only provisioning.preview/provisioning.apply.
Updated .github/copilot-instructions.md and the active panel plan so future agents
provide full links instead of asking for manual capability selection.

Validation: Node URL assertions against the actual Markdown passed (path, action,
purpose, lifetime, exact repeated capabilities); git diff --check passed.
No token was issued, no server changed, and no authenticated browser flow was claimed.
The broader panel provisioning blocker remains open. The development skill guided
the documentation checkpoint and this record. No commit/push performed.
