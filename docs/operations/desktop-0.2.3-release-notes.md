# Orzio Clash Report Desktop 0.2.3-launcher-preview.1

Local candidate. Not published or installed by this change.

- Approved Orzio branding, navy navigation and teal actions in both themes.
- Operational overview, clearer PT/EN navigation and recent-file availability feedback.
- Consistent secondary forms, wrapping operation tabs and embedded Windows icons.
- Fixes a Windows file-sharing error that prevented a verified update download from being renamed before starting the installer.

Engine remains `0.1.0-preview.3`. XML, reports, snapshots, catalogs and governance formats are unchanged. Existing reports and launcher preferences remain in their original locations.

## First upgrade from 0.2.2

The installed 0.2.2 updater contains the file-sharing defect. Publishing a fixed version cannot repair the updater already running on the computer. After publication and QA approval, download the version-specific official installer and its SHA-256 from the desktop GitHub release, verify the checksum and run the installer manually in the same installation scope. Later checks can use the repaired updater. Do not promise an old-button end-to-end PASS.

The existing pipeline produces unsigned installers. SHA-256 verification and official version-specific URLs remain required. Windows may warn for a browser-downloaded installer; no SmartScreen or Authenticode protection is claimed for an installer created by the in-app download.

## Rollback

Retain the previous immutable installer and verify its published hash before use. Close the launcher and reinstall in the original scope, without selecting deletion of local state. This change has no data migration. Installed downgrade and machine-wide/per-user coexistence still require isolated Windows acceptance; fixture recovery tests do not replace that check.
