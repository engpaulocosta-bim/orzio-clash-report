# Changelog

All notable changes for OrzioClashReport are recorded here.

## Unreleased

### Added

- Source-only Step 29A foundation for explicit human identity governance in Core:
  immutable evidence endpoints, explicit human identity decisions, and project-scoped
  decision documents.
- Strict deterministic schema-v1 JSON adapter
  `OrzioClashReport.Persistence.IdentityGovernanceJson`.
- Source-only Step 29B CLI authoring workflow:
  `create-identity-governance` and `append-identity-decision`.
- Safe replace-existing persistence for identity-governance JSON through
  `IdentityGovernanceFileReplacer`, preserving original bytes on failure and cleaning
  temporary files.
- Source-only Step 29C read-only CLI evidence-validation workflow:
  `validate-identity-governance`, which loads one project catalog, its indexed run
  snapshots, and a governance document, and validates the document's project binding and
  every decision's `runId` + `occurrenceIndex` evidence endpoints against the indexed
  snapshots.
- Source-only Step 30A standalone review workflow:
  `render-identity-governance-report`, which renders one deterministic, self-contained HTML
  review of explicit human identity decisions only after evidence validation succeeds.
- Pure Core Step 30A presentation pipeline:
  `IIdentityGovernanceReviewPresenter` / `DeterministicIdentityGovernanceReviewPresenter`
  plus immutable review presentation types for resolved endpoints and persisted decision
  order.
- Dedicated HTML adapter for Step 30A:
  `IdentityGovernanceReviewHtmlRenderer`, with strict HTML encoding and a standalone visual
  contract that does not modify the existing longitudinal renderer.
- Safe replace/create writer for derived HTML output through `DerivedHtmlReportWriter`,
  preserving the destination on failure and cleaning temporary files.
- Pure Core evidence validator `OrzioClashReport.Core.Governance`:
  `IIdentityGovernanceEvidenceValidator` / `DeterministicIdentityGovernanceEvidenceValidator`,
  with explicitly typed `ProjectIdMismatch`, `DuplicateIndexedRunId`, `RunNotIndexed`, and
  `OccurrenceIndexOutOfRange` issues in deterministic order.
- Unit and contract tests for Step 29A identity-governance validation and serialization.
- Unit and contract tests for Step 29B CLI parsing, append conflicts, and replace-existing
  persistence behavior.
- Unit and contract tests for Step 29C evidence validation and CLI read-only behavior,
  including byte-for-byte proof that no project catalog, run index, snapshot, governance
  file, report destination, or other file is ever created or modified.
- Unit and contract tests for Step 30A Core presentation, standalone HTML rendering, CLI
  parsing, evidence gating, collision handling, safe replacement, LF-only/UTF-8 output,
  and byte-for-byte proof that project catalog, run index, snapshots, governance JSON, and
  longitudinal report remain unchanged.

### Notes

- This Step 29A/29B/29C capability is source-only as of July 28, 2026 and is not part of
  the published `v0.1.0-preview.2` binary contract.
- The Step 29A/29B authoring CLI creates and appends explicit human decisions only. It does
  not validate against snapshots, infer or propagate identity, act as an interactive review
  workflow, project decisions into reports, or introduce a Clash Ledger.
- The Step 29C `validate-identity-governance` command is read-only: it never writes,
  replaces, or creates any file. It validates only project binding and evidence-endpoint
  existence -- never matcher candidacy, run adjacency, left/right ordering intent,
  transitivity across decisions, graph conflicts, identity merges, reopening, decision
  supersession, reviewer identity, timestamps, or responsibility, and it does not project
  decisions into reports or introduce a Clash Ledger.
- The Step 30A `render-identity-governance-report` command is source-only as of July 29,
  2026 and is not part of the published `v0.1.0-preview.2` binary contract. It renders one
  standalone review report only after validation succeeds, does not alter the project
  catalog schema, does not touch the existing longitudinal report, does not group
  decisions by persistent identity, and does not implement transitivity, Clash Ledger, or
  `Reopened`.

## 0.1.0-preview.2

Internal preview for Windows `win-x64`.

### Added

- Strict schema-v1 operational project catalog JSON adapter.
- `create-project` and `render-project` CLI commands for regenerable longitudinal project
  workflows built from immutable snapshots and an explicit run index.
- `append-project-snapshot` for explicit append-only updates to an existing project catalog
  run index, using a safe in-place run-index replacement workflow.
- Expanded packaged Windows smoke coverage for `create-project`,
  `append-project-snapshot`, and `render-project`, using repeated anonymized fixtures.
- `docs/operations/project-catalog.md` in the release package.

### Validation Status

- Single-run parsing, grouping, and HTML presentation were human-validated on one private
  real export.
- Longitudinal matching, lifecycle classification, continuity links, continuity paths, and
  longitudinal HTML have not been validated against three real historical exports.
- The expanded packaging smoke uses repeated anonymized fixtures only and does not
  constitute real longitudinal validation.
- Longitudinal behavior remains experimental.

### Known Limitations

- No persistent clash identity.
- No Clash Ledger.
- No `Reopened` lifecycle state.
- No aggregate multi-run lifecycle.
- No automatic chronology.
- No automatic clash responsibility.
- No PDF export, embedded clash images, WPF UI, licensing, or live Navisworks API adapter.
- Private validation XML, HTML, PDF, paths, project names, model names, and personal names
  are not part of the repository or release artifacts.

## 0.1.0-preview.1

Internal preview release for Windows `win-x64`.

### Added

- Single-run Clash Detective XML parsing and grouped self-contained HTML reporting.
- Immutable coordination-run snapshots.
- Strict schema-v2 run manifests with explicit executed clash test coverage.
- Explicit ordered run-index JSON.
- Snapshot-to-snapshot and run-index adjacent comparison commands.
- Longitudinal stdout summary and self-contained longitudinal HTML.
- `orzioclash --help` and `orzioclash --version`.
- Windows release smoke script for packaged binaries.
- CI packaging smoke for the `win-x64` self-contained single-file executable.
- Release workflow for internal preview packaging and future tag-triggered prereleases.

### Validation Status

- Single-run parsing, grouping, and HTML presentation were human-validated on one private
  real export.
- Longitudinal matching, lifecycle classification, continuity links, continuity paths, and
  longitudinal HTML have not been validated against three real historical exports.
- Longitudinal behavior remains experimental.

### Known Limitations

- No persistent clash identity.
- No Clash Ledger.
- No `Reopened` lifecycle state.
- No aggregate multi-run lifecycle.
- No automatic chronology.
- No automatic clash responsibility.
- No PDF export, embedded clash images, WPF UI, licensing, or live Navisworks API adapter.
- Private validation XML, HTML, PDF, paths, project names, model names, and personal names
  are not part of the repository or release artifacts.
