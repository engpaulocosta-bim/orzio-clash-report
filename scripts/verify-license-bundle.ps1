#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Directory,
    [string] $RuntimeConfigPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Directory = (Resolve-Path -LiteralPath $Directory).Path
foreach ($required in @('LICENSE', 'THIRD-PARTY-NOTICES.md', 'licenses/inventory.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $Directory $required) -PathType Leaf)) {
        throw "Missing license bundle file: $required"
    }
}
$inventory = Get-Content -LiteralPath (Join-Path $Directory 'licenses/inventory.json') -Raw | ConvertFrom-Json
if ($inventory.schemaVersion -ne 1 -or @($inventory.noticeFiles).Count -eq 0) {
    throw 'Unsupported or empty license inventory.'
}
$expected = @('inventory.json')
foreach ($notice in $inventory.noticeFiles) {
    if ($notice.path -cnotmatch '^licenses/[A-Za-z0-9._-]+\.txt$' -or
        $notice.sha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Invalid notice path or SHA-256 in license inventory.'
    }
    $path = Join-Path $Directory $notice.path
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $notice.sha256) {
        throw "Missing or modified third-party notice: $($notice.path)"
    }
    $expected += Split-Path -Leaf $notice.path
}
$actual = @(Get-ChildItem -LiteralPath (Join-Path $Directory 'licenses') -Recurse -File |
    ForEach-Object { $_.FullName.Substring((Join-Path $Directory 'licenses').Length + 1).Replace('\', '/') })
if (@(Compare-Object ($expected | Sort-Object -Unique) ($actual | Sort-Object -Unique)).Count -ne 0) {
    throw 'Unexpected or missing files in the license bundle.'
}
if ($RuntimeConfigPath -and -not (Test-Path -LiteralPath $RuntimeConfigPath -PathType Leaf)) {
    throw 'The supplied runtime configuration is missing.'
}
$runtimeConfig = if ($RuntimeConfigPath) { $RuntimeConfigPath } else {
    Join-Path $Directory 'OrzioClashReport.Launcher.Desktop.runtimeconfig.json'
}
if (Test-Path -LiteralPath $runtimeConfig -PathType Leaf) {
    $runtime = (Get-Content -LiteralPath $runtimeConfig -Raw | ConvertFrom-Json).runtimeOptions.includedFrameworks |
        Where-Object { $_.name -eq 'Microsoft.NETCore.App' }
    $runtimeNotice = $inventory.components | Where-Object {
        $_.package -eq 'Microsoft.NETCore.App.Runtime.win-x64' -and $_.version -eq $runtime.version
    }
    if (@($runtime).Count -ne 1 -or @($runtimeNotice).Count -ne 1 -or $runtime.version -ne $runtimeNotice.version) {
        throw 'The staged .NET runtime does not match the license inventory. Refresh the notices.'
    }
}
Write-Host "Verified $($inventory.noticeFiles.Count) third-party notice files."
