# Copilot Instructions
<!-- Version: 2.0 -->

* code comments should always be in English;
* identifiers and canonical routes must be English; use `/board`, not `/mesa`;
* UI text must support English (default) and Brazilian Portuguese through PanelText;
* preserve the browser-local language preference and never translate customer data;
* response to user queries should be in IDE current language;
* avoid to change code that was not related to the query;
* when agent has to change a method and it change the async status, the agent should update the method callers too;
* for extensions methods use always "source" as default parameter name
* use one file for each class
* for #regions tags: no blank lines between consecutive regions, but always add one blank line after #region opening and one blank line before #endregion closing

## Identity operator credential requests

When asking the user to issue an Identity operator token, always provide a complete
clickable link with action=issue, URL-encoded purpose, lifetimeSeconds and repeated
capability parameters. Use only the exact required capabilities; never provide only
the bare /management/tokens URL or ask the user to configure the form manually.
The current 15-minute panel provisioning link and verified query contract are in
deploy/README.md under "Requesting an operator token". Explain that the link only
prefills the form and still requires explicit issuance/MFA. Ask for the path to a
private token file, never the secret pasted into chat. Do not mint or expand access.

For the pending telephony entitlement activation specifically, use the complete
link in deploy/README.md under Validation. It adds identity.scopes.read alongside
provisioning.preview/apply so the shared scope definition can be preserved.
Never apply a guessed scope declaration just to satisfy manifest validation.

## #Region Formatting Rules

### Correct Format:#region Properties

    public string Name { get; set; }
    public int Age { get; set; }

#endregion
#region Methods

    public void DoSomething()
    {
        // implementation
    }

#endregion
### Incorrect Format:#region Properties
    public string Name { get; set; }
    public int Age { get; set; }
#endregion

#region Methods
    public void DoSomething()
    {
        // implementation
    }
#endregion
