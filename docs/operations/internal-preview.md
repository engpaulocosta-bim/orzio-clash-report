# Internal Preview Guide

This guide explains how to operate version `0.1.0-preview.3` on Windows without opening
the source code.

## Status

This is an internal controlled pilot, not a public release, and not a fully validated
longitudinal MVP.

Current source and package candidate version: `0.1.0-preview.3`. Release availability is
determined by the matching Git tag and GitHub prerelease. This document describes the
contents and behavior of version `0.1.0-preview.3` without asserting its current
publication state.

Version history:

- `v0.1.0-preview.2` did not package the identity-governance workflow.
- `v0.1.0-preview.3` does package the identity-governance workflow.

Single-run parsing, grouping, and HTML were human-validated on one private real export.
Longitudinal matching, lifecycle classification, continuity links, continuity paths, and
longitudinal HTML have not been validated on three real historical exports. Matching,
lifecycle, and continuity remain experimental.

The preview does not provide persistent clash identity, Clash Ledger, `Reopened`,
automatic identity propagation, transitivity, graph merge, inferred chronology, or
automatic clash responsibility.

Legal distribution terms remain an owner decision.

## Download and Verify

Download these files from the version `0.1.0-preview.3` artifact bundle or the matching
GitHub prerelease when available:

- `orzio-clash-report-v0.1.0-preview.3-win-x64.zip`
- `orzio-clash-report-v0.1.0-preview.3-win-x64.sha256`

Verify the ZIP checksum in PowerShell:

```powershell
$zip = ".\orzio-clash-report-v0.1.0-preview.3-win-x64.zip"
$expected = (Get-Content ".\orzio-clash-report-v0.1.0-preview.3-win-x64.sha256").Split(" ")[0]
$actual = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -ne $expected) { throw "Checksum mismatch." }
```

Extract the ZIP:

```powershell
Expand-Archive -LiteralPath ".\orzio-clash-report-v0.1.0-preview.3-win-x64.zip" -DestinationPath ".\orzio-preview"
Set-Location ".\orzio-preview"
```

Check the executable:

```powershell
.\orzioclash.exe --version
.\orzioclash.exe --help
```

Expected version output:

```text
orzioclash 0.1.0-preview.3
```

## Recommended Layout

Keep project inputs, snapshots, governance, and reports inside a controlled workspace:

```text
coordination-work\
  project.json
  identity-governance.json
  inputs\
    run-001\
      clash-export.xml
      run-manifest.json
    run-002\
      clash-export.xml
      run-manifest.json
    run-003\
      clash-export.xml
      run-manifest.json
  snapshots\
    run-001.json
    run-002.json
    run-003.json
  reports\
    single-run.html
    longitudinal.html
    project-longitudinal.html
    identity-governance-review.html
  run-index.json
```

Before running the pilot:

- confirm local execution is authorized
- confirm the Navisworks exports are authorized
- review each manifest as a human declaration
- create backups of the XML, manifests, snapshots, run index, project catalog, and
  governance JSON

Do not store private XML, generated HTML, PDFs, screenshots, real paths, project names,
model names, or personal names in Git or shared release artifacts.

## Generate a Single-Run Report

```powershell
.\orzioclash.exe ".\coordination-work\inputs\run-001\clash-export.xml" -o ".\coordination-work\reports\single-run.html"
```

Open the generated HTML locally in a browser. The file is self-contained and does not need
network access.

## Prepare a Schema-Version 2 Manifest

Create one manifest per coordination run. Timestamps, revisions, model identity, and
`executedClashTests` are human declarations.

Use the provided `samples/run-manifest.sample.json` as an anonymized reference. Do not
create a competing manifest schema.

## Create Immutable Snapshots

```powershell
.\orzioclash.exe snapshot `
  --xml ".\coordination-work\inputs\run-001\clash-export.xml" `
  --manifest ".\coordination-work\inputs\run-001\run-manifest.json" `
  -o ".\coordination-work\snapshots\run-001.json"
```

Repeat the same pattern for `run-002` and `run-003`. Snapshot output paths are create-new
only. Existing files are refused.

## Create the Explicit Run Index

Run-index order is authoritative. The program never reorders runs by timestamp, file name,
revision, or `RunId`.

```powershell
.\orzioclash.exe index-snapshots `
  --snapshot ".\coordination-work\snapshots\run-001.json" `
  --snapshot ".\coordination-work\snapshots\run-002.json" `
  --snapshot ".\coordination-work\snapshots\run-003.json" `
  -o ".\coordination-work\run-index.json"
```

The package includes `samples/run-index.template.json` as a minimal template.

## Produce the Longitudinal Output

```powershell
.\orzioclash.exe compare-index `
  --index ".\coordination-work\run-index.json" `
  -o ".\coordination-work\reports\longitudinal.html"
```

The command prints a deterministic longitudinal summary and pairwise adjacent summaries.
The optional HTML is self-contained.

## Create the Project Catalog

```powershell
.\orzioclash.exe create-project `
  --project-id coordination-project `
  --name "Coordination Project" `
  --index ".\coordination-work\run-index.json" `
  --report ".\coordination-work\reports\project-longitudinal.html" `
  -o ".\coordination-work\project.json"
```

The resulting `project.json` remains operational state only. It stores project metadata,
one run-index reference, and one report destination. It does not store snapshots,
matching, lifecycle, continuity, or persistent clash identity.

## Append One Snapshot and Re-render

```powershell
.\orzioclash.exe snapshot `
  --xml ".\coordination-work\inputs\run-004\clash-export.xml" `
  --manifest ".\coordination-work\inputs\run-004\run-manifest.json" `
  -o ".\coordination-work\snapshots\run-004.json"

.\orzioclash.exe append-project-snapshot `
  --project ".\coordination-work\project.json" `
  --snapshot ".\coordination-work\snapshots\run-004.json"

.\orzioclash.exe render-project --project ".\coordination-work\project.json"
```

`append-project-snapshot` updates only the run index. It does not regenerate the project
report automatically.

## Identity Governance Workflow

Create one governance document:

```powershell
.\orzioclash.exe create-identity-governance `
  --project-id coordination-project `
  -o ".\coordination-work\identity-governance.json"
```

Append one explicit confirmation:

```powershell
.\orzioclash.exe append-identity-decision `
  --governance ".\coordination-work\identity-governance.json" `
  --decision-id decision-001 `
  --decision-kind ConfirmSameIdentity `
  --left-run-id run-001 `
  --left-occurrence-index 0 `
  --right-run-id run-002 `
  --right-occurrence-index 0 `
  --persistent-identity-id identity-001 `
  --reviewer-alias coordinator-a `
  --reason "Confirmed from review"
```

Append one explicit rejection:

```powershell
.\orzioclash.exe append-identity-decision `
  --governance ".\coordination-work\identity-governance.json" `
  --decision-id decision-002 `
  --decision-kind RejectSameIdentity `
  --left-run-id run-003 `
  --left-occurrence-index 1 `
  --right-run-id run-004 `
  --right-occurrence-index 1 `
  --reviewer-alias coordinator-a `
  --reason "Different physical clashes"
```

Validate before rendering:

```powershell
.\orzioclash.exe validate-identity-governance `
  --project ".\coordination-work\project.json" `
  --governance ".\coordination-work\identity-governance.json"
```

Render the standalone review report only after validation succeeds:

```powershell
.\orzioclash.exe render-identity-governance-report `
  --project ".\coordination-work\project.json" `
  --governance ".\coordination-work\identity-governance.json" `
  -o ".\coordination-work\reports\identity-governance-review.html"
```

Operational rules:

- confirmations require `persistentIdentityId`
- rejections must not carry `persistentIdentityId`
- validation is read-only
- review rendering is derived and regenerable
- the review report does not project raw `ClashObject.SourceModel`
- outputs remain local

See the dedicated guides:

- [docs/operations/identity-governance-cli.md](identity-governance-cli.md)
- [docs/operations/identity-governance-validation.md](identity-governance-validation.md)
- [docs/operations/identity-governance-review-report.md](identity-governance-review-report.md)

## Smoke Test the Package

The package includes `smoke-release.ps1`. It uses repeated anonymized fixtures as a
packaging smoke test only. It does not constitute real sequential validation.

```powershell
$workspace = ".\smoke-workspace"
New-Item -ItemType Directory -Force -Path $workspace | Out-Null
.\smoke-release.ps1 -ExecutablePath ".\orzioclash.exe" -SmokeWorkspace $workspace
```

## Privacy Precautions

- Keep private validation XML, HTML, PDFs, local paths, project names, model names, and
  personal names outside Git and release artifacts.
- Do not publish real NWD, NWF, NWC, RVT, XML, HTML, PDF, screenshots, or viewpoint
  images.
- Review manifests and governance JSON before sharing them. They may contain project-safe
  aliases that still require authorization.
- The standalone review report intentionally excludes raw `ClashObject.SourceModel`.

## Supported Behavior

- Windows `win-x64` packaged executable.
- Single-run grouped HTML from Clash Detective XML.
- Immutable snapshot creation from XML plus schema-v2 manifest.
- Explicit ordered run-index creation.
- Adjacent comparison over an explicit run index.
- Operational project-catalog creation, append-only snapshot indexing, and report
  regeneration.
- Explicit human identity-governance JSON.
- Read-only evidence validation.
- Standalone deterministic review HTML.

## Unsupported Behavior

- Persistent clash identity.
- Clash Ledger.
- `Reopened`.
- Automatic propagation, transitivity, graph merge, or inferred chronology.
- Automatic clash responsibility.
- Interactive review workflow.
- Database, multi-user workflow, or auth.
- PDF export, embedded clash images, WPF UI, licensing, and live Navisworks API reading.

For a controlled pilot walkthrough and feedback template, see
[docs/operations/pilot-evaluation.md](pilot-evaluation.md).
