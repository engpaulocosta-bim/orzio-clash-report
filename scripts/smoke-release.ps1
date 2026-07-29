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

function Invoke-OrzioRaw {
    param(
        [string] $Executable,
        [string[]] $Arguments,
        [string] $Name,
        [string] $OutputDirectory
    )

    $stdoutPath = Join-Path $OutputDirectory "$Name.stdout.txt"
    $stderrPath = Join-Path $OutputDirectory "$Name.stderr.txt"

    $escapedArguments = @($Arguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + $_.Replace('"', '\"') + '"'
        }
        else {
            $_
        }
    })

    $process = Start-Process -FilePath $Executable `
        -ArgumentList $escapedArguments `
        -NoNewWindow `
        -PassThru `
        -Wait `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath

    $exitCode = $process.ExitCode

    return @{
        ExitCode = $exitCode
        StdOutPath = $stdoutPath
        StdErrPath = $stderrPath
    }
}

function Invoke-Orzio {
    param(
        [string] $Executable,
        [string[]] $Arguments,
        [string] $Name,
        [string] $OutputDirectory
    )

    $result = Invoke-OrzioRaw -Executable $Executable -Arguments $Arguments -Name $Name -OutputDirectory $OutputDirectory
    if ($result.ExitCode -ne 0) {
        throw "Command '$Name' failed with exit code $($result.ExitCode)."
    }

    if ((Get-Item -LiteralPath $result.StdErrPath).Length -ne 0) {
        throw "Command '$Name' wrote to stderr."
    }

    return $result
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

function Assert-TextDoesNotContain {
    param([string] $Text, [string] $Unexpected, [string] $Description)

    if ($Text.IndexOf($Unexpected, [StringComparison]::Ordinal) -ge 0) {
        throw "$Description contained unexpected text: $Unexpected"
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

function Assert-NoFilesMatching {
    param([string] $RootPath, [string] $Filter, [string] $Description)

    $files = @(Get-ChildItem -LiteralPath $RootPath -Recurse -File -Filter $Filter)
    if ($files.Count -ne 0) {
        throw "$Description were left behind."
    }
}

function Assert-Utf8WithoutBom {
    param([string] $Path, [string] $Description)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        throw "$Description must be UTF-8 without BOM."
    }
}

function Assert-LfOnly {
    param([string] $Path, [string] $Description)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    foreach ($value in $bytes) {
        if ($value -eq 13) {
            throw "$Description must use LF line endings only."
        }
    }
}

function Write-Utf8NoBom {
    param([string] $Path, [string] $Content)

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Write-CustomManifest {
    param(
        [string] $TemplatePath,
        [string] $OutputPath,
        [string] $RunId,
        [string] $CreatedAt
    )

    $template = Get-Content -LiteralPath $TemplatePath -Raw
    $custom = $template.Replace(
        '"runId": "coordination-sample-clash-xml"',
        ('"runId": "' + $RunId + '"'))
    $custom = $custom.Replace(
        '"createdAt": "2026-07-10T09:00:00+01:00"',
        ('"createdAt": "' + $CreatedAt + '"'))

    Write-Utf8NoBom -Path $OutputPath -Content $custom
}

function Set-SnapshotSourceModelToken {
    param([string] $SnapshotPath, [string] $EscapedJsonToken)

    $original = '"sourceModel": "Project_A_HVAC_PD_R00.rvt"'
    $replacement = '"sourceModel": "' + $EscapedJsonToken + '"'
    $json = Get-Content -LiteralPath $SnapshotPath -Raw

    if ($json.IndexOf($original, [StringComparison]::Ordinal) -lt 0) {
        throw "Expected sourceModel token was not found in snapshot '$SnapshotPath'."
    }

    $updated = $json.Replace($original, $replacement)
    Write-Utf8NoBom -Path $SnapshotPath -Content $updated
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
$manifestDirectory = Join-Path $runRoot "manifests"
New-Item -ItemType Directory -Path $reportsDirectory | Out-Null
New-Item -ItemType Directory -Path $snapshotDirectory | Out-Null
New-Item -ItemType Directory -Path $manifestDirectory | Out-Null

$singleRunReportPath = Join-Path $reportsDirectory "single-run-report.html"
$snapshot1Path = Join-Path $snapshotDirectory "run-001.json"
$snapshot2Path = Join-Path $snapshotDirectory "run-002.json"
$snapshot3Path = Join-Path $snapshotDirectory "run-003.json"
$snapshot4Path = Join-Path $snapshotDirectory "run-004.json"
$manifest1Path = Join-Path $manifestDirectory "run-001.manifest.json"
$manifest2Path = Join-Path $manifestDirectory "run-002.manifest.json"
$manifest3Path = Join-Path $manifestDirectory "run-003.manifest.json"
$manifest4Path = Join-Path $manifestDirectory "run-004.manifest.json"
$indexPath = Join-Path $runRoot "run-index.json"
$longitudinalPath = Join-Path $reportsDirectory "longitudinal-report.html"
$projectPath = Join-Path $runRoot "project.json"
$projectReportPath = Join-Path $reportsDirectory "project-longitudinal.html"
$governancePath = Join-Path $runRoot "identity-governance.json"
$reviewReportPath = Join-Path $reportsDirectory "identity-governance-review.html"
$invalidGovernancePath = Join-Path $runRoot "identity-governance-invalid.json"
$failureReviewPath = Join-Path $reportsDirectory "identity-governance-invalid-review.html"

Write-CustomManifest -TemplatePath $sampleManifest -OutputPath $manifest1Path -RunId "run-001" -CreatedAt "2026-07-10T09:00:00+01:00"
Write-CustomManifest -TemplatePath $sampleManifest -OutputPath $manifest2Path -RunId "run-002" -CreatedAt "2026-07-17T09:00:00+01:00"
Write-CustomManifest -TemplatePath $sampleManifest -OutputPath $manifest3Path -RunId "run-003" -CreatedAt "2026-07-24T09:00:00+01:00"
Write-CustomManifest -TemplatePath $sampleManifest -OutputPath $manifest4Path -RunId "run-004" -CreatedAt "2026-07-31T09:00:00+01:00"

$versionResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("--version") -Name "version" -OutputDirectory $runRoot
$helpResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("--help") -Name "help" -OutputDirectory $runRoot
$singleRunResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @($sampleXml, "-o", $singleRunReportPath) -Name "single-run-report" -OutputDirectory $runRoot
$snapshot1Result = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $manifest1Path, "-o", $snapshot1Path) -Name "snapshot-1" -OutputDirectory $runRoot
$snapshot2Result = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $manifest2Path, "-o", $snapshot2Path) -Name "snapshot-2" -OutputDirectory $runRoot
$snapshot3Result = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $manifest3Path, "-o", $snapshot3Path) -Name "snapshot-3" -OutputDirectory $runRoot

Set-SnapshotSourceModelToken -SnapshotPath $snapshot1Path -EscapedJsonToken 'C:\\Clients\\Acme\\Secret\\Model-A.nwc'
Set-SnapshotSourceModelToken -SnapshotPath $snapshot2Path -EscapedJsonToken '\\\\fileserver\\private\\Model-B.nwc'
Set-SnapshotSourceModelToken -SnapshotPath $snapshot3Path -EscapedJsonToken '/srv/customer/private/Model-C.nwc'

$indexResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("index-snapshots", "--snapshot", $snapshot1Path, "--snapshot", $snapshot2Path, "--snapshot", $snapshot3Path, "-o", $indexPath) -Name "index-snapshots" -OutputDirectory $runRoot
$compareIndexInitialResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("compare-index", "--index", $indexPath, "-o", $longitudinalPath) -Name "compare-index-initial" -OutputDirectory $runRoot

Assert-TextEquals (Get-NormalizedFileText -Path $versionResult.StdOutPath) "orzioclash 0.1.0-preview.3`n" "Version output"
Assert-TextContains (Get-NormalizedFileText -Path $helpResult.StdOutPath) "append-project-snapshot" "Help output"
Assert-TextContains (Get-NormalizedFileText -Path $helpResult.StdOutPath) "render-identity-governance-report" "Help output"
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

$snapshot4Result = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("snapshot", "--xml", $sampleXml, "--manifest", $manifest4Path, "-o", $snapshot4Path) -Name "snapshot-4" -OutputDirectory $runRoot
Set-SnapshotSourceModelToken -SnapshotPath $snapshot4Path -EscapedJsonToken 'C:\\Clients\\Acme\\Secret\\Model-A.nwc'
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
Assert-NoFilesMatching -RootPath $runRoot -Filter ".run-index-replace-*.tmp" -Description "Run-index replacement temp files"

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

$createGovernanceResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("create-identity-governance", "--project-id", "smoke-project", "-o", $governancePath) -Name "create-identity-governance" -OutputDirectory $runRoot
$createGovernanceStdOut = Get-NormalizedFileText -Path $createGovernanceResult.StdOutPath
Assert-TextEquals -Actual $createGovernanceStdOut -Expected ("Project: smoke-project`nDecisions: 0`nIdentity governance written to " + (Get-Item -LiteralPath $governancePath).FullName + "`n") -Description "Create-identity-governance stdout"
Assert-NonEmptyFile -Path $governancePath -Description "Identity-governance JSON"
Assert-Utf8WithoutBom -Path $governancePath -Description "Identity-governance JSON"
Assert-LfOnly -Path $governancePath -Description "Identity-governance JSON"

$governanceCreateJson = Get-Content -LiteralPath $governancePath -Raw | ConvertFrom-Json
if ($governanceCreateJson.schemaVersion -ne 1) {
    throw "Identity-governance schemaVersion must be 1."
}

Assert-TextEquals -Actual ([string] $governanceCreateJson.projectId) -Expected "smoke-project" -Description "Identity-governance projectId"
if ($governanceCreateJson.decisions.Count -ne 0) {
    throw "Identity-governance document must start with zero decisions."
}

$governanceHashAfterCreate = Get-FileSha256 -Path $governancePath
$createGovernanceAgainResult = Invoke-OrzioRaw -Executable $resolvedExecutable -Arguments @("create-identity-governance", "--project-id", "smoke-project", "-o", $governancePath) -Name "create-identity-governance-existing" -OutputDirectory $runRoot
if ($createGovernanceAgainResult.ExitCode -ne 1) {
    throw "create-identity-governance existing-output check must fail with exit code 1."
}

Assert-TextEquals -Actual (Get-NormalizedFileText -Path $createGovernanceAgainResult.StdOutPath) -Expected "" -Description "Create-identity-governance existing-output stdout"
Assert-TextContains -Text (Get-NormalizedFileText -Path $createGovernanceAgainResult.StdErrPath) -Expected "Failed to create identity governance: Identity governance file already exists." -Description "Create-identity-governance existing-output stderr"
Assert-EqualHash -Actual (Get-FileSha256 -Path $governancePath) -Expected $governanceHashAfterCreate -Description "Identity-governance create-new bytes"
Assert-NoFilesMatching -RootPath $runRoot -Filter ".identity-governance-replace-*.tmp" -Description "Identity-governance replacement temp files"

$appendConfirmResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @(
    "append-identity-decision",
    "--governance", $governancePath,
    "--decision-id", "decision-001",
    "--decision-kind", "ConfirmSameIdentity",
    "--left-run-id", "run-001",
    "--left-occurrence-index", "0",
    "--right-run-id", "run-002",
    "--right-occurrence-index", "0",
    "--persistent-identity-id", "identity-001",
    "--reviewer-alias", "coordinator-a",
    "--reason", "Confirmed from model context"
) -Name "append-identity-decision-confirm" -OutputDirectory $runRoot

$governanceHashAfterConfirm = Get-FileSha256 -Path $governancePath
Assert-DifferentHash -Actual $governanceHashAfterConfirm -Expected $governanceHashAfterCreate -Description "Identity-governance after confirmation append"
$governanceAfterConfirm = Get-Content -LiteralPath $governancePath -Raw | ConvertFrom-Json
if ($governanceAfterConfirm.decisions.Count -ne 1) {
    throw "Governance file must contain exactly one decision after confirmation append."
}

Assert-TextEquals -Actual ([string] $governanceAfterConfirm.decisions[0].persistentIdentityId) -Expected "identity-001" -Description "Confirmation persistent identity id"
Assert-NoFilesMatching -RootPath $runRoot -Filter ".identity-governance-replace-*.tmp" -Description "Identity-governance replacement temp files"

$appendRejectResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @(
    "append-identity-decision",
    "--governance", $governancePath,
    "--decision-id", "decision-002",
    "--decision-kind", "RejectSameIdentity",
    "--left-run-id", "run-003",
    "--left-occurrence-index", "1",
    "--right-run-id", "run-004",
    "--right-occurrence-index", "1",
    "--reviewer-alias", "coordinator-b"
) -Name "append-identity-decision-reject" -OutputDirectory $runRoot

$governanceHashAfterReject = Get-FileSha256 -Path $governancePath
Assert-DifferentHash -Actual $governanceHashAfterReject -Expected $governanceHashAfterConfirm -Description "Identity-governance after rejection append"
$governanceAfterReject = Get-Content -LiteralPath $governancePath -Raw | ConvertFrom-Json
if ($governanceAfterReject.decisions.Count -ne 2) {
    throw "Governance file must contain exactly two decisions after rejection append."
}

Assert-TextEquals -Actual ([string] $governanceAfterReject.decisions[0].decisionKind) -Expected "ConfirmSameIdentity" -Description "Governance decision order 1"
Assert-TextEquals -Actual ([string] $governanceAfterReject.decisions[1].decisionKind) -Expected "RejectSameIdentity" -Description "Governance decision order 2"
if ($governanceAfterReject.decisions[1].PSObject.Properties.Name -contains "persistentIdentityId") {
    throw "RejectSameIdentity must not persist a persistentIdentityId."
}
Assert-NoFilesMatching -RootPath $runRoot -Filter ".identity-governance-replace-*.tmp" -Description "Identity-governance replacement temp files"

$projectHashBeforeGovernanceReadOnly = Get-FileSha256 -Path $projectPath
$runIndexHashBeforeGovernanceReadOnly = Get-FileSha256 -Path $indexPath
$snapshot1HashBeforeGovernanceReadOnly = Get-FileSha256 -Path $snapshot1Path
$snapshot2HashBeforeGovernanceReadOnly = Get-FileSha256 -Path $snapshot2Path
$snapshot3HashBeforeGovernanceReadOnly = Get-FileSha256 -Path $snapshot3Path
$snapshot4HashBeforeGovernanceReadOnly = Get-FileSha256 -Path $snapshot4Path
$governanceHashBeforeGovernanceReadOnly = Get-FileSha256 -Path $governancePath
$longitudinalHashBeforeGovernanceReadOnly = Get-FileSha256 -Path $projectReportPath

$validateGovernanceResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("validate-identity-governance", "--project", $projectPath, "--governance", $governancePath) -Name "validate-identity-governance" -OutputDirectory $runRoot
$validateGovernanceStdOut = Get-NormalizedFileText -Path $validateGovernanceResult.StdOutPath
$expectedValidateGovernanceStdOut =
    "Project: smoke-project`n" +
    "Indexed runs: 4`n" +
    "Decisions: 2`n" +
    "Confirmations: 1`n" +
    "Rejections: 1`n" +
    "Evidence endpoints: 4`n" +
    "Identity governance validation passed.`n"
Assert-TextEquals -Actual $validateGovernanceStdOut -Expected $expectedValidateGovernanceStdOut -Description "Validate-identity-governance stdout"

$renderReviewResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("render-identity-governance-report", "--project", $projectPath, "--governance", $governancePath, "-o", $reviewReportPath) -Name "render-identity-governance-review-1" -OutputDirectory $runRoot
$renderReviewStdOut = Get-NormalizedFileText -Path $renderReviewResult.StdOutPath
Assert-TextEquals -Actual $renderReviewStdOut -Expected ("Project: smoke-project`nIndexed runs: 4`nDecisions: 2`nConfirmations: 1`nRejections: 1`nEvidence endpoints: 4`nIdentity governance review written to " + (Get-Item -LiteralPath $reviewReportPath).FullName + "`n") -Description "Render-identity-governance-report stdout"
Assert-NonEmptyFile -Path $reviewReportPath -Description "Identity-governance review HTML"
Assert-Utf8WithoutBom -Path $reviewReportPath -Description "Identity-governance review HTML"
Assert-LfOnly -Path $reviewReportPath -Description "Identity-governance review HTML"

$reviewHtml = Get-NormalizedFileText -Path $reviewReportPath
Assert-TextContains -Text $reviewHtml -Expected "<title>Identity Governance Review</title>" -Description "Identity-governance review HTML"
Assert-TextContains -Text $reviewHtml -Expected "ConfirmSameIdentity" -Description "Identity-governance review HTML"
Assert-TextContains -Text $reviewHtml -Expected "RejectSameIdentity" -Description "Identity-governance review HTML"
if ([regex]::Matches($reviewHtml, "Persistent identity id").Count -ne 1) {
    throw "Identity-governance review HTML must contain 'Persistent identity id' exactly once."
}
Assert-TextDoesNotContain -Text $reviewHtml -Unexpected "Element A source model" -Description "Identity-governance review HTML"
Assert-TextDoesNotContain -Text $reviewHtml -Unexpected "Element B source model" -Description "Identity-governance review HTML"
Assert-TextDoesNotContain -Text $reviewHtml -Unexpected "C:\Clients" -Description "Identity-governance review HTML"
Assert-TextDoesNotContain -Text $reviewHtml -Unexpected "\\fileserver" -Description "Identity-governance review HTML"
Assert-TextDoesNotContain -Text $reviewHtml -Unexpected "/srv/customer" -Description "Identity-governance review HTML"

$reviewHtmlBytesFirst = [System.IO.File]::ReadAllBytes($reviewReportPath)
$renderReviewAgainResult = Invoke-Orzio -Executable $resolvedExecutable -Arguments @("render-identity-governance-report", "--project", $projectPath, "--governance", $governancePath, "-o", $reviewReportPath) -Name "render-identity-governance-review-2" -OutputDirectory $runRoot
$reviewHtmlBytesSecond = [System.IO.File]::ReadAllBytes($reviewReportPath)
Assert-ByteArrayEquals -Actual $reviewHtmlBytesSecond -Expected $reviewHtmlBytesFirst -Description "Repeated identity-governance review HTML"
Assert-NoFilesMatching -RootPath $runRoot -Filter ".derived-html-report-*.tmp" -Description "Derived HTML temp files"

Assert-EqualHash -Actual (Get-FileSha256 -Path $projectPath) -Expected $projectHashBeforeGovernanceReadOnly -Description "Project catalog after governance validation/render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $indexPath) -Expected $runIndexHashBeforeGovernanceReadOnly -Description "Run index after governance validation/render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $snapshot1Path) -Expected $snapshot1HashBeforeGovernanceReadOnly -Description "Snapshot 1 after governance validation/render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $snapshot2Path) -Expected $snapshot2HashBeforeGovernanceReadOnly -Description "Snapshot 2 after governance validation/render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $snapshot3Path) -Expected $snapshot3HashBeforeGovernanceReadOnly -Description "Snapshot 3 after governance validation/render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $snapshot4Path) -Expected $snapshot4HashBeforeGovernanceReadOnly -Description "Snapshot 4 after governance validation/render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $governancePath) -Expected $governanceHashBeforeGovernanceReadOnly -Description "Governance after validation/render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $projectReportPath) -Expected $longitudinalHashBeforeGovernanceReadOnly -Description "Longitudinal report after governance validation/render"

$validGovernanceJson = Get-Content -LiteralPath $governancePath -Raw
$invalidGovernanceJson = $validGovernanceJson.Replace('"runId": "run-001"', '"runId": "run-missing"')
Write-Utf8NoBom -Path $invalidGovernancePath -Content $invalidGovernanceJson
Write-Utf8NoBom -Path $failureReviewPath -Content "SENTINEL-REVIEW"

$projectHashBeforeFailure = Get-FileSha256 -Path $projectPath
$runIndexHashBeforeFailure = Get-FileSha256 -Path $indexPath
$snapshot1HashBeforeFailure = Get-FileSha256 -Path $snapshot1Path
$snapshot2HashBeforeFailure = Get-FileSha256 -Path $snapshot2Path
$snapshot3HashBeforeFailure = Get-FileSha256 -Path $snapshot3Path
$snapshot4HashBeforeFailure = Get-FileSha256 -Path $snapshot4Path
$governanceHashBeforeFailure = Get-FileSha256 -Path $governancePath
$invalidGovernanceHashBeforeFailure = Get-FileSha256 -Path $invalidGovernancePath
$longitudinalHashBeforeFailure = Get-FileSha256 -Path $projectReportPath
$failureReviewHashBeforeFailure = Get-FileSha256 -Path $failureReviewPath

$failureResult = Invoke-OrzioRaw -Executable $resolvedExecutable -Arguments @("render-identity-governance-report", "--project", $projectPath, "--governance", $invalidGovernancePath, "-o", $failureReviewPath) -Name "render-identity-governance-review-invalid" -OutputDirectory $runRoot
if ($failureResult.ExitCode -ne 1) {
    throw "Invalid render-identity-governance-report must fail with exit code 1."
}

Assert-TextEquals -Actual (Get-NormalizedFileText -Path $failureResult.StdOutPath) -Expected "" -Description "Invalid render-identity-governance-report stdout"
$failureStdErr = Get-NormalizedFileText -Path $failureResult.StdErrPath
Assert-TextContains -Text $failureStdErr -Expected "Identity governance validation failed." -Description "Invalid render-identity-governance-report stderr"
Assert-TextContains -Text $failureStdErr -Expected "run-missing" -Description "Invalid render-identity-governance-report stderr"

Assert-EqualHash -Actual (Get-FileSha256 -Path $projectPath) -Expected $projectHashBeforeFailure -Description "Project catalog after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $indexPath) -Expected $runIndexHashBeforeFailure -Description "Run index after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $snapshot1Path) -Expected $snapshot1HashBeforeFailure -Description "Snapshot 1 after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $snapshot2Path) -Expected $snapshot2HashBeforeFailure -Description "Snapshot 2 after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $snapshot3Path) -Expected $snapshot3HashBeforeFailure -Description "Snapshot 3 after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $snapshot4Path) -Expected $snapshot4HashBeforeFailure -Description "Snapshot 4 after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $governancePath) -Expected $governanceHashBeforeFailure -Description "Governance after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $invalidGovernancePath) -Expected $invalidGovernanceHashBeforeFailure -Description "Invalid governance after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $projectReportPath) -Expected $longitudinalHashBeforeFailure -Description "Longitudinal report after invalid render"
Assert-EqualHash -Actual (Get-FileSha256 -Path $failureReviewPath) -Expected $failureReviewHashBeforeFailure -Description "Sentinel review output after invalid render"
Assert-NoFilesMatching -RootPath $runRoot -Filter ".derived-html-report-*.tmp" -Description "Derived HTML temp files"
Assert-NoFilesMatching -RootPath $runRoot -Filter ".identity-governance-replace-*.tmp" -Description "Identity-governance replacement temp files"
Assert-NoFilesMatching -RootPath $runRoot -Filter ".run-index-replace-*.tmp" -Description "Run-index replacement temp files"

Write-Output "Release smoke passed. Packaging smoke covered project catalog, identity-governance authoring, read-only evidence validation, and standalone review rendering with repeated anonymized fixtures only; this is not real longitudinal validation."
