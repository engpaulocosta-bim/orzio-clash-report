# Changelog

All notable changes for OrzioClashReport are recorded here.

## Unreleased

## 0.2.0-launcher-preview.1

Private pilot candidate for `Orzio Clash Report Desktop`, a Windows desktop application
built on the unchanged `0.1.0-preview.3` engine.

### Added

- Four launcher projects on `net8.0` -- Contracts, Application, Infrastructure, and an
  Avalonia Desktop shell -- alongside the unchanged `netstandard2.0` engine. No launcher
  project references an engine project, and no engine project references the launcher.
- An application shell with seven sections: Início, Relatório rápido, Snapshots,
  Longitudinal, Projetos, Governança, and Definições. There is no basic or advanced mode.
- A design token file that is the only place a colour, radius, spacing, or type size is
  defined, with the specified light and dark palettes, and a test that fails any view
  stating such a literal.
- An engine status badge covering all six engine states, each with its own glyph and
  wording so no state is distinguishable by colour alone. Engine identity comes from
  running the engine with `--version` as its single argument, matched against the published
  version line, after a SHA-256 check against the packaged engine manifest.
- Quick report end to end without a terminal: choose an export, choose a destination,
  generate, follow the engine's output, cancel, and open the result.
- Typed forms for `snapshot`, `compare`, `compare-snapshots`, `index-snapshots`,
  `compare-index`, `create-project`, `append-project-snapshot`, and `render-project`. No
  screen assembles a command line; each form produces a typed request and one gateway turns
  it into the engine's published argument vector.
- Typed forms for `create-identity-governance`, `append-identity-decision`,
  `validate-identity-governance`, and `render-identity-governance-report`.
- Output collision handling for derived HTML only: an existing report prompts a human, and
  replacing is never the default. Snapshots, run indexes, project catalogs, and governance
  documents are never offered for replacement, because the engine persists them with
  create-new semantics.
- A job journal, so an operation interrupted by the application closing is reported at the
  next start and never resumed automatically.
- A redacted JSON Lines log, kept fourteen days and at most twenty files, pruned on start.
  A destination is recorded as an extension, a SHA-256 of the path, and the kind of root it
  lives under; the absolute path, the directory chain, the argument vector, and the
  engine's own output are never written.
- A diagnostic bundle produced only on an explicit action, containing exactly six named
  files, after the human has seen the complete list and the exact redacted log.
- A Portuguese and English interface, following the operating system by default and switchable
  at any time under Definições / Settings, applied immediately without reopening. Every visible
  string comes from one table carrying both translations; the language changes visible text only
  and never a value the engine receives, so `ConfirmSameIdentity` and `RejectSameIdentity` are
  written exactly like that in both languages. Failures are shown from the launcher's own error
  code rather than from engine text, so they read in the chosen language while the engine's own
  words stay untranslated beneath them.
- A per-user Windows installer requiring no administrator rights, in Portuguese and English,
  with publish, package, and smoke scripts, and a SHA-256 for the installer.
- `docs/operations/launcher-pilot.md` with the pilot procedure and the evaluation
  questionnaire.

### Validation Status

- The engine is unchanged at `0.1.0-preview.3`; its validation status is unchanged with it.
- Single-run parsing, grouping, and HTML remain human-validated on one private real export.
- Longitudinal matching, lifecycle classification, continuity links, continuity paths, and
  the longitudinal report have still not been validated against
  three real historical exports, and the application labels those operations experimental.
- The desktop application's operations are covered by automated tests, including tests that
  drive a real child process through the argument vectors the launcher actually builds.
  That is not validation on a real project; the private pilot is how that claim gets earned.
- The desktop application has not yet been run by a human on a clean Windows machine.

### Known Limitations

- The installer is not code signed. SmartScreen warns on first run.
- Windows only. There is no macOS package.
- No PDF export, no embedded clash images, no report templating, no branding, and no
  manifest builder: a run manifest is still authored outside the application.
- No auto-update, no licensing, no account, no cloud, and no telemetry.
- One operation runs per window; there is no queue.
- The application reaches the engine as a child process. That is deliberate for this pilot
  and is not treated as urgent debt.
- No Clash Ledger, no `Reopened`, no automatic identity assignment, propagation,
  transitivity, graph merge, chronology, or responsibility.
- Legal distribution terms remain an owner decision.

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
