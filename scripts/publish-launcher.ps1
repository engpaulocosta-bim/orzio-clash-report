#requires -Version 7.0
<#
.SYNOPSIS
    Publishes the launcher and the engine into the installer staging layout.

.DESCRIPTION
    Produces exactly the tree the installer ships:

        <staging>\OrzioClashReport.Launcher.Desktop.exe
        <staging>\engine\win-x64\orzioclash.exe
        <staging>\engine\win-x64\engine-manifest.json
        <staging>\samples\
        <staging>\docs\

    The engine is published with the same options the release workflow already uses, so the
    executable inside the installer is produced the same way as the one in a release ZIP.

    engine-manifest.json records the SHA-256 of the executable that was actually published, computed
    here from that file. There is no placeholder and no hard-coded digest: the launcher refuses to
    run an engine whose bytes do not match this value.
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $StagingDirectory = (Join-Path $PSScriptRoot '..' 'artifacts' 'launcher' 'staging')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$launcherProject = Join-Path $repositoryRoot 'src/OrzioClashReport.Launcher.Desktop/OrzioClashReport.Launcher.Desktop.csproj'
$engineProject = Join-Path $repositoryRoot 'src/OrzioClashReport.Cli/OrzioClashReport.Cli.csproj'

if (Test-Path -LiteralPath $StagingDirectory) {
    Remove-Item -LiteralPath $StagingDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $StagingDirectory | Out-Null
$engineDirectory = Join-Path $StagingDirectory 'engine/win-x64'
New-Item -ItemType Directory -Force -Path $engineDirectory | Out-Null

Write-Host 'Publishing the launcher (win-x64, self-contained)...'

# PublishTrimmed is deliberately off: trimming a UI application needs its own verification pass, and
# this phase is about getting a verified build into an evaluator's hands.
dotnet publish $launcherProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $StagingDirectory `
    -p:PublishTrimmed=false `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw 'Failed to publish the launcher.'
}

Write-Host 'Publishing the engine (win-x64, self-contained, single file)...'

# Exactly the options release.yml uses. The engine's publish model is not changed by the launcher.
dotnet publish $engineProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $engineDirectory `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw 'Failed to publish the engine.'
}

$launcherExe = Join-Path $StagingDirectory 'OrzioClashReport.Launcher.Desktop.exe'
if (-not (Test-Path -LiteralPath $launcherExe -PathType Leaf)) {
    throw "The launcher executable was not produced: $launcherExe"
}

$engineExe = Join-Path $engineDirectory 'orzioclash.exe'
if (-not (Test-Path -LiteralPath $engineExe -PathType Leaf)) {
    throw "The engine executable was not produced: $engineExe"
}

Write-Host 'Reading the published engine version...'

$versionLine = & $engineExe --version
if ($LASTEXITCODE -ne 0) {
    throw 'The published engine did not answer --version.'
}

if ($versionLine -notmatch '^orzioclash (?<version>[0-9]+\.[0-9]+\.[0-9]+-[A-Za-z0-9.]+)$') {
    throw "Unexpected engine version output: $versionLine"
}

$engineVersion = $Matches['version']

Write-Host "Computing SHA-256 of the published engine ($engineVersion)..."

$engineHash = (Get-FileHash -LiteralPath $engineExe -Algorithm SHA256).Hash.ToLowerInvariant()

$manifest = [ordered]@{
    schemaVersion = 1
    engineVersion = $engineVersion
    fileName      = 'orzioclash.exe'
    sha256        = $engineHash
}

$manifestPath = Join-Path $engineDirectory 'engine-manifest.json'
$manifestJson = $manifest | ConvertTo-Json -Depth 3
[System.IO.File]::WriteAllText($manifestPath, $manifestJson, (New-Object System.Text.UTF8Encoding($false)))

Write-Host 'Copying samples and documentation...'

$samplesDirectory = Join-Path $StagingDirectory 'samples'
New-Item -ItemType Directory -Force -Path $samplesDirectory | Out-Null

foreach ($sample in @(
    'samples/sample-clash.xml',
    'samples/sample-clash.run-manifest.json',
    'samples/run-manifest.sample.json',
    'samples/run-index.template.json')) {

    Copy-Item -LiteralPath (Join-Path $repositoryRoot $sample) -Destination $samplesDirectory
}

$docsDirectory = Join-Path $StagingDirectory 'docs'
New-Item -ItemType Directory -Force -Path $docsDirectory | Out-Null

foreach ($document in @(
    'README.md',
    'CHANGELOG.md',
    'docs/operations/internal-preview.md',
    'docs/operations/pilot-evaluation.md',
    'docs/operations/desktop-pilot.md')) {

    $source = Join-Path $repositoryRoot $document
    if (Test-Path -LiteralPath $source -PathType Leaf) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $docsDirectory (Split-Path -Leaf $document))
    }
}

Write-Host 'Removing debug symbols from the staging tree...'
Get-ChildItem -LiteralPath $StagingDirectory -Recurse -File -Filter '*.pdb' | Remove-Item -Force

Write-Host ''
Write-Host "Staging tree ready: $StagingDirectory"
Write-Host "Engine version:    $engineVersion"
Write-Host "Engine SHA-256:    $engineHash"
