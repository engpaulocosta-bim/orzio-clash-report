# Orzio design update implementation plan

**Goal:** Implement the supplied Orzio design package sequentially, preserving engine, file formats, product identity and the installed update channel.

**Architecture:** Reuse Avalonia resource dictionaries, existing navigation commands, localized strings and recent-output storage. Embed approved assets; retain the existing verified-download updater and Inno Setup installer.

**Tech stack:** .NET 8, Avalonia 11.3.20, xUnit, PowerShell, Inno Setup.

**Spec:** User-supplied Orzio_Clash_Report_Design_Update_Package.zip, tasks T00–T18.

## Constraints and decisions

- Work on `feat/orzio-design-update`, based on `7fe5c49`.
- User explicitly requested continuous execution; use the supplied design without another design approval cycle.
- No engine, schema, report template, product ID or feed changes.
- Use Segoe UI/system fallback; no new font dependency or telemetry.
- No fabricated project, clash or revision metrics: current launcher has no active-project summary source.
- Remote mutations require explicit authorization under user AGENTS.md; prepare the candidate before requesting publication approval.
- Record actual outcomes and outstanding gates; local compilation cannot prove a published upgrade.

## Ordered tasks

- [x] T00: Inventory entrypoints, update path, versions, installer and CI; record evidence.
- [x] T01: Baseline full solution tests and anonymized fixtures; record counts and limitations.
- [x] T02: Embed exact approved original, wordmark, symbol and ICO in Desktop/Assets; verify hashes and build.
- [x] T03: Update both token palettes, spacing and native font; run style and theme tests.
- [x] T04: Replace OC placeholder with approved symbol, navy sidebar, readable active/hover/focus states; preserve navigation.
- [x] T05: Replace promotional hero with operational overview and existing quick-report/compare commands.
- [x] T06: Add honest history loading/error states using existing store; retain real engine probe.
- [x] T07: Handle long recent names, missing files and open failures; test retained history and error feedback.
- [x] T08: Localize overview and conflict-evolution labels in PT/EN; preserve IDs.
- [x] T09: Apply shared tokens to secondary views; verify all sections and both themes.
- [x] T10: Keep footer update action and real launcher version; remove duplicate engine badge.
- [x] T11: Embed executable/window icon and configure installer icon without changing AppId or data paths.
- [x] T12: Full regression suite and CLI fixture smoke; preserve byte-identical engine outputs.
- [x] T13: Record old-version manifest, URL, version and installation compatibility.
- [x] T14: Fix only demonstrated updater defects; independent review of installer/update boundaries.
- [ ] T15: Raise launcher only to 0.2.3-launcher-preview.1; build candidate, installer and SHA256 if toolchain permits.
- [ ] T16: Native visual/keyboard QA, full tests and fresh independent review; document gate results.
- [ ] T17: Publish and prove old-installation update only after authorization and all gates pass.
- [ ] T18: Verify failure recovery, record rollback and final evidence without claiming unperformed release tests.

## Verification and rollback

Run targeted tests after relevant edits, then Release solution tests and packaging smoke. Review missing assets, long names, missing outputs, cancelled/failed history load, both themes and update failures. Rollback is reinstalling the previous immutable installer with unchanged AppId and data paths; never remove user state. Installer and published-feed acceptance require actual evidence.

## Execution boundary

T00–T14 implementation/checks are recorded; checkmarks do not assert native installed acceptance. T04/T09/T11 still need native QA. T15 is partial (local ZIP exists; installer compiler blocked). T16–T18 cannot close until native, installed, publication-authorization and recovery gates are satisfied. Final source f52ecd5; 1806 Release tests and packaged CLI smoke passed. Overall BLOCKED, not complete.
