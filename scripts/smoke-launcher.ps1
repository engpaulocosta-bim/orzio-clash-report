#Requires -Version 5.1
<#
.SYNOPSIS
    Smoke test for an installed Orzio Clash Report Desktop, run on a clean Windows machine.

.DESCRIPTION
    Verifies what can be verified without a human at the keyboard: the installed layout, the engine
    identity and integrity, and that the engine produces a report from the packaged sample into a
    directory OUTSIDE the installation. After uninstalling, it verifies that the report is still
    there, that the installation directory is gone, and that no service or scheduled task was left
    behind.

    The steps a human must perform are listed by the script and are not claimed by it: installing
    without administrator rights, opening the application from the Start menu, and confirming the
    window works are human observations, not script output.
#>
[CmdletBinding()]
param(
    [string] $InstallDirectory = (Join-Path $env:LOCALAPPDATA "Programs\Orzio\ClashReportLauncher"),
    [string] $SmokeWorkspace = (Join-Path ([System.IO.Path]::GetTempPath()) "orzio-launcher-smoke"),
    [switch] $AfterUninstall
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Assert-True {
    param([bool] $Condition, [string] $Message)

    if (-not $Condition) {
        # Each message states what should have been true, so a failure has to say it was not.
        # Throwing the expectation on its own reads like a success that crashed.
        throw "CHECK FAILED, this is not true: $Message"
    }

    Write-Host "  ok  $Message"
}

if ($AfterUninstall) {
    Write-Host "Checking what remains after uninstalling..."

    $report = Join-Path $SmokeWorkspace "smoke-report.html"

    if (-not (Test-Path -LiteralPath $report)) {
        # An error record renders as one paragraph, so the explanation is written out first and the
        # thrown message is the single sentence that says what to do.
        Write-Host ""
        Write-Host "This run verifies what survived uninstalling, so it expects a report left behind"
        Write-Host "by the installed-state run at:"
        Write-Host "  $report"
        Write-Host ""
        Write-Host "There is no report there, which means the installed-state run never happened."
        Write-Host ""
        throw "Run this script without -AfterUninstall first, while the application is still installed."
    }

    Assert-True (Test-Path -LiteralPath $report -PathType Leaf) `
        "The report produced before uninstalling still exists."
    Assert-True ((Get-Item -LiteralPath $report).Length -gt 0) `
        "The report produced before uninstalling is not empty."

    Assert-True (-not (Test-Path -LiteralPath (Join-Path $InstallDirectory "OrzioClashReport.Launcher.Desktop.exe"))) `
        "The launcher executable was removed."
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $InstallDirectory "engine"))) `
        "The engine directory was removed."
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $InstallDirectory "samples"))) `
        "The samples directory was removed."
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $InstallDirectory "docs"))) `
        "The docs directory were removed."

    $services = @(Get-Service | Where-Object { $_.Name -like "*Orzio*" -or $_.DisplayName -like "*Orzio*" })
    Assert-True ($services.Count -eq 0) "No service named after Orzio was left behind."

    $tasks = @(Get-ScheduledTask -ErrorAction SilentlyContinue |
        Where-Object { $_.TaskName -like "*Orzio*" -or $_.TaskPath -like "*Orzio*" })
    Assert-True ($tasks.Count -eq 0) "No scheduled task named after Orzio was left behind."

    Write-Host ""
    Write-Host "Post-uninstall checks passed."
    Write-Host "Still to confirm by hand: the Start menu entry is gone, and any project data you"
    Write-Host "created is untouched."
    return
}

Write-Host "Checking the installed layout..."

$launcher = Join-Path $InstallDirectory "OrzioClashReport.Launcher.Desktop.exe"
$engine = Join-Path $InstallDirectory "engine\win-x64\orzioclash.exe"
$manifestPath = Join-Path $InstallDirectory "engine\win-x64\engine-manifest.json"
$sample = Join-Path $InstallDirectory "samples\sample-clash.xml"

if (-not (Test-Path -LiteralPath $InstallDirectory -PathType Container)) {
    # An error record renders as one paragraph, so the explanation is written out first and the
    # thrown message is the single sentence that says what to do.
    Write-Host ""
    Write-Host "Nothing is installed at:"
    Write-Host "  $InstallDirectory"
    Write-Host ""
    Write-Host "This script checks an installed application; it does not install one. Build the"
    Write-Host "installer with scripts/publish-launcher.ps1 and scripts/package-launcher.ps1, then"
    Write-Host "run the installer it produces. Pass -InstallDirectory if you installed elsewhere."
    Write-Host ""
    throw "Install the application first, then run this script again."
}

Assert-True (Test-Path -LiteralPath $launcher -PathType Leaf) "The launcher executable is installed."
Assert-True (Test-Path -LiteralPath $engine -PathType Leaf) "The engine executable is installed."
Assert-True (Test-Path -LiteralPath $manifestPath -PathType Leaf) "The engine manifest is installed."
Assert-True (Test-Path -LiteralPath $sample -PathType Leaf) "The sample export is installed."

Assert-True ($InstallDirectory -notlike "$env:ProgramFiles*") `
    "The installation is per user, not under Program Files."

Write-Host "Verifying the engine identity..."
$versionLine = & $engine --version
if ($LASTEXITCODE -ne 0) { throw "The engine did not answer --version." }
Assert-True ($versionLine -match '^orzioclash [0-9]+\.[0-9]+\.[0-9]+-[A-Za-z0-9.]+$') `
    "The engine reports its version in the published format: $versionLine"

Write-Host "Verifying the engine integrity against the packaged manifest..."
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$actual = (Get-FileHash -LiteralPath $engine -Algorithm SHA256).Hash.ToLowerInvariant()
Assert-True ($manifest.sha256 -eq $actual) "The installed engine matches the packaged SHA-256."
Assert-True ($versionLine -eq "orzioclash $($manifest.engineVersion)") `
    "The engine version matches the manifest."

Write-Host "Generating a report from the packaged sample, OUTSIDE the installation..."
New-Item -ItemType Directory -Force -Path $SmokeWorkspace | Out-Null
$report = Join-Path $SmokeWorkspace "smoke-report.html"
if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report -Force }

Assert-True ($SmokeWorkspace -notlike "$InstallDirectory*") `
    "The report destination is outside the installation directory."

Push-Location $SmokeWorkspace
try {
    & $engine $sample -o $report
    if ($LASTEXITCODE -ne 0) { throw "The engine failed to generate the sample report." }
}
finally {
    Pop-Location
}

Assert-True (Test-Path -LiteralPath $report -PathType Leaf) "The sample report was produced."
Assert-True ((Get-Item -LiteralPath $report).Length -gt 0) "The sample report is not empty."

Write-Host ""
Write-Host "Installed-state checks passed."
Write-Host ""
Write-Host "Still to confirm by hand, in this order:"
Write-Host "  1. The install ran without an administrator prompt."
Write-Host "  2. The application opens from the Start menu."
Write-Host "  3. The engine badge reads 'Motor pronto'."
Write-Host "  4. Relatório rápido produces a report into a folder outside the installation."
Write-Host "  5. Close the application, then uninstall it."
Write-Host "  6. Re-run this script with -AfterUninstall."
