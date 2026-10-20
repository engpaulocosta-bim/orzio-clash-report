# Desktop automatic releases and user-initiated updates

## Channels

This repository has two independent release channels:

- Existing engine/CLI preview: tags such as v0.1.0-preview.3, produced by .github/workflows/release.yml.
- Windows desktop launcher: tags in the form desktop-v<launcher-version>, produced by .github/workflows/desktop-release.yml.

The desktop updater ignores engine/CLI releases. It searches for a newer desktop release via the public GitHub Releases API, including preview releases. It does not depend on the GitHub releases/latest redirect (which excludes prereleases).

## Publishing

Every push to master checks the desktop version. A new desktop release is produced only when the versioned release does not already exist, after successful restore/build/tests, publish and Inno Setup packaging. An existing tag without a release is treated as an error, not silently replaced. Workflow dispatch on master can retry a failed release, but cannot overwrite a published version.

On any desired desktop update, advance the launcher version consistently in:

1. src/OrzioClashReport.Launcher.Desktop/OrzioClashReport.Launcher.Desktop.csproj: VersionPrefix / VersionSuffix.
2. src/OrzioClashReport.Launcher.Application/LauncherBuildInfo.cs: LauncherVersion.
3. installer/windows/OrzioClashReportLauncher.iss: LauncherVersion (numeric) and LauncherFullVersion.

The release workflow rejects mismatches. For example, version 0.2.1-launcher-preview.1 yields tag desktop-v0.2.1-launcher-preview.1 and installer:

orzio-clash-report-desktop-v0.2.1-launcher-preview.1-win-x64-setup.exe

Do not reuse an existing version for changed published bytes. Bump the numeric version for a new installer upgrade. Changes in master without a version bump run CI but do not create additional releases.

When the workflow runs successfully, the release contains:

- The versioned Inno Setup installer executable.
- Its matching .exe.sha256 text file.
- orzio-clash-update-manifest.json with version, installerUrl, sha256 and releaseNotesUrl.

The workflow makes preview versions GitHub prereleases. All release assets stay in the same public repository; no additional GitHub App, PAT or cross-repository permission is required. The standard GITHUB_TOKEN must have contents:write permission, which the release job requests. Repository policy may still restrict it.

## How the application checks and installs

The user opens Settings and clicks "Procurar atualizações." Only then the application makes network requests to the official public GitHub Releases API. It selects the highest applicable desktop-v version, compares it against LauncherBuildInfo, and retrieves the published manifest from that same release.

Before offering the update, the application checks that the manifest version matches the release and that the installer URL points to the exact expected asset path in engpaulocosta-bim/orzio-clash-report. The manifest must contain a 64-digit hexadecimal SHA-256.

The user must explicitly choose "Instalar atualização" in a confirmation window. The application then downloads the installer to the user's local Orzio update directory, enforces a 250 MiB maximum, checks SHA-256 and opens the normal Inno Setup wizard. A hash mismatch prevents execution. The installer targets the existing application identity and preserves local user project files, reports and exports. Its normal close-applications prompt is enabled; finish any active processing before accepting the upgrade.

Internet connectivity is not required for any normal reporting operation. Update errors are shown in Settings. No files from the user's projects or reports are sent to GitHub.

## Acceptance checks

1. Open a pull request and wait for .github/workflows/ci.yml to pass, including Windows desktop package smoke.
2. Merge the change into master only after source review and tests. The new desktop release workflow should publish the tag derived from the launcher version.
3. Inspect the release: the installer, checksum and manifest must all exist and the checksum must match the installer bytes.
4. Install the previous desktop version and click "Procurar atualizações." Confirm that the newer version is offered and that declining does not download or launch anything.
5. Accept the update, confirm the installer is launched only after SHA-256 verification, and verify the installed launcher version and user data after upgrade.
6. Re-run the release workflow on master with the same version and confirm that it skips publication rather than replacing the published assets.

The desktop installer is currently unsigned. Windows SmartScreen may show a warning. SHA-256 verifies correspondence with the GitHub release, but is not a substitute for code signing. Publishing the first desktop update requires real Windows installer and UI validation.

## Compatibility with existing release controls

The existing release.yml, v-prefixed engine tags and internal CLI preview checklist remain unchanged. Their historical manual-tag requirements apply to the engine/CLI channel. Automated desktop-v tags are a separate, explicitly versioned mechanism and should never be confused with engine releases.
