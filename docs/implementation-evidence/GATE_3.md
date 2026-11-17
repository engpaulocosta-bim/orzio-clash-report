# Gate 3 — Installed Windows resources

Status: PASS on the local Windows test machine.

Source 4d00071. Inno Setup 6.7.3 was manually installed by the user; setup build and actual per-user upgrade succeeded. ApplicationIcon, Window.Icon and SetupIconFile use the approved ICO. Built and installed executable icons and actual uninstaller icon were extracted and visually inspected. New logo observed in native titlebar, Start recommended tile and Alt+Tab. Start shortcut targets the installed executable; Windows uninstall entry names 0.2.3 and uses that executable for DisplayIcon. AppId/scope/data paths unchanged.

The user supplied two screenshots confirming the running Orzio taskbar icon and installed interface. The taskbar shows the approved blue/teal symbol on its light tile. This closes the separate taskbar visual check. These screenshots contain private project names and are not copied into Git or release artifacts. No system icon cache was cleared.
