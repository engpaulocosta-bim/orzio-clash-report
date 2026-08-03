#Requires -Version 5.1
<#
.SYNOPSIS
    Validates the launcher staging tree, compiles the installer, and writes its SHA-256.

.DESCRIPTION
    Refuses to package anything that does not belong in a distributed artifact: debug symbols,
    temporary files, Navisworks or Revit models, and unexpected images. Then compiles the Inno
    Setup script and writes a checksum file beside the installer.

    This installer is not code signed. That is stated here, in the release notes, and in the pilot
    guide, rather than implied away.
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot,
    [string] $StagingDirectory,
    [string] $OutputDirectory,
    [string] $LauncherVersion = "0.2.0-launcher-preview.1",
    [string] $InnoSetupCompiler
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# $PSScriptRoot is not populated while a param() block's defaults are evaluated under Windows
# PowerShell 5.1 invoked with -File, so the defaults are resolved here instead of there.
if (-not $RepositoryRoot) {
    $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
    $RepositoryRoot = (Resolve-Path -LiteralPath (Join-Path $scriptDirectory "..")).Path
}

$RepositoryRoot = $RepositoryRoot.TrimEnd('\', '/')

if (-not $StagingDirectory) {
    $StagingDirectory = Join-Path $RepositoryRoot "artifacts\launcher\staging"
}

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot "artifacts\launcher"
}

if (-not (Test-Path -LiteralPath $StagingDirectory -PathType Container)) {
    throw "Staging directory not found: $StagingDirectory. Run scripts/publish-launcher.ps1 first."
}

# A repository under a path with a space is ordinary; the installer compiler is handed the resolved
# native path so nothing downstream has to guess where the value ends.
$StagingDirectory = (Resolve-Path -LiteralPath $StagingDirectory).Path.TrimEnd('\', '/')

Write-Host "Validating the staged layout..."

$requiredFiles = @(
    "OrzioClashReport.Launcher.Desktop.exe",
    "engine/win-x64/orzioclash.exe",
    "engine/win-x64/engine-manifest.json",
    "samples/sample-clash.xml",
    "samples/sample-clash.run-manifest.json",
    "samples/run-manifest.sample.json",
    "samples/run-index.template.json",
    "docs/README.md",
    "docs/CHANGELOG.md",
    "docs/launcher-pilot.md"
)

foreach ($required in $requiredFiles) {
    $path = Join-Path $StagingDirectory $required
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file is missing from the staged layout: $required"
    }
}

$staged = @(Get-ChildItem -LiteralPath $StagingDirectory -Recurse -File | ForEach-Object {
        $_.FullName.Substring($StagingDirectory.Length + 1).Replace('\', '/')
    })

$forbidden = @($staged | Where-Object {
        $_ -match '\.pdb$' -or
        $_ -match '\.tmp$' -or
        $_ -match '\.(nwd|nwf|nwc|rvt)$' -or
        $_ -match '\.(png|jpg|jpeg|gif)$' -or
        $_ -match '(^|/)\.(derived-html-report|identity-governance-replace|run-index-replace)-'
    })

if ($forbidden.Count -ne 0) {
    throw "Files that must never be distributed were found in the staged layout: $($forbidden -join ', ')"
}

Write-Host "Verifying the engine manifest against the staged engine..."
$manifestPath = Join-Path $StagingDirectory "engine/win-x64/engine-manifest.json"
$enginePath = Join-Path $StagingDirectory "engine/win-x64/orzioclash.exe"
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

if ($manifest.schemaVersion -ne 1) {
    throw "Unexpected engine manifest schema version: $($manifest.schemaVersion)"
}

$actualSha256 = (Get-FileHash -LiteralPath $enginePath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($manifest.sha256 -ne $actualSha256) {
    throw "The engine manifest digest does not match the staged engine. Re-run scripts/publish-launcher.ps1."
}

if ($manifest.executableFileName -ne "orzioclash.exe") {
    throw "Unexpected engine executable name in the manifest: $($manifest.executableFileName)"
}

Write-Host "Engine manifest verified: $($manifest.engineVersion) / $actualSha256"

if (-not $InnoSetupCompiler) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) }

    if ($candidates.Count -eq 0) {
        throw "Inno Setup 6.3 or later was not found. Install it from https://jrsoftware.org/isdl.php, or pass -InnoSetupCompiler with the path to ISCC.exe."
    }

    $InnoSetupCompiler = $candidates[0]
}

# ArchitecturesAllowed=x64compatible was introduced in 6.3; an older compiler fails with an
# unhelpful message about an unknown value, so it is rejected here with the reason.
$compilerVersion = (Get-Item -LiteralPath $InnoSetupCompiler).VersionInfo.FileVersion
if ($compilerVersion -match '^(?<major>\d+)\.(?<minor>\d+)') {
    $major = [int] $Matches["major"]
    $minor = [int] $Matches["minor"]
    if ($major -lt 6 -or ($major -eq 6 -and $minor -lt 3)) {
        throw "Inno Setup $compilerVersion is too old; the installer script needs 6.3 or later."
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path.TrimEnd('\', '/')

$script = Join-Path $RepositoryRoot "installer\windows\OrzioClashReportLauncher.iss"
Write-Host "Compiling the installer..."
# Each define is one argument. A trailing separator was trimmed above so that a quoted path with a
# space survives being turned back into a command line.
& $InnoSetupCompiler `
    "/DLauncherVersion=$LauncherVersion" `
    "/DStagingDirectory=$StagingDirectory" `
    "/DOutputDirectory=$OutputDirectory" `
    $script
if ($LASTEXITCODE -ne 0) { throw "Compiling the installer failed." }

$installer = Join-Path $OutputDirectory "OrzioClashReportSetup-$LauncherVersion.exe"
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
    throw "The installer was not produced: $installer"
}

$installerSha256 = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = "$installer.sha256"
Set-Content -LiteralPath $checksumPath `
    -Value "$installerSha256  $(Split-Path -Leaf $installer)" -NoNewline -Encoding ascii

Write-Host ""
Write-Host "Installer   : $installer"
Write-Host "SHA-256     : $installerSha256"
Write-Host "Checksum    : $checksumPath"
Write-Host ""
Write-Host "This installer is NOT code signed. Windows SmartScreen will warn on first run."
Write-Host "Verify the SHA-256 above before installing."
