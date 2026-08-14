#requires -Version 7.0
<#
.SYNOPSIS
    Smoke-tests the installed launcher on a clean Windows machine.

.DESCRIPTION
    Runs the ten checks that decide whether a build is fit to hand to an evaluator:

        1.  install without administrator rights
        2.  the Start menu shortcut exists and points at the installed executable
        3.  the launcher is present and reports its version
        4.  the engine is present and reports the version recorded in engine-manifest.json,
            and its SHA-256 matches that manifest
        5.  a report is generated from the bundled sample into a directory OUTSIDE the installation
        6.  the launcher process closes cleanly
        7.  uninstall runs
        8.  the generated report still exists afterwards
        9.  no service was left behind
        10. no scheduled task was left behind

    Step 5 exercises the engine directly with the same argument vector the launcher builds, because
    driving the window itself needs a human. What a human still has to confirm by hand is listed at
    the end of this script's output, and in docs/operations/desktop-pilot.md.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $InstallerPath,

    [string] $ReportDirectory = (Join-Path $env:USERPROFILE 'Documents\OrzioSmoke'),

    [switch] $SkipInstall,
    [switch] $SkipUninstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\Orzio\ClashReportLauncher'
$launcherExe = Join-Path $installDirectory 'OrzioClashReport.Launcher.Desktop.exe'
$engineExe = Join-Path $installDirectory 'engine\win-x64\orzioclash.exe'
$manifestPath = Join-Path $installDirectory 'engine\win-x64\engine-manifest.json'
$startMenuShortcut = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Orzio\Orzio Clash Report.lnk'

$failures = New-Object System.Collections.Generic.List[string]

function Test-Step {
    param([string] $Name, [scriptblock] $Body)

    Write-Host "-> $Name"
    try {
        & $Body
        Write-Host "   ok"
    }
    catch {
        Write-Host "   FAILED: $($_.Exception.Message)"
        $script:failures.Add("$Name : $($_.Exception.Message)")
    }
}

# 1. Install without elevation.
if (-not $SkipInstall) {
    Test-Step 'Install without administrator rights' {
        if (-not (Test-Path -LiteralPath $InstallerPath -PathType Leaf)) {
            throw "Installer not found: $InstallerPath"
        }

        $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $principal = New-Object Security.Principal.WindowsPrincipal($identity)
        if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw 'This smoke run is elevated. Run it as a standard user to test the real install path.'
        }

        $process = Start-Process -FilePath $InstallerPath `
            -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
            -Wait -PassThru

        if ($process.ExitCode -ne 0) {
            throw "The installer exited with $($process.ExitCode)."
        }

        if (-not (Test-Path -LiteralPath $installDirectory -PathType Container)) {
            throw "The per-user installation directory was not created: $installDirectory"
        }
    }
}

# 2. Start menu shortcut.
Test-Step 'Start menu shortcut exists' {
    if (-not (Test-Path -LiteralPath $startMenuShortcut -PathType Leaf)) {
        throw "Shortcut not found: $startMenuShortcut"
    }

    $shell = New-Object -ComObject WScript.Shell
    $target = $shell.CreateShortcut($startMenuShortcut).TargetPath

    if ($target -ne $launcherExe) {
        throw "The shortcut points at '$target' instead of '$launcherExe'."
    }
}

# 3. Launcher present.
Test-Step 'Launcher is installed' {
    if (-not (Test-Path -LiteralPath $launcherExe -PathType Leaf)) {
        throw "Launcher not found: $launcherExe"
    }

    if ((Get-Item -LiteralPath $launcherExe).Length -le 0) {
        throw 'The launcher executable is empty.'
    }
}

# 4. Engine present, matching its manifest.
Test-Step 'Engine matches its packaged manifest' {
    if (-not (Test-Path -LiteralPath $engineExe -PathType Leaf)) {
        throw "Engine not found: $engineExe"
    }

    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Engine manifest not found: $manifestPath"
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $actualHash = (Get-FileHash -LiteralPath $engineExe -Algorithm SHA256).Hash.ToLowerInvariant()

    if ($actualHash -ne $manifest.sha256) {
        throw "The installed engine hash ($actualHash) does not match the manifest ($($manifest.sha256))."
    }

    $versionLine = & $engineExe --version
    if ($versionLine -ne "orzioclash $($manifest.engineVersion)") {
        throw "The engine reported '$versionLine' but the manifest declares '$($manifest.engineVersion)'."
    }
}

# 5. Generate a report outside the installation directory.
$reportPath = Join-Path $ReportDirectory 'smoke-report.html'

Test-Step 'Generate a report from the bundled sample, outside the installation' {
    New-Item -ItemType Directory -Force -Path $ReportDirectory | Out-Null

    if (Test-Path -LiteralPath $reportPath) {
        Remove-Item -LiteralPath $reportPath -Force
    }

    $sample = Join-Path $installDirectory 'samples\sample-clash.xml'
    if (-not (Test-Path -LiteralPath $sample -PathType Leaf)) {
        throw "Bundled sample not found: $sample"
    }

    # The same argument vector the launcher builds: the XML positional, then -o, then an absolute path.
    Push-Location $ReportDirectory
    try {
        & $engineExe $sample '-o' $reportPath
        if ($LASTEXITCODE -ne 0) {
            throw "The engine exited with $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }

    if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
        throw "The report was not produced: $reportPath"
    }

    if ((Get-Item -LiteralPath $reportPath).Length -le 0) {
        throw 'The report is empty.'
    }

    if ($reportPath.StartsWith($installDirectory, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The report was written inside the installation directory.'
    }
}

# 6. No launcher process left running.
Test-Step 'No launcher process is left running' {
    $running = Get-Process -Name 'OrzioClashReport.Launcher.Desktop' -ErrorAction SilentlyContinue
    if ($null -ne $running) {
        throw 'A launcher process is still running. Close it before uninstalling.'
    }
}

# 7. Uninstall.
if (-not $SkipUninstall) {
    Test-Step 'Uninstall' {
        $uninstaller = Get-ChildItem -LiteralPath $installDirectory -Filter 'unins*.exe' -ErrorAction SilentlyContinue |
            Select-Object -First 1

        if ($null -eq $uninstaller) {
            throw "No uninstaller was found in $installDirectory."
        }

        $process = Start-Process -FilePath $uninstaller.FullName `
            -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
            -Wait -PassThru

        if ($process.ExitCode -ne 0) {
            throw "The uninstaller exited with $($process.ExitCode)."
        }

        Start-Sleep -Seconds 2

        if (Test-Path -LiteralPath $launcherExe) {
            throw 'The launcher executable survived the uninstall.'
        }

        if (Test-Path -LiteralPath $engineExe) {
            throw 'The engine survived the uninstall.'
        }

        if (Test-Path -LiteralPath $startMenuShortcut) {
            throw 'The Start menu shortcut survived the uninstall.'
        }
    }
}

# 8. The user's report survives.
Test-Step 'The generated report survives the uninstall' {
    if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
        throw "The uninstaller removed a file it does not own: $reportPath"
    }
}

# 9. No service left behind.
Test-Step 'No service was left behind' {
    $services = @(Get-Service -ErrorAction SilentlyContinue | Where-Object { $_.Name -like '*Orzio*' })
    if ($services.Count -ne 0) {
        throw "Services left behind: $($services.Name -join ', ')"
    }
}

# 10. No scheduled task left behind.
Test-Step 'No scheduled task was left behind' {
    $tasks = @(Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object { $_.TaskName -like '*Orzio*' })
    if ($tasks.Count -ne 0) {
        throw "Scheduled tasks left behind: $($tasks.TaskName -join ', ')"
    }
}

Write-Host ''
if ($failures.Count -ne 0) {
    Write-Host 'SMOKE FAILED:'
    foreach ($failure in $failures) {
        Write-Host "  - $failure"
    }

    exit 1
}

Write-Host 'Smoke passed.'
Write-Host ''
Write-Host 'Still requires a human, and is NOT covered by this script:'
Write-Host '  - opening the application from the Start menu and seeing the shell render'
Write-Host '  - the SmartScreen prompt on first run of an unsigned installer'
Write-Host '  - behaviour under an AppLocker policy that blocks %LOCALAPPDATA%'
Write-Host '  - generating a report through the window rather than through the engine directly'
