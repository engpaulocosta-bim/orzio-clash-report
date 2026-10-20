#requires -Version 7.0
<#
.SYNOPSIS
    Reads the desktop launcher version from src/LauncherVersion.props, its single source.

.DESCRIPTION
    Returns an object with Prefix (numeric, e.g. 0.2.1), Suffix (e.g. launcher-preview.1, may be empty),
    Full (Prefix or Prefix-Suffix) and Tag (desktop-v<Full>). Used by the installer packaging script and by
    the desktop release workflow, so neither keeps its own copy of the version.
#>
[CmdletBinding()]
param(
    [string] $PropsPath = (Join-Path $PSScriptRoot '..' 'src' 'LauncherVersion.props')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PropsPath -PathType Leaf)) {
    throw "Launcher version file not found: $PropsPath"
}

[xml] $props = Get-Content -LiteralPath $PropsPath -Raw
$group = $props.Project.PropertyGroup
$prefix = ([string] $group.LauncherVersionPrefix).Trim()
$suffix = ([string] $group.LauncherVersionSuffix).Trim()

if ($prefix -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') {
    throw "Invalid LauncherVersionPrefix '$prefix' in $PropsPath. Expected major.minor.patch."
}

if ($suffix -and $suffix -notmatch '^[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*$') {
    throw "Invalid LauncherVersionSuffix '$suffix' in $PropsPath."
}

$full = if ($suffix) { "$prefix-$suffix" } else { $prefix }

[pscustomobject] @{
    Prefix = $prefix
    Suffix = $suffix
    Full   = $full
    Tag    = "desktop-v$full"
}
