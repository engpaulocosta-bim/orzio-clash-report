# Response to independent design review

Reviewer: independent Claude CLI, read-only; original report in `design-initial.md`.

1. Confirmed history data-loss mechanism. Corrupt/unreadable history now surfaces errors; `AddAsync` cannot replace it. Unsupported schema/invalid items are rejected before write. This deliberately corrects the old convenience-store error contract to meet T06/T07 data preservation; persistent JSON schema remains version 1. The operation executor logs a warning and retains successful engine results when recording convenience history fails. Real-file and real-process tests cover preservation, error presentation, artifact success and completed journal state.
2. Confirmed `Win32Exception` escaped update error handling. Added the exception to the existing expected-failure filter, plus an install-start failure test proving the application stays open and reports Failed. Recent-file open handling covers the same platform exception.
3. Tightened light accent and tertiary tokens; new resource-driven contrast tests measure the relevant text/action/sidebar pairs against AA 4.5:1 in both themes. No dependency added.
4. Sidebar refresh icon concern was not reproduced: shared `Button PathIcon` inherits its button Foreground, and sidebar-action sets the dedicated muted sidebar brush. Rendered light/dark previews show the light icon. No redundant override added.
5. Unsigned distribution and same-user file replacement risk predate this change; no publisher-authenticity claim is made. Release notes distinguish browser warnings from in-app verification.
6. Avalonia focus-visible rules compile/load; real Windows keyboard and DPI acceptance remain pending. Headless navigation/layout coverage is not substituted for native proof.
7. Missing files retain an enabled Open action so restored files can be retried; failure is labelled and reported. Disabling it would strand restored files until a refresh. Overview refreshes when selected again.
8. Excluded original artwork and provenance README from runtime resources; preserved original in source. PNG/ICO derivatives are embedded.
9–11. Fixed indentation and status-bar copy; render fixture uses the actual launcher build version. Historical test version values remain test inputs.
12. No unverified Windows version floor or installation-scope policy changes. Existing machine-wide/per-user coexistence and unsigned-installer limits remain native release gates.

Initial red tests: corrupt history and installer start failures both failed (exit 1). After fixes, targeted history/engine/update tests passed. A cancellation fixture initially returned HTTP 503 without observing its token; corrected the fake to honor cancellation. A live candidate process also locked Release outputs; the process launched for QA was stopped before final checks. Final suite results are in T16.

Follow-up audit caught missing InvalidDataException handling for structurally invalid history (it is not an IOException). Reproduced through both Home and real-process executor with unsupported schema and invalid items: four failures, exit 1. Added the explicit exception to both handlers and expanded real-file tests. This also covers startup/return-to-overview paths; no catch-all was introduced.
