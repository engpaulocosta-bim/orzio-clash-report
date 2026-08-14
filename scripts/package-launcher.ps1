#requires -Version 7.0
<#
.SYNOPSIS
    Validates the staging tree, builds the Windows installer, and publishes its SHA-256.

.DESCRIPTION
    Refuses to package a staging tree that contains debug symbols, temporary files, Navisworks or
    Revit models, or images. Those are either build leftovers or a client's own data, and neither
    belongs in something that gets sent to an evaluator.

    The installer produced here is NOT code signed. That is stated in the output, in the release
    notes and in the pilot documentation, because pretending otherwise would be worse than the
    SmartScreen warning it causes.
#>
[CmdletBinding()]
param(
    [string] $StagingDirectory = (Join-Path $PSScriptRoot '..' 'artifacts' 'launcher' 'staging'),
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..' 'artifacts' 'launcher' 'installer'),
    [string] $InnoSetupCompiler = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$scriptPath = Join-Path $repositoryRoot 'installer/windows/OrzioClashReportLauncher.iss'

if (-not (Test-Path -LiteralPath $StagingDirectory -PathType Container)) {
    throw "The staging tree does not exist: $StagingDirectory. Run publish-launcher.ps1 first."
}

Write-Host 'Validating the staging tree...'

$requiredFiles = @(
    'OrzioClashReport.Launcher.Desktop.exe',
    'engine/win-x64/orzioclash.exe',
    'engine/win-x64/engine-manifest.json'
)

foreach ($required in $requiredFiles) {
    $path = Join-Path $StagingDirectory $required
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "The staging tree is missing a required file: $required"
    }
}

$manifest = Get-Content -LiteralPath (Join-Path $StagingDirectory 'engine/win-x64/engine-manifest.json') -Raw | ConvertFrom-Json

if ($manifest.schemaVersion -ne 1) {
    throw "Unsupported engine manifest schema version: $($manifest.schemaVersion)"
}

if ($manifest.sha256 -notmatch '^[0-9a-f]{64}$') {
    throw 'The engine manifest does not carry a valid lower-case SHA-256.'
}

$actualEngineHash = (Get-FileHash `
    -LiteralPath (Join-Path $StagingDirectory 'engine/win-x64/orzioclash.exe') `
    -Algorithm SHA256).Hash.ToLowerInvariant()

if ($actualEngineHash -ne $manifest.sha256) {
    throw "The engine manifest hash does not match the staged executable. Re-run publish-launcher.ps1."
}

$forbiddenPattern = '\.(pdb|tmp|nwd|nwf|nwc|rvt|png|jpg|jpeg|gif)$'

$forbidden = @(Get-ChildItem -LiteralPath $StagingDirectory -Recurse -File |
    Where-Object { $_.Name -match $forbiddenPattern } |
    ForEach-Object { $_.FullName.Substring($StagingDirectory.Length + 1).Replace('\', '/') })

if ($forbidden.Count -ne 0) {
    throw "Forbidden files were found in the staging tree: $($forbidden -join ', ')"
}

Write-Host "Staging tree validated. Engine SHA-256: $actualEngineHash"

if (-not (Test-Path -LiteralPath $InnoSetupCompiler -PathType Leaf)) {
    throw "Inno Setup compiler was not found at '$InnoSetupCompiler'. Install Inno Setup 6 or pass -InnoSetupCompiler."
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

Write-Host 'Building the installer...'

& $InnoSetupCompiler `
    "/DStagingDir=$((Resolve-Path $StagingDirectory).Path)" `
    "/DOutputDir=$((Resolve-Path $OutputDirectory).Path)" `
    $scriptPath

if ($LASTEXITCODE -ne 0) {
    throw 'Inno Setup failed to build the installer.'
}

$installer = Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.exe' |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $installer) {
    throw "No installer was produced in $OutputDirectory."
}

$installerHash = (Get-FileHash -LiteralPath $installer.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = "$($installer.FullName).sha256"

Set-Content -LiteralPath $checksumPath -Value "$installerHash  $($installer.Name)" -NoNewline -Encoding ascii

Write-Host ''
Write-Host "Installer:        $($installer.FullName)"
Write-Host "Installer SHA-256: $installerHash"
Write-Host ''
Write-Host 'This installer is NOT code signed. Windows SmartScreen will warn on first run.'
Write-Host 'Verify the download with the SHA-256 above before installing.'
