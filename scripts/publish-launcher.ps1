#Requires -Version 5.1
<#
.SYNOPSIS
    Publishes the Orzio Clash Report Desktop launcher and the engine into one staging layout.

.DESCRIPTION
    Produces the exact directory tree the installer packages:

        <staging>\OrzioClashReport.Launcher.Desktop.exe
        <staging>\engine\win-x64\orzioclash.exe
        <staging>\engine\win-x64\engine-manifest.json
        <staging>\samples\
        <staging>\docs\

    The engine is published with the same self-contained single-file model the release workflow
    already uses. The engine manifest's SHA-256 is computed over the executable that was actually
    published; it is never written by hand.
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string] $StagingDirectory = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "artifacts/launcher/staging"),
    [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function New-CleanDirectory {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string] $Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$launcherProject = Join-Path $RepositoryRoot "src/OrzioClashReport.Launcher.Desktop/OrzioClashReport.Launcher.Desktop.csproj"
$engineProject = Join-Path $RepositoryRoot "src/OrzioClashReport.Cli/OrzioClashReport.Cli.csproj"

foreach ($project in @($launcherProject, $engineProject)) {
    if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
        throw "Project not found: $project"
    }
}

New-CleanDirectory -Path $StagingDirectory
$engineDirectory = Join-Path $StagingDirectory "engine/win-x64"
New-Item -ItemType Directory -Force -Path $engineDirectory | Out-Null

Write-Host "Publishing the launcher (win-x64, self-contained)..."
# PublishTrimmed is deliberately off at this stage: a trimmed Avalonia application needs its own
# validation pass, and the pilot is not the place to discover a trimming defect.
& dotnet publish $launcherProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $StagingDirectory `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:DebugType=None `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw "Publishing the launcher failed." }

Write-Host "Publishing the engine (win-x64, self-contained, single file)..."
& dotnet publish $engineProject `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $engineDirectory `
    -p:PublishSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw "Publishing the engine failed." }

$launcherExe = Join-Path $StagingDirectory "OrzioClashReport.Launcher.Desktop.exe"
$engineExe = Join-Path $engineDirectory "orzioclash.exe"

foreach ($exe in @($launcherExe, $engineExe)) {
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
        throw "Expected executable was not produced: $exe"
    }
    if ((Get-Item -LiteralPath $exe).Length -le 0) {
        throw "Published executable is empty: $exe"
    }
}

Write-Host "Reading the engine version from the published executable..."
$versionLine = & $engineExe --version
if ($LASTEXITCODE -ne 0) { throw "Failed to read the engine version." }
if ($versionLine -notmatch '^orzioclash (?<version>[0-9]+\.[0-9]+\.[0-9]+-[A-Za-z0-9.]+)$') {
    throw "Unexpected engine version output: $versionLine"
}
$engineVersion = $Matches["version"]

# The digest is computed over the engine that was actually published, right here.
$engineSha256 = Get-Sha256 -Path $engineExe

$manifest = [ordered]@{
    schemaVersion      = 1
    engineVersion      = $engineVersion
    executableFileName = "orzioclash.exe"
    sha256             = $engineSha256
}

$manifestPath = Join-Path $engineDirectory "engine-manifest.json"
$manifestJson = $manifest | ConvertTo-Json -Depth 3
[System.IO.File]::WriteAllText($manifestPath, $manifestJson, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "Copying samples and documentation..."
$samplesDirectory = Join-Path $StagingDirectory "samples"
$docsDirectory = Join-Path $StagingDirectory "docs"
New-Item -ItemType Directory -Force -Path $samplesDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $docsDirectory | Out-Null

foreach ($sample in @(
        "samples/sample-clash.xml",
        "samples/sample-clash.run-manifest.json",
        "samples/run-manifest.sample.json",
        "samples/run-index.template.json")) {
    Copy-Item -LiteralPath (Join-Path $RepositoryRoot $sample) -Destination $samplesDirectory
}

foreach ($doc in @(
        "README.md",
        "CHANGELOG.md",
        "docs/operations/launcher-pilot.md")) {
    Copy-Item -LiteralPath (Join-Path $RepositoryRoot $doc) -Destination $docsDirectory
}

Write-Host ""
Write-Host "Staging directory : $StagingDirectory"
Write-Host "Engine version    : $engineVersion"
Write-Host "Engine SHA-256    : $engineSha256"
