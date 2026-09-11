# Dedicated board only

Objective: /board starts with the extension/endpoint mosaic, with no global header,
navigation, notices, legend, retention or always-visible filters above it. A compact
filter button beside the extension count opens an overlay; closing it preserves
filter state and URL. The normal page retains its existing controls.

Constraints: English source and bilingual SUI UI; preserve access checks, data,
customer filters and state warnings (compact indicator + menu details). No PBX,
AMI, Identity, nginx or checkout changes. Only the Eveo panel may be deployed.
Existing unrelated edits are preserved. Popover and fullscreen must compose.

1. CURRENT: Separate the dedicated render branch and add the compact filter menu.
2. Pending: Validate top-of-page geometry, filters/reload/share, keyboard, mobile,
   language/fullscreen, normal-page regressions and build.
3. Pending: Publish only the panel to Eveo and record verified delivery.
