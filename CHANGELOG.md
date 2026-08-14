# Changelog

All notable changes for OrzioClashReport are recorded here.

## Unreleased

## 0.2.0-launcher-preview.1

First installable Windows desktop application. The engine is unchanged: this release adds a
graphical launcher around the existing CLI contracts and does not alter, weaken, or bypass any
of them.

### Added

- Four launcher projects on `net8.0`: `OrzioClashReport.Launcher.Contracts` (dependency-free
  DTOs and ports), `.Application` (pure launcher policy), `.Infrastructure` (process, filesystem
  and hashing adapters) and `.Desktop` (Avalonia UI with a manual composition root and no
  dependency-injection container).
- An application shell with exactly seven sections — Início, Relatório rápido, Snapshots,
  Longitudinal, Projetos, Governança, Definições — and a token-based design system in which no
  view carries a literal colour, font size, radius or spacing value.
- Engine verification before use: the executable is located at its packaged path, hashed against
  `engine-manifest.json`, and only then run with `--version` as its single argument under a
  five-second timeout. Six engine states are each shown with their own glyph and label, never by
  colour alone.
- Quick report end to end without a terminal: choose an export, choose a destination, generate,
  follow the engine's output, cancel, and open the result.
- Typed forms for `snapshot`, `compare`, `compare-snapshots`, `index-snapshots`, `compare-index`,
  `create-project`, `append-project-snapshot` and `render-project`, each with one argument-vector
  test compared element by element.
- Typed forms for `create-identity-governance`, `append-identity-decision`,
  `validate-identity-governance` and `render-identity-governance-report`.
- A job journal under `%LOCALAPPDATA%\Orzio\ClashReportLauncher\jobs`, an interrupted-operation
  notice at startup, redacted JSON Lines logs with fourteen-day and twenty-file retention, and an
  explicitly requested diagnostic bundle limited to a closed six-entry allow-list.
- A Windows installer, plus publish, package and smoke scripts. Per-user installation without
  administrator rights; machine-wide only as an explicit fallback for AppLocker environments.

### Contracts preserved

- Core remains `netstandard2.0`. No engine project references the launcher, and an architecture
  test fails the build if one ever does.
- The CLI remains available and its published stdout, stderr and exit-code contracts are unchanged.
- The launcher never assembles a command line. Arguments are passed element by element, with no
  shell intermediary, and every `-o` destination is absolute.
- Run order remains an explicit human declaration. Nothing is sorted by date, name or revision, and
  a repeated entry is preserved and reported rather than removed.
- Persistent clash identity exists only through an explicit human `ConfirmSameIdentity` decision
  carrying a `persistentIdentityId`. A rejection can never carry one. An algorithmic `High`
  confidence is never presented as a human confirmation.
- Snapshots, run indexes, project catalogs and governance documents keep their create-new
  semantics: the launcher refuses to replace one and only ever offers a different name. A derived
  HTML report may be replaced, but never by default and never without a decision naming the file.

### Known limitations

- The installer is not code signed, and this is stated rather than worked around.
- The launcher drives the engine as a subprocess. This is a deliberate choice for this phase.
- The installer has not yet been built or run on Windows; the ten-step smoke script exists and is
  pending a clean-machine run by an evaluator.
- Longitudinal matching, lifecycle, continuity links, continuity paths and longitudinal HTML remain
  experimental and unvalidated against three real historical exports.
- No macOS package, no PDF, no auto-update, no licensing, no cloud, and no telemetry.

## 0.1.0-preview.3

Internal controlled pilot candidate for Windows `win-x64`.

### Added

- Step 29A foundation for explicit human identity governance in Core: immutable evidence
  endpoints, explicit human identity decisions, and project-scoped decision documents.
- Strict deterministic schema-v1 JSON adapter
  `OrzioClashReport.Persistence.IdentityGovernanceJson`.
- Step 29B CLI authoring workflow: `create-identity-governance` and
  `append-identity-decision`.
- Safe replace-existing persistence for identity-governance JSON through
  `IdentityGovernanceFileReplacer`, preserving original bytes on failure and cleaning
  temporary files.
- Step 29C read-only CLI evidence-validation workflow:
  `validate-identity-governance`, which loads one project catalog, its indexed run
  snapshots, and a governance document, and validates the document's project binding and
  every decision's `runId` + `occurrenceIndex` evidence endpoints against the indexed
  snapshots.
- Step 30A standalone review workflow: `render-identity-governance-report`, which renders
  one deterministic, self-contained HTML review of explicit human identity decisions only
  after evidence validation succeeds.
- Pure Core Step 30A presentation pipeline:
  `IIdentityGovernanceReviewPresenter` / `DeterministicIdentityGovernanceReviewPresenter`
  plus immutable review presentation types for resolved endpoints and persisted decision
  order.
- Dedicated HTML adapter for Step 30A: `IdentityGovernanceReviewHtmlRenderer`, with strict
  HTML encoding and a standalone visual contract that does not modify the existing
  longitudinal renderer.
- Safe replace/create writer for derived HTML output through `DerivedHtmlReportWriter`,
  preserving the destination on failure and cleaning temporary files.
- Pure Core evidence validator `OrzioClashReport.Core.Governance`:
  `IIdentityGovernanceEvidenceValidator` / `DeterministicIdentityGovernanceEvidenceValidator`,
  with explicitly typed `ProjectIdMismatch`, `DuplicateIndexedRunId`, `RunNotIndexed`, and
  `OccurrenceIndexOutOfRange` issues in deterministic order.
- Packaged Windows internal controlled pilot coverage for the project-catalog and
  identity-governance workflows through `smoke-release.ps1`, using repeated anonymized
  fixtures only.
- Packaged operational guides for the identity-governance workflow and the controlled
  pilot evaluation procedure.

### Validation Status

- Single-run parsing, grouping, and HTML presentation were human-validated on one private
  real export.
- Longitudinal matching, lifecycle classification, continuity links, continuity paths, and
  longitudinal HTML have not been validated against three real historical exports.
- Packaging smoke uses repeated anonymized fixtures only and does not constitute real
  longitudinal validation.
- Matching, lifecycle, and continuity remain experimental.

### Known Limitations

- `v0.1.0-preview.2` did not package the identity-governance workflow.
- `v0.1.0-preview.3` packages the identity-governance workflow only for an internal
  controlled pilot.
- The Step 29A/29B authoring CLI creates and appends explicit human decisions only. It
  does not validate against snapshots, infer or propagate identity, act as an interactive
  review workflow, project decisions into reports, or introduce a Clash Ledger.
- The Step 29C `validate-identity-governance` command is read-only: it never writes,
  replaces, or creates any file. It validates only project binding and evidence-endpoint
  existence, never matcher candidacy, run adjacency, left/right ordering intent,
  transitivity across decisions, graph conflicts, identity merges, reopening, decision
  supersession, reviewer identity, timestamps, or responsibility.
- The Step 30A `render-identity-governance-report` command renders one standalone review
  report only after validation succeeds, does not alter the project catalog schema, does
  not touch the existing longitudinal report, does not group decisions by persistent
  identity, does not project raw `ClashObject.SourceModel`, and does not implement
  transitivity, Clash Ledger, or `Reopened`.
- Persistent clash identity exists only through an explicit human
  `ConfirmSameIdentity` decision carrying a `persistentIdentityId`.
- No Clash Ledger.
- No `Reopened` lifecycle state.
- No automatic identity assignment, propagation, transitivity, graph merge, project-wide
  identity graph, longitudinal identity integration, chronology, or clash responsibility.
- No interactive review workflow, database, multi-user workflow, or auth.
- Legal distribution terms remain an owner decision.

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
