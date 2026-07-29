# Project Catalog Workflow

This workflow is included in versions `0.1.0-preview.2` and `0.1.0-preview.3`. This
document describes the operational behavior packaged for version `0.1.0-preview.3`
without asserting its current publication state.

## Purpose

The project catalog is operational state, not evidence. It stores:

- a human `projectId`
- a human `displayName`
- one explicit run-index reference
- one longitudinal HTML destination

It does not store snapshots, matching, lifecycle, continuity, persistent clash identity,
Clash Ledger data, `Reopened`, or chronology.

## Recommended Layout

Keep the project catalog, run index, snapshots, governance JSON, and reports inside one
movable folder tree:

```text
coordination-project/
  project.json
  identity-governance.json
  run-index.json
  snapshots/
    run-001.json
    run-002.json
    run-003.json
  reports/
    longitudinal.html
    identity-governance-review.html
```

The project catalog stores canonical relative references with `/` separators, so the whole
folder can be moved to another root without rewriting the JSON.

## Create the Run Index

Create immutable run snapshots first, then build the explicit ordered run index:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  index-snapshots \
  --snapshot snapshots/run-001.json \
  --snapshot snapshots/run-002.json \
  --snapshot snapshots/run-003.json \
  -o run-index.json
```

The order of `--snapshot` arguments is the only sequence authority.

## Create the Project Catalog

Create the project catalog from the validated run index:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  create-project \
  --project-id example-project \
  --name "Example Coordination Project" \
  --index run-index.json \
  --report reports/longitudinal.html \
  -o project.json
```

Operational notes:

- `create-project` validates the run index and loads every referenced snapshot before it
  writes `project.json`.
- A project catalog workflow requires its run index, all resolved snapshots, and report
  destination to stay inside the project catalog directory tree.
- The report destination must never resolve to the project catalog, the run index, or any
  snapshot.
- The report file does not need to exist yet.
- The report parent directory must already exist.
- The catalog file is create-new only and is never overwritten in place.

## Render the Project

Recalculate the full longitudinal result from immutable evidence and rewrite the HTML:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  render-project \
  --project project.json
```

`render-project` resolves the run index and report path relative to `project.json`,
reloads all snapshots, reruns the same derived longitudinal pipeline used by
`compare-index`, and rewrites the report destination.

## Append One Snapshot to the Project

Append one persisted snapshot explicitly to the end of the existing project run index:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  append-project-snapshot \
  --project project.json \
  --snapshot snapshots/run-004.json
```

Operational notes:

- `append-project-snapshot` loads the project catalog, resolves the existing run index,
  loads every already-indexed snapshot, validates the new snapshot, and only then replaces
  the run-index file in place.
- The command preserves every existing run-index reference exactly as loaded and appends
  one new reference at the end.
- Duplicate references remain allowed.
- The project catalog file remains unchanged.
- Existing snapshots and the appended snapshot remain immutable evidence.
- The report is not regenerated automatically. Run `render-project` separately after a
  successful append when you want refreshed HTML.
- The same workspace rule still applies: the run index, all existing snapshots, the new
  snapshot, and the report destination must stay inside the project catalog directory tree.

Recommended operational sequence:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  snapshot \
  --xml inputs/run-004.xml \
  --manifest manifests/run-004.json \
  -o snapshots/run-004.json

dotnet run --project src/OrzioClashReport.Cli -- \
  append-project-snapshot \
  --project project.json \
  --snapshot snapshots/run-004.json

dotnet run --project src/OrzioClashReport.Cli -- \
  render-project \
  --project project.json
```

## Relationship to Identity Governance

The project catalog remains minimal operational state even in `v0.1.0-preview.3`.
Identity-governance files are separate explicit inputs:

- `identity-governance.json` is not stored in the project catalog
- `render-identity-governance-report` does not add a review path to the project catalog
- `validate-identity-governance` and `render-identity-governance-report` reuse the project
  catalog only to resolve the authoritative run index and immutable snapshots

## Privacy

Do not commit private validation exports, private project names, local filesystem details,
or personal information. Use safe aliases only.

## Limitations

- No persistent clash identity
- No Clash Ledger
- No `Reopened`
- No database
- No automatic chronology
- No non-adjacent or all-vs-all comparison
- No persisted matching, lifecycle, continuity links, continuity paths, or presentation
