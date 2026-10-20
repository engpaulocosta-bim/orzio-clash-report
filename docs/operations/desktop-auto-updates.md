# Desktop automatic releases and user-initiated updates

## Channels

This repository has two independent release channels:

- Existing engine/CLI preview: tags such as v0.1.0-preview.3, produced by .github/workflows/release.yml.
- Windows desktop launcher: tags in the form desktop-v<launcher-version>, produced by .github/workflows/desktop-release.yml.

The desktop updater ignores engine/CLI releases. It searches for a newer desktop release via the public GitHub Releases API, including preview releases. It does not depend on the GitHub releases/latest redirect (which excludes prereleases).

## Publishing

Every push to master runs a fast plan job that reads the desktop version. A new desktop release is produced only when that versioned release does not already exist, after successful restore/build/tests, publish and Inno Setup packaging on Windows. An existing tag without a release is treated as an error, not silently replaced. Workflow dispatch on master can retry a failed release, but cannot overwrite a published version.

The desktop version has a single source, src/LauncherVersion.props:

```xml
<LauncherVersionPrefix>0.2.1</LauncherVersionPrefix>
<LauncherVersionSuffix>launcher-preview.1</LauncherVersionSuffix>
```

The desktop and launcher-application projects import it, LauncherBuildInfo reads it from the assembly, scripts/package-launcher.ps1 passes it to Inno Setup (/DLauncherVersion and /DLauncherFullVersion), and the workflow reads it through scripts/get-launcher-version.ps1. No other file carries the version; DesktopVersionContractTests fails if one does.

To ship a desktop update: raise LauncherVersionSuffix (for example launcher-preview.1 to launcher-preview.2) or LauncherVersionPrefix in that file, in the same pull request as the change, and merge it to master. Version 0.2.1-launcher-preview.1 yields tag desktop-v0.2.1-launcher-preview.1 and installer:

orzio-clash-report-desktop-v0.2.1-launcher-preview.1-win-x64-setup.exe

On every pull request the plan job writes a summary with the tag a merge would publish. When a pull request changes shipped files (src/, installer/, scripts/) but the version is already published, it emits a warning: merging it will not create a release until the version is raised.

Do not reuse an existing version for changed published bytes. Changes in master without a version bump run CI but do not create additional releases.

When the workflow runs successfully, the release contains:

- The versioned Inno Setup installer executable.
- Its matching .exe.sha256 text file.
- orzio-clash-update-manifest.json with version, installerUrl, sha256 and releaseNotesUrl.

The workflow makes preview versions GitHub prereleases. All release assets stay in the same public repository; no additional GitHub App, PAT or cross-repository permission is required. The standard GITHUB_TOKEN must have contents:write permission, which the release job requests. Repository policy may still restrict it.

## How the application checks and installs

The user clicks "Procurar atualizações", available in the sidebar footer and in Definições. Only then the application makes network requests to the official public GitHub Releases API. It selects the highest applicable desktop-v version, compares it against LauncherBuildInfo.LauncherVersion, and retrieves the published manifest from that same release. When a newer version exists, the sidebar and the home screen show it; nothing is downloaded yet.

Before offering the update, the application checks that the manifest version matches the release and that the installer URL points to the exact expected asset path in engpaulocosta-bim/orzio-clash-report. The manifest must contain a 64-digit hexadecimal SHA-256.

The user must explicitly choose "Instalar atualização" in Definições, which also links to the release notes. The button is disabled while an engine job is running. The application then downloads the installer to the user's local Orzio update directory with visible progress, enforces a 250 MiB maximum, checks SHA-256 and starts the normal Inno Setup wizard directly (no shell intermediary). A hash mismatch prevents execution. Once the verified installer has started, the application closes itself so the installer can replace its files. The installer targets the existing application identity and preserves local user project files, reports and exports.

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
