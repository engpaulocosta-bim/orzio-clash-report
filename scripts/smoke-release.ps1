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

    return (Get-Item -LiteralPath $Path).FullName
}

function Resolve-RequiredDirectory {
    param([string] $Path, [string] $Description)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Description path is required."
    }

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "$Description path does not exist or is not a directory."
    }

    return (Get-Item -LiteralPath $Path).FullName
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

function Get-NormalizedFileText {
    param([string] $Path)

    return ((Get-Content -LiteralPath $Path -Raw) -replace "`r`n", "`n")
}

function Assert-TextContains {
    param([string] $Text, [string] $Expected, [string] $Description)

    if ($Text.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        throw "$Description did not contain expected text: $Expected"
    }
}

function Assert-TextEquals {
    param([string] $Actual, [string] $Expected, [string] $Description)

    if (-not [string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) {
        throw "$Description did not match the expected text exactly."
    }
}

function Get-FileSha256 {
    param([string] $Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Assert-EqualHash {
    param([string] $Actual, [string] $Expected, [string] $Description)

    if (-not [string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) {
        throw "$Description hash changed unexpectedly."
    }
}

function Assert-DifferentHash {
    param([string] $Actual, [string] $Expected, [string] $Description)

    if ([string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) {
        throw "$Description hash did not change."
    }
}

function Assert-RelativeReference {
    param([string] $Reference, [string] $Description)

    if ([string]::IsNullOrWhiteSpace($Reference)) {
        throw "$Description must not be empty."
    }

    if ([System.IO.Path]::IsPathRooted($Reference)) {
        throw "$Description must be relative."
    }

    if ($Reference.IndexOf('\', [StringComparison]::Ordinal) -ge 0) {
        throw "$Description must use '/' separators only."
    }
}

function Assert-ByteArrayEquals {
    param([byte[]] $Actual, [byte[]] $Expected, [string] $Description)

    if ($Actual.Length -ne $Expected.Length) {
        throw "$Description lengths differ."
    }

    for ($i = 0; $i -lt $Actual.Length; $i++) {
        if ($Actual[$i] -ne $Expected[$i]) {
            throw "$Description bytes differ."
        }
    }
}

function Assert-NoReplacementTempFiles {
    param([string] $RunIndexPath)

    $directory = Split-Path -Parent $RunIndexPath
    $tempFiles = @(Get-ChildItem -LiteralPath $directory -Filter ".run-index-replace-*.tmp" -File)
    if ($tempFiles.Count -ne 0) {
        throw "Run-index replacement temp files were left behind."
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

$reportsDirectory = Join-Path $runRoot "reports"
$snapshotDirectory = Join-Path $runRoot "snapshots"
New-Item -ItemType Directory -Path $reportsDirectory | Out-Null
New-Item -ItemType Directory -Path $snapshotDirectory | Out-Null

$singleRunReportPath = Join-Path $reportsDirectory "single-run-report.html"
$snapshot1Path = Join-Path $snapshotDirectory "run-001.json"
$snapshot2Path = Join-Path $snapshotDirectory "run-002.json"
$snapshot3Path = Join-Path $snapshotDirectory "run-003.json"
$snapshot4Path = Join-Path $snapshotDirectory "run-004.json"
$indexPath = Join-Path $runRoot "run-index.json"
$longitudinalPath = Join-Path $reportsDirectory "longitudinal-report.html"
$projectPath = Join-Path $runRoot "project.json"
$projectReportPath = Join-Path $reportsDirectory "project-longitudinal.html"

$versionResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("--version") -Name "version" -OutputDirectory $runRoot
$helpResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("--help") -Name "help" -OutputDirectory $runRoot
$singleRunResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @($sampleXml, "-o", $singleRunReportPath) -Name "single-run-report" -OutputDirectory $runRoot
$snapshot1Result = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $sampleManifest, "-o", $snapshot1Path) -Name "snapshot-1" -OutputDirectory $runRoot
$snapshot2Result = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $sampleManifest, "-o", $snapshot2Path) -Name "snapshot-2" -OutputDirectory $runRoot
$snapshot3Result = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $sampleManifest, "-o", $snapshot3Path) -Name "snapshot-3" -OutputDirectory $runRoot
$indexResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("index-snapshots", "--snapshot", $snapshot1Path, "--snapshot", $snapshot2Path, "--snapshot", $snapshot3Path, "-o", $indexPath) -Name "index-snapshots" -OutputDirectory $runRoot
$compareIndexInitialResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("compare-index", "--index", $indexPath, "-o", $longitudinalPath) -Name "compare-index-initial" -OutputDirectory $runRoot

Assert-TextEquals (Get-NormalizedFileText -Path $versionResult.StdOutPath) "orzioclash 0.1.0-preview.2`n" "Version output"
Assert-TextContains (Get-NormalizedFileText -Path $helpResult.StdOutPath) "append-project-snapshot" "Help output"
Assert-TextContains (Get-NormalizedFileText -Path $singleRunResult.StdOutPath) "raw clashes ->" "Single-run stdout"
Assert-TextContains (Get-NormalizedFileText -Path $indexResult.StdOutPath) "Indexed snapshots: 3" "Run-index stdout"
Assert-TextContains (Get-NormalizedFileText -Path $compareIndexInitialResult.StdOutPath) "Indexed runs: 3" "Initial compare-index stdout"

Assert-NonEmptyFile -Path $singleRunReportPath -Description "Single-run HTML report"
Assert-NonEmptyFile -Path $snapshot1Path -Description "First snapshot"
Assert-NonEmptyFile -Path $snapshot2Path -Description "Second snapshot"
Assert-NonEmptyFile -Path $snapshot3Path -Description "Third snapshot"
Assert-NonEmptyFile -Path $indexPath -Description "Run index"
Assert-NonEmptyFile -Path $longitudinalPath -Description "Longitudinal HTML report"

$createProjectResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("create-project", "--project-id", "smoke-project", "--name", "Smoke Project", "--index", $indexPath, "--report", $projectReportPath, "-o", $projectPath) -Name "create-project" -OutputDirectory $runRoot
Assert-NonEmptyFile -Path $projectPath -Description "Project catalog"

$projectJson = Get-Content -LiteralPath $projectPath -Raw | ConvertFrom-Json
if ($projectJson.schemaVersion -ne 1) {
    throw "Project catalog schemaVersion must be 1."
}

Assert-TextEquals -Actual ([string] $projectJson.projectId) -Expected "smoke-project" -Description "Project catalog projectId"
Assert-TextEquals -Actual ([string] $projectJson.displayName) -Expected "Smoke Project" -Description "Project catalog displayName"
Assert-TextEquals -Actual ([string] $projectJson.runIndexPath) -Expected "run-index.json" -Description "Project catalog runIndexPath"
Assert-TextEquals -Actual ([string] $projectJson.longitudinalReportPath) -Expected "reports/project-longitudinal.html" -Description "Project catalog longitudinalReportPath"
Assert-RelativeReference -Reference $projectJson.runIndexPath -Description "Project catalog runIndexPath"
Assert-RelativeReference -Reference $projectJson.longitudinalReportPath -Description "Project catalog longitudinalReportPath"
Assert-TextContains (Get-NormalizedFileText -Path $createProjectResult.StdOutPath) "Project: smoke-project" "Create-project stdout"

$renderProjectInitialResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("render-project", "--project", $projectPath) -Name "render-project-initial" -OutputDirectory $runRoot
Assert-TextContains (Get-NormalizedFileText -Path $renderProjectInitialResult.StdOutPath) "Indexed runs: 3" "Initial render-project stdout"
Assert-TextContains (Get-NormalizedFileText -Path $renderProjectInitialResult.StdOutPath) "Adjacent comparisons: 2" "Initial render-project stdout"
Assert-NonEmptyFile -Path $projectReportPath -Description "Initial project report"

$snapshot4Result = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $sampleManifest, "-o", $snapshot4Path) -Name "snapshot-4" -OutputDirectory $runRoot
Assert-NonEmptyFile -Path $snapshot4Path -Description "Fourth snapshot"
Assert-TextContains (Get-NormalizedFileText -Path $snapshot4Result.StdOutPath) "Snapshot written to" "Fourth snapshot stdout"

$projectHashBeforeAppend = Get-FileSha256 -Path $projectPath
$runIndexHashBeforeAppend = Get-FileSha256 -Path $indexPath
$snapshot1HashBeforeAppend = Get-FileSha256 -Path $snapshot1Path
$snapshot2HashBeforeAppend = Get-FileSha256 -Path $snapshot2Path
$snapshot3HashBeforeAppend = Get-FileSha256 -Path $snapshot3Path
$snapshot4HashBeforeAppend = Get-FileSha256 -Path $snapshot4Path
$projectReportHashBeforeAppend = Get-FileSha256 -Path $projectReportPath

$appendResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("append-project-snapshot", "--project", $projectPath, "--snapshot", $snapshot4Path) -Name "append-project-snapshot" -OutputDirectory $runRoot
$appendStdOut = Get-NormalizedFileText -Path $appendResult.StdOutPath
$appendLines = @($appendStdOut.TrimEnd("`n").Split("`n"))
$expectedAppendLines = @(
    "Project: smoke-project",
    "Appended snapshot: snapshots/run-004.json",
    "Indexed snapshots: 4"
)
if ($appendLines.Count -ne 4) {
    throw "Append-project-snapshot stdout did not contain exactly four public lines."
}

for ($i = 0; $i -lt $expectedAppendLines.Count; $i++) {
    Assert-TextEquals -Actual $appendLines[$i] -Expected $expectedAppendLines[$i] -Description "Append-project-snapshot stdout line $($i + 1)"
}

$runIndexLinePrefix = "Run index updated: "
if ($appendLines[3].IndexOf($runIndexLinePrefix, [StringComparison]::Ordinal) -ne 0) {
    throw "Append-project-snapshot stdout line 4 must start with 'Run index updated: '."
}

$actualRunIndexPath = $appendLines[3].Substring($runIndexLinePrefix.Length)
$expectedResolvedRunIndexPath = (Get-Item -LiteralPath $indexPath).FullName
$actualResolvedRunIndexPath = (Get-Item -LiteralPath $actualRunIndexPath).FullName
Assert-TextEquals -Actual $actualResolvedRunIndexPath -Expected $expectedResolvedRunIndexPath -Description "Append-project-snapshot run-index path"

$projectHashAfterAppend = Get-FileSha256 -Path $projectPath
$runIndexHashAfterAppend = Get-FileSha256 -Path $indexPath
$snapshot1HashAfterAppend = Get-FileSha256 -Path $snapshot1Path
$snapshot2HashAfterAppend = Get-FileSha256 -Path $snapshot2Path
$snapshot3HashAfterAppend = Get-FileSha256 -Path $snapshot3Path
$snapshot4HashAfterAppend = Get-FileSha256 -Path $snapshot4Path
$projectReportHashAfterAppend = Get-FileSha256 -Path $projectReportPath

Assert-DifferentHash -Actual $runIndexHashAfterAppend -Expected $runIndexHashBeforeAppend -Description "Run index"
Assert-EqualHash -Actual $projectHashAfterAppend -Expected $projectHashBeforeAppend -Description "Project catalog"
Assert-EqualHash -Actual $snapshot1HashAfterAppend -Expected $snapshot1HashBeforeAppend -Description "Snapshot 1"
Assert-EqualHash -Actual $snapshot2HashAfterAppend -Expected $snapshot2HashBeforeAppend -Description "Snapshot 2"
Assert-EqualHash -Actual $snapshot3HashAfterAppend -Expected $snapshot3HashBeforeAppend -Description "Snapshot 3"
Assert-EqualHash -Actual $snapshot4HashAfterAppend -Expected $snapshot4HashBeforeAppend -Description "Snapshot 4"
Assert-EqualHash -Actual $projectReportHashAfterAppend -Expected $projectReportHashBeforeAppend -Description "Project report"
Assert-NoReplacementTempFiles -RunIndexPath $indexPath

$runIndexJson = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
if ($runIndexJson.schemaVersion -ne 1) {
    throw "Run index schemaVersion must be 1."
}

if ($runIndexJson.snapshotPaths.Count -ne 4) {
    throw "Run index must contain exactly four snapshot paths after append."
}

$expectedSnapshotReferences = @(
    "snapshots/run-001.json",
    "snapshots/run-002.json",
    "snapshots/run-003.json",
    "snapshots/run-004.json"
)

for ($i = 0; $i -lt $expectedSnapshotReferences.Count; $i++) {
    $actualReference = [string] $runIndexJson.snapshotPaths[$i]
    Assert-TextEquals -Actual $actualReference -Expected $expectedSnapshotReferences[$i] -Description "Run-index snapshot reference $i"
    Assert-RelativeReference -Reference $actualReference -Description "Run-index snapshot reference $i"
}

$compareIndexUpdatedResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("compare-index", "--index", $indexPath, "-o", $projectReportPath) -Name "compare-index-updated" -OutputDirectory $runRoot
$compareIndexUpdatedStdOut = Get-NormalizedFileText -Path $compareIndexUpdatedResult.StdOutPath
Assert-TextContains -Text $compareIndexUpdatedStdOut -Expected "Indexed runs: 4" -Description "Updated compare-index stdout"
Assert-TextContains -Text $compareIndexUpdatedStdOut -Expected "Adjacent comparisons: 3" -Description "Updated compare-index stdout"
$compareIndexUpdatedHtmlBytes = [System.IO.File]::ReadAllBytes($projectReportPath)

$renderProjectUpdatedResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("render-project", "--project", $projectPath) -Name "render-project-updated" -OutputDirectory $runRoot
$renderProjectUpdatedStdOut = Get-NormalizedFileText -Path $renderProjectUpdatedResult.StdOutPath
Assert-TextContains -Text $renderProjectUpdatedStdOut -Expected "Indexed runs: 4" -Description "Updated render-project stdout"
Assert-TextContains -Text $renderProjectUpdatedStdOut -Expected "Adjacent comparisons: 3" -Description "Updated render-project stdout"
$renderProjectUpdatedHtmlBytes = [System.IO.File]::ReadAllBytes($projectReportPath)

Assert-TextEquals -Actual $compareIndexUpdatedStdOut -Expected $renderProjectUpdatedStdOut -Description "compare-index and render-project stdout"
Assert-ByteArrayEquals -Actual $renderProjectUpdatedHtmlBytes -Expected $compareIndexUpdatedHtmlBytes -Description "compare-index and render-project HTML"

Write-Output "Release smoke passed. Packaging smoke covered create-project, append-project-snapshot, and render-project with repeated anonymized fixtures; this is not real longitudinal validation."
