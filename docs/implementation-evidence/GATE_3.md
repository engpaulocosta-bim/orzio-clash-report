# Gate 3 — Windows resources

Status: BLOCKED.

Executor: Codex; independent static path review: Claude CLI. Source `f52ecd5`.

ApplicationIcon, Window.Icon and Inno SetupIconFile point at the supplied ICO. The built win-x64 executable carries the branded icon: extracted and visually inspected. ProductVersion agrees with version props. AppId, shortcuts, uninstall icon source and data paths remain unchanged.

Inno Setup 6 is absent and automatic approval review rejected its installation (“blocked by policy”). No candidate setup exists. Installed Start/taskbar/Alt+Tab/uninstall icons and cache refresh are NOT_RUN. This gate cannot pass from executable resource checks alone.
