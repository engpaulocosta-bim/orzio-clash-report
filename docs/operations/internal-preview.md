# Internal Preview Guide

This guide explains how to operate the `0.1.0-preview.1` internal preview on Windows
without opening the source code.

## Status

This is an internal preview, not a fully validated longitudinal MVP.

Single-run parsing, grouping, and HTML were human-validated on one private real export.
Longitudinal matching, lifecycle classification, continuity links, continuity paths, and
longitudinal HTML have not been validated on three real historical exports. Longitudinal
behavior remains experimental.

The preview does not provide persistent clash identity, Clash Ledger, `Reopened`,
aggregate multi-run lifecycle, automatic chronology, or automatic clash responsibility.

## Download and Verify

Download these files from the internal preview artifact or prerelease:

- `orzio-clash-report-v0.1.0-preview.1-win-x64.zip`
- `orzio-clash-report-v0.1.0-preview.1-win-x64.sha256`

Verify the ZIP checksum in PowerShell:

```powershell
$zip = ".\orzio-clash-report-v0.1.0-preview.1-win-x64.zip"
$expected = (Get-Content ".\orzio-clash-report-v0.1.0-preview.1-win-x64.sha256").Split(" ")[0]
$actual = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -ne $expected) { throw "Checksum mismatch." }
```

Extract the ZIP:

```powershell
Expand-Archive -LiteralPath ".\orzio-clash-report-v0.1.0-preview.1-win-x64.zip" -DestinationPath ".\orzio-preview"
Set-Location ".\orzio-preview"
```

Check the executable:

```powershell
.\orzioclash.exe --version
.\orzioclash.exe --help
```

Expected version output:

```text
orzioclash 0.1.0-preview.1
```

## Recommended Layout

Keep project inputs, snapshots, and reports in a controlled workspace:

```text
coordination-work\
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
  run-index.json
```

Do not store private XML, generated HTML, PDFs, real paths, project names, model names, or
personal names in Git or shared release artifacts.

For longitudinal work, prepare one XML export and one schema-version 2 manifest for each
coordination run. The three exports must belong to the same project. The intended sequence
`R1 -> R2 -> R3` is a human declaration represented by run-index order. The program does
not infer chronology, discipline, revision, or clash-test renames from timestamps, file
names, model names, or XML contents.

## Navisworks XML Preparation

Export Clash Detective results as XML from Navisworks. Confirm that the export represents
the intended coordination run and that model source names are present in the exported
object data. The current assembler resolves source models only by exact trimmed
case-insensitive equality against declared manifest `sourceFileName` or `sourceFilePath`.
It does not infer revisions or disciplines from file names.

The `sourceFileName` or `sourceFilePath` declared for each model revision must match the
source model token observed in the XML. Clash test names should remain coherent across
runs when they represent the same test. If a test was renamed by humans, the preview will
not infer that relationship automatically.

## Generate a Single-Run Report

```powershell
.\orzioclash.exe ".\coordination-work\inputs\run-001\clash-export.xml" -o ".\coordination-work\reports\single-run.html"
```

Open the generated HTML locally in a browser. The file is self-contained and does not need
network access.

## Prepare a Schema-Version 2 Manifest

Create one manifest per coordination run. Timestamps, revisions, model identity, and
`executedClashTests` are human declarations. The manifest declares the exact model
revisions and the clash tests that were executed.

```json
{
  "schemaVersion": 2,
  "runId": "coordination-run-001",
  "createdAt": "2026-07-21T09:00:00+01:00",
  "models": [
    {
      "company": "Example",
      "discipline": "Structure",
      "modelName": "Structure_Main",
      "revision": "R01",
      "sourceFileName": "Structure_Main_R01.nwc"
    }
  ],
  "executedClashTests": [
    {
      "name": "Structure self clash",
      "modelA": { "company": "Example", "discipline": "Structure", "modelName": "Structure_Main" },
      "modelB": { "company": "Example", "discipline": "Structure", "modelName": "Structure_Main" }
    }
  ]
}
```

Use the provided `samples/run-manifest.sample.json` as an anonymized reference. Do not
create a competing manifest schema.

## Create Immutable Snapshots

Create one snapshot for each XML/manifest pair:

```powershell
.\orzioclash.exe snapshot `
  --xml ".\coordination-work\inputs\run-001\clash-export.xml" `
  --manifest ".\coordination-work\inputs\run-001\run-manifest.json" `
  -o ".\coordination-work\snapshots\run-001.json"

.\orzioclash.exe snapshot `
  --xml ".\coordination-work\inputs\run-002\clash-export.xml" `
  --manifest ".\coordination-work\inputs\run-002\run-manifest.json" `
  -o ".\coordination-work\snapshots\run-002.json"

.\orzioclash.exe snapshot `
  --xml ".\coordination-work\inputs\run-003\clash-export.xml" `
  --manifest ".\coordination-work\inputs\run-003\run-manifest.json" `
  -o ".\coordination-work\snapshots\run-003.json"
```

Snapshot output paths are create-new only. Existing files are refused.

## Create an Explicit Run Index

Run-index order is authoritative. The tool never reorders runs by timestamp, file name,
revision, or `RunId`.

Declare the intended `R1 -> R2 -> R3` order explicitly:

```powershell
.\orzioclash.exe index-snapshots `
  --snapshot ".\coordination-work\snapshots\run-001.json" `
  --snapshot ".\coordination-work\snapshots\run-002.json" `
  --snapshot ".\coordination-work\snapshots\run-003.json" `
  -o ".\coordination-work\run-index.json"
```

The package includes `samples/run-index.template.json` as a minimal template.

## Produce Longitudinal Output

```powershell
.\orzioclash.exe compare-index `
  --index ".\coordination-work\run-index.json" `
  -o ".\coordination-work\reports\longitudinal.html"
```

The command prints a deterministic longitudinal summary and pairwise adjacent summaries.
The optional HTML is self-contained.

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
- Do not publish real NWD, NWF, NWC, RVT, XML, HTML, PDF, screenshots, or viewpoint images.
- Review manifests before sharing them. They may contain project-specific model labels.
- Treat source clash GUIDs as evidence only, not persistent identity.

## Troubleshooting

- **Input file not found**: verify the XML path and quote paths containing spaces.
- **Snapshot manifest file not found**: verify the manifest path.
- **No manifest model matched SourceFileName or SourceFilePath**: align
  `sourceFileName` or `sourceFilePath` with the exact source model token exported by
  Navisworks.
- **Coordination run snapshot file already exists**: choose a new snapshot path.
- **Run index must contain at least two snapshot references**: create an index with two or
  more snapshots.
- **Longitudinal result looks odd**: confirm index order. The index order is the sequence
  authority.

## Supported Behavior

- Windows `win-x64` packaged executable.
- Single-run grouped HTML from Clash Detective XML.
- Immutable snapshot creation from XML plus schema-v2 manifest.
- Explicit ordered run-index creation.
- Adjacent comparison over an explicit run index.
- Self-contained lifecycle and longitudinal HTML.

## Unsupported Behavior

- Persistent clash identity.
- Clash Ledger.
- `Reopened`.
- Aggregate multi-run lifecycle.
- Automatic chronology.
- Automatic clash responsibility.
- Responsibility inference from Autodesk sign-in, creator, owner, or last-changed fields.
- PDF export, embedded clash images, WPF UI, licensing, and live Navisworks API reading.
