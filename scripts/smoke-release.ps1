[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ExecutablePath,

    [Parameter(Mandatory = $true)]
    [string] $SmokeWorkspace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RequiredFile {
    param([string] $Path, [string] $Description)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Description path is required."
    }

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description path does not exist or is not a file."
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Resolve-RequiredDirectory {
    param([string] $Path, [string] $Description)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Description path is required."
    }

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "$Description path does not exist or is not a directory."
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Get-ReleaseRoot {
    $scriptDirectory = Split-Path -Parent $PSCommandPath
    if (Test-Path -LiteralPath (Join-Path $scriptDirectory "samples\sample-clash.xml") -PathType Leaf) {
        return $scriptDirectory
    }

    $parentDirectory = Split-Path -Parent $scriptDirectory
    if (Test-Path -LiteralPath (Join-Path $parentDirectory "samples\sample-clash.xml") -PathType Leaf) {
        return $parentDirectory
    }

    throw "Could not locate anonymized sample files next to the smoke script."
}

function Assert-SafeWorkspace {
    param([string] $WorkspacePath, [string] $ReleaseRoot)

    $workspaceFullPath = [System.IO.Path]::GetFullPath($WorkspacePath).TrimEnd('\')
    $releaseRootFullPath = [System.IO.Path]::GetFullPath($ReleaseRoot).TrimEnd('\')
    $driveRoot = [System.IO.Path]::GetPathRoot($workspaceFullPath).TrimEnd('\')
    $homePath = [System.IO.Path]::GetFullPath([Environment]::GetFolderPath("UserProfile")).TrimEnd('\')

    if ($workspaceFullPath.Equals($driveRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Smoke workspace must not be a drive root."
    }

    if ($workspaceFullPath.Equals($homePath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Smoke workspace must not be the user profile directory."
    }

    if ($workspaceFullPath.Equals($releaseRootFullPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Smoke workspace must not be the release or repository root."
    }
}

function Invoke-Orzio {
    param(
        [string] $Executable,
        [string[]] $Arguments,
        [string] $Name,
        [string] $OutputDirectory
    )

    $stdoutPath = Join-Path $OutputDirectory "$Name.stdout.txt"
    $stderrPath = Join-Path $OutputDirectory "$Name.stderr.txt"

    & $Executable @Arguments 1> $stdoutPath 2> $stderrPath
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        throw "Command '$Name' failed with exit code $exitCode."
    }

    if ((Get-Item -LiteralPath $stderrPath).Length -ne 0) {
        throw "Command '$Name' wrote to stderr."
    }

    return @{
        StdOutPath = $stdoutPath
        StdErrPath = $stderrPath
    }
}

function Assert-NonEmptyFile {
    param([string] $Path, [string] $Description)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description was not created."
    }

    if ((Get-Item -LiteralPath $Path).Length -le 0) {
        throw "$Description is empty."
    }
}

$resolvedExecutable = Resolve-RequiredFile -Path $ExecutablePath -Description "Executable"
$resolvedWorkspace = Resolve-RequiredDirectory -Path $SmokeWorkspace -Description "Smoke workspace"
$releaseRoot = Get-ReleaseRoot
Assert-SafeWorkspace -WorkspacePath $resolvedWorkspace -ReleaseRoot $releaseRoot

$sampleXml = Resolve-RequiredFile -Path (Join-Path $releaseRoot "samples\sample-clash.xml") -Description "Sample XML"
$sampleManifest = Resolve-RequiredFile -Path (Join-Path $releaseRoot "samples\sample-clash.run-manifest.json") -Description "Sample manifest"

$runRoot = Join-Path $resolvedWorkspace ("orzio-smoke-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $runRoot | Out-Null

$reportPath = Join-Path $runRoot "single-run-report.html"
$snapshotDirectory = Join-Path $runRoot "snapshots"
New-Item -ItemType Directory -Path $snapshotDirectory | Out-Null
$snapshot1Path = Join-Path $snapshotDirectory "run-001.json"
$snapshot2Path = Join-Path $snapshotDirectory "run-002.json"
$snapshot3Path = Join-Path $snapshotDirectory "run-003.json"
$indexPath = Join-Path $runRoot "run-index.json"
$longitudinalPath = Join-Path $runRoot "longitudinal-report.html"

Invoke-Orzio -Executable $resolvedExecutable -Arguments @("--version") -Name "version" -OutputDirectory $runRoot | Out-Null
Invoke-Orzio -Executable $resolvedExecutable -Arguments @("--help") -Name "help" -OutputDirectory $runRoot | Out-Null
Invoke-Orzio -Executable $resolvedExecutable -Arguments @($sampleXml, "-o", $reportPath) -Name "single-run-report" -OutputDirectory $runRoot | Out-Null
Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $sampleManifest, "-o", $snapshot1Path) -Name "snapshot-1" -OutputDirectory $runRoot | Out-Null
Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $sampleManifest, "-o", $snapshot2Path) -Name "snapshot-2" -OutputDirectory $runRoot | Out-Null
Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $sampleManifest, "-o", $snapshot3Path) -Name "snapshot-3" -OutputDirectory $runRoot | Out-Null
Invoke-Orzio -Executable $resolvedExecutable -Arguments @("index-snapshots", "--snapshot", $snapshot1Path, "--snapshot", $snapshot2Path, "--snapshot", $snapshot3Path, "-o", $indexPath) -Name "index-snapshots" -OutputDirectory $runRoot | Out-Null
Invoke-Orzio -Executable $resolvedExecutable -Arguments @("compare-index", "--index", $indexPath, "-o", $longitudinalPath) -Name "compare-index" -OutputDirectory $runRoot | Out-Null

Assert-NonEmptyFile -Path $reportPath -Description "Single-run HTML report"
Assert-NonEmptyFile -Path $snapshot1Path -Description "First snapshot"
Assert-NonEmptyFile -Path $snapshot2Path -Description "Second snapshot"
Assert-NonEmptyFile -Path $snapshot3Path -Description "Third snapshot"
Assert-NonEmptyFile -Path $indexPath -Description "Run index"
Assert-NonEmptyFile -Path $longitudinalPath -Description "Longitudinal HTML report"

Write-Output "Release smoke passed. This packaging smoke uses repeated anonymized fixtures and is not real sequential validation."
