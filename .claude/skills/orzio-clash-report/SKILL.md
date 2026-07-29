---
name: orzio-clash-report
description: >
  Mandatory architecture guardrail for OrzioClashReport, the Navisworks clash-to-HTML
  coordination report tool (C#, .NET). Trigger whenever writing, refactoring, reviewing,
  debugging or packaging any part of this tool: the domain model, the Navisworks Clash
  Detective XML parser, the rule-based clash grouper, the HTML report renderer, the CLI,
  the tests, or the future Navisworks .NET API adapter and WPF/dockable-panel UI. Trigger
  even if the user only says "add a field", "fix the parser", "change the report", "group
  the clashes" or "read from the API". Enforces Ports-and-Adapters layering, netstandard2.0
  core with zero third-party deps, immutable source-agnostic domain types, defensive
  parsing with no invented XML/API members, deterministic rendering, no silent catch, and
  honest validation where compiles, runs, and validated-on-a-real-model are three distinct
  claims. Does NOT cover Revit add-in work (use revit-addin-dev) or Orzio Budget Supabase
  work (use the orzio-* SaaS skills).
---

# OrzioClashReport — architecture law

This tool reads Autodesk Navisworks Clash Detective results and produces a clean, grouped
HTML coordination report. The value is not detection (Navisworks detects). The value is
**honest grouping**: hundreds of raw clashes collapse to tens of real problems. Treat this
as a production product with a paying-user and portfolio purpose, not a script.

Two facts pin the whole design:
- MVP input is the Navisworks **Clash XML export**. Nothing else in the MVP.
- The near-certain next step reads clashes live from the Navisworks **.NET API**, which is
  **.NET Framework 4.8 only** and compatible within a single Navisworks major version
  (2024/2025/2026 need separate binaries). The core must absorb that later with zero change.

## The twelve laws

1. **Ports and Adapters. Dependencies point inward only.** `Core` knows nothing about XML,
   HTML, or Navisworks. Adapters depend on `Core`, never on each other.
2. **Core targets `netstandard2.0`.** Non-negotiable. It must be consumable by both the
   `net8.0` CLI today and a `net48` Navisworks-API adapter tomorrow. Never retarget the
   core to `net8.0`.
3. **Core has zero third-party dependencies.** No Newtonsoft, no logging framework. XML
   parsing lives in the XML adapter using `System.Xml.Linq`, never in Core.
4. **Domain types are immutable and source-agnostic.** `ClashReportDocument`, `ClashBatch`,
   `ClashResult`, `ClashObject`, `ClashPoint`, `ClashStatus`, `GroupedClashReport`,
   `ClashGroup`. No public mutable setters after construction. A `ClashResult` must be
   identical whether it came from XML or from the live API.
5. **No invented members.** Never reference an XML element/attribute or a Navisworks API
   member you have not seen in a fixture or verified in docs. Unseen fields are optional.
6. **Defensive parsing.** Missing optional field becomes null. Unrecognized status maps to
   `ClashStatus.Unknown` and is logged. Malformed input fails loudly with a clear message,
   it never produces a silently-wrong report.
7. **No silent catch.** Catch, log with context, then decide: skip this element or fail the
   run. Swallowing an exception is a defect.
8. **Deterministic rendering.** Same input produces byte-identical HTML. No `DateTime.Now`
   in the body, no unordered dictionaries, stable sort keys. This is what makes golden-file
   tests possible.
9. **The grouper is the differentiator, and it is rule-based.** Collapse exact duplicates
   (same element-id pair, same point within tolerance), then group by discipline-pair plus
   level. No LLM/AI grouping in the MVP: it adds cost and non-determinism for no gain.
10. **Discipline resolution is pluggable.** Naming conventions vary per project, so element
    to discipline mapping goes behind `IDisciplineResolver`, never hardcoded in the grouper
    or renderer.
11. **No business logic in UI or CLI.** `Program.cs` and any future WPF/dockable panel only
    wire `IClashSource -> IClashGrouper -> IReportRenderer`. All logic lives in Core.
12. **Reserve, do not build, the future.** The Navisworks-API adapter, PDF output, WPF UI,
    and image enrichment each get a reserved interface and a `// FUTURE:` seam comment, and
    nothing more, until the MVP is validated on a real model.

## Scope gate (reject creep)

In scope for the MVP: XML parsing, rule-based grouping, deterministic light-theme HTML with
per-discipline color accents, CLI, tests.

Out of scope until explicitly reopened, log any request to a README backlog instead of
building it: clash images/viewpoint capture, PDF, licensing/keys/payment, WPF or any GUI,
the live Navisworks-API adapter, status editing, responsible-party assignment, comments,
history, database, CDE/BIM 360 integration, LLM grouping, DI container, config framework.
Image support, when it eventually lands, reuses Navisworks-exported viewpoint images; it
does not generate images from scratch.

## Honest validation framework

Three different claims. Never conflate them, and never report a higher one than you verified.

- **Compiles**: `dotnet build` succeeds.
- **Runs**: `dotnet test` is green AND the CLI produced an HTML file from the sample fixture.
- **Validated on a real model**: a human ran it on a real, anonymized Navisworks export and
  confirmed the grouped report matches reality.

An agent can claim the first two. Only the human can claim the third. When reporting done,
state exactly what was verified and what remains unverified.

## Internal preview release contract

Current source and package version: `0.1.0-preview.2`, distributed as `orzioclash.exe` for
Windows `win-x64`. Release availability is determined by the matching Git tag and GitHub
prerelease. This repository documentation describes version `0.1.0-preview.2` without
asserting its current publication state. Release tags must point to commits contained in
`master`.

README and user-facing documentation remain English. Published CLI stdout/stderr contracts
must not be silently broken after release.

No real/private artifacts enter Git or release packages: validation XML, HTML, PDF, local
paths, project names, model names, personal names, NWD, NWF, NWC, RVT, and images stay out.
Private validation stays documented only through public aliases and scoped claims.

Validation status is intentionally narrow: single-run parsing, grouping, and HTML were
human-validated on one private real export. Longitudinal matching, lifecycle, continuity
links, continuity paths, and longitudinal HTML remain unvalidated against three real
historical exports and are experimental. Release/package smoke may use repeated anonymized
fixtures for `--version`, `--help`, single-run HTML, three snapshots, run-index creation,
`compare-index`, `create-project`, `append-project-snapshot`, and `render-project`, but
that smoke is packaging coverage only and is not real longitudinal validation.

The preview does not provide persistent clash identity, Clash Ledger, `Reopened`,
aggregate multi-run lifecycle, automatic chronology, or automatic clash responsibility.
`workflow_dispatch` release runs are packaging dry runs only. Responsibility/authorship
remains deferred to a later human-governance stage.

## Source-only human identity governance

Steps 29A, 29B, 29C, and 30A add a source-only workflow for explicit human identity
governance. It is not part of the published `v0.1.0-preview.2` binary contract.

- Snapshots remain immutable evidence only.
- Algorithmic matching remains suggestion only, never persisted truth.
- Persistent identity exists only when a human creates `ConfirmSameIdentity`.
- `RejectSameIdentity` must never carry a persistent identity id.
- Absence of a decision is only absence of a decision; there is no persisted pending state.
- Evidence endpoints reuse `runId` plus immutable `occurrenceIndex` inside a snapshot.
- `create-identity-governance` may create one empty governance document only.
- `append-identity-decision` may append one explicit human decision only, preserving all
  previous decisions and replacing the existing file only after a complete temporary write
  succeeds.
- The authoring commands (`create-identity-governance`, `append-identity-decision`) never
  validate against snapshots and never load project catalogs or run indexes for identity
  semantics.
- `validate-identity-governance` (Step 29C) is the one command that does load a project
  catalog, its run index, and its indexed snapshots -- but only to read-only validate
  project binding and evidence-endpoint existence. It never writes, replaces, or creates
  any file.
- `render-identity-governance-report` (Step 30A) is the one command that renders one
  standalone, regenerable HTML review of explicit human decisions -- but only after Step
  29C evidence validation succeeds, and without mutating the project catalog, the
  governance file, any snapshot, or the existing longitudinal report.
- No command infers or propagates identity, and none is an interactive review workflow.
- No report projection, no Clash Ledger, no `Reopened`, no automatic propagation, no
  automatic transitivity, no automatic chronology, and no automatic responsibility exist at
  this stage.

## Model identity vs. revision

`ModelIdentity` is stable and revision-free. `ModelRevision` composes `ModelIdentity` plus
revision-specific metadata. Revision, file path, content hash, run id, and timestamp must
never enter `ModelIdentity` equality or `StableKey`. No automatic revision extraction in the
MVP.

## Run manifest

The run manifest is an explicit input contract. Do not infer revisions from filenames,
paths, XML, Forma, or ACC. `schemaVersion` belongs to the JSON adapter, not the Core. A
`RunManifest` contains at most one `ModelRevision` per stable `ModelIdentity`.

## Coordination run snapshot

`ClashOccurrence` is run-specific evidence, not cross-run identity. It preserves source A/B
order and references exact `ModelRevision` values. `CoordinationRun` is an immutable
snapshot of `RunManifest` plus ordered occurrences. Every occurrence revision must be
declared exactly in the manifest. Every occurrence must also correspond to a declared
executed test and model pair -- see "Explicit executed clash test coverage" below. Do not
create fingerprints, matching, deduplication, or source-model inference in these types.

## Explicit executed clash test coverage

`RunManifest` explicitly declares `ExecutedClashTests`. Each declaration contains a test
name and an ordered pair of revision-free `ModelIdentity` values. Coverage lookup is
case-insensitive by test name and unordered by model pair, while the declaration preserves
A/B order. Every `ClashOccurrence` in a `CoordinationRun` must correspond to a declared
executed test and model pair. An executed test may have zero occurrences -- that is how a
run proves it executed a test and got zero results, rather than never running it at all.
Lifecycle test coverage must come only from the explicit manifest declaration, never from
observed occurrences. Manifest JSON `schemaVersion` 2 is required; `schemaVersion` 1 is
intentionally rejected: it never declared executed-test coverage, and silently migrating it
to an empty `executedClashTests` list would conflate "no test ran" with "we don't know what
ran."

## Matching vocabulary

Match contracts describe a candidate relationship; they do not prove identity.
`ClashMatchConfidence` has `Low`, `Medium`, and `High` only. `High` is not `Exact` or
human-confirmed. `MatchEvidence` records `Supports`, `Contradicts`, or `Unavailable`
signals. Source clash GUID is evidence only until stability is validated on sequential real
exports. Do not compute scores, thresholds, fingerprints, lifecycle statuses, or automatic
actions in the match contract types.

## Pairwise clash matcher port

`IClashMatcher` is a pairwise Core port. It assesses one ordered previous/current
`ClashOccurrence` pair. A non-null result is a candidate `ClashMatchAssessment`; `null` means
no candidate assessment. `Low` is not the same as `null`, and `Unavailable` is an evidence
verdict, not a matcher result. The matcher must preserve the exact input occurrence
references. Run traversal, one-to-one conflict resolution, lifecycle classification, and
verifiability guards belong to a future run comparer.

## Conservative clash matcher

`ConservativeClashMatcher` requires matching clash-test name, revision-free model pair, and
model-aligned opaque element-id pair. Model revisions and file metadata never participate in
matching. The matcher accepts direct or swapped A/B model alignment and handles self-clash
element pairs without mutating occurrences. Source clash GUID is supplemental evidence only:
equal GUID can raise `Medium` to `High`, but unequal or missing GUID does not create or
destroy a candidate. Element identifiers and source GUIDs are compared ordinally and
case-sensitively. The matcher does not use spatial position, metadata, scores, fingerprints,
or lifecycle status.

## Deterministic run comparer

`IClashRunComparer` compares explicit previous/current `CoordinationRun` roles and never
infers order from timestamps or run ids. `DeterministicClashRunComparer` evaluates every
pair through `IClashMatcher`, preserves all candidates, and greedily selects a one-to-one
subset by `High > Medium > Low`, previous index, then current index. Candidates are indexed
because duplicate occurrence references are allowed. Alternative candidates remain auditable
and must not be treated as false. Unmatched does not mean `New` or `Resolved` and may still
have alternative candidates. The greedy policy is deterministic but not globally optimal;
lifecycle classification remains out of scope.

## Conservative clash lifecycle

`ConservativeClashLifecycleClassifier` consumes an existing `ClashRunMatchResult` and never
reruns matching. Selected `Medium`/`High` without competing alternatives becomes
`StillOpen`; `Low` or competing alternatives becomes `Unverifiable`. Unmatched previous
becomes `Resolved` only when it has no alternative candidate and both revision-free model
identities plus the clash test are observed in the current run. Unmatched current becomes
`New` only under the symmetric conditions in the previous run. A clash test is considered
observed in a run only when that run's `RunManifest.ExecutedClashTests` explicitly declares
it for the same revision-free model pair (direct or A/B-swapped); this is what lets a zero-
occurrence declared test still support `Resolved`/`New`. Raw `ClashStatus` never drives
lifecycle. `Reopened` requires longer history and is out of scope.

## Revision-aware lifecycle HTML

`HtmlLifecycleReportRenderer` lives in `OrzioClashReport.Output.Html` and consumes
`ClashLifecycleResult` directly. The renderer presents `Status`, selected-match confidence,
lifecycle evidence, and match evidence exactly as already produced; it never reclassifies or
rematches. `ClashLifecycleResult.Entries` order is preserved. The legacy
`HtmlReportRenderer` / `IReportRenderer` path remains unchanged. The `compare` command may
optionally write the lifecycle HTML with `-o`/`--output`; without output, its eleven-line
console summary remains unchanged. Source file path metadata, content hashes, and arbitrary
`ClashObject.Properties` are not rendered. Source clash GUIDs, when displayed, are labeled
as evidence only and never as persistent IDs. The HTML is deterministic, self-contained,
and contains no JavaScript or external assets. No `Reopened` status, stable clash id,
persistence, or history beyond two runs exists yet.

## Coordination run assembly

`ICoordinationRunAssembler` converts an already-parsed `ClashReportDocument` plus a
declared `RunManifest` into `CoordinationRun`. `ExactSourceModelCoordinationRunAssembler`
resolves each `ClashObject.SourceModel` only by trimmed `OrdinalIgnoreCase` equality against
`ModelRevision.SourceFileName` or `SourceFilePath`. No basename extraction, extension
removal, path normalization, revision parsing, fuzzy matching, or first-match fallback is
allowed. Zero matches and multiple distinct manifest-model matches are assembly failures.
The exact `ModelRevision` instances from `RunManifest.Models` and exact `ClashResult`
references from the source document are preserved. Batch order, clash order, duplicates,
and A/B orientation are preserved. `CoordinationRun` remains the final authority for
executed-test coverage. The XML and JSON adapters remain independent; the assembler lives
in Core and performs no I/O.

`NavisworksXmlClashSource` populates `ClashObject.SourceModel` from the parsed smarttags
(`Item Source File`, falling back to `Item Source File Name`). `samples/sample-clash.run-manifest.json`
is the companion manifest for `samples/sample-clash.xml`, so the real XML → JSON → assembler
pipeline is exercised end to end in tests.

## Two-run comparison CLI

The legacy single-XML HTML command remains supported.
The compare command receives previous/current XML and manifest paths explicitly; it never infers chronological order from `CreatedAt`, `RunId`, revision, or filenames.
The CLI is only the composition root: XML source → manifest source → coordination-run assembler → matcher → run comparer → lifecycle classifier.
`Program.cs` never recreates matching or lifecycle rules.
Compare mode writes the same deterministic eleven-line console summary as before and may optionally write the revision-aware lifecycle HTML with `-o`/`--output`.
The same run or same file may be supplied in both roles for synthetic smoke testing; this is not sequential real-model validation.

## Immutable coordination-run JSON snapshots

A `CoordinationRun` may now be persisted as a deterministic schema-v1 JSON snapshot by
`OrzioClashReport.Persistence.RunSnapshotJson` (net8.0, System.Text.Json). The public
`JsonCoordinationRunSnapshotSerializer` exposes `Serialize`, `Parse`, `Save`, and `Load`.
This is the single deliberate exception to the "no persistence/history/database" scope gate:
single-run snapshot persistence, explicit snapshot creation CLI, explicit
snapshot-to-snapshot comparison CLI, explicit ordered run-index persistence CLI, and
explicit ordered run-index consumption CLI for adjacent-pair traversal exist; automatic
discovery, chronology inference, latest/previous lookup, all-vs-all comparison, multi-run
lifecycle, ledgers, `Reopened` classification, persistent clash identity, and databases
still do not.

The snapshot is evidence storage, not a persisted lifecycle decision. It contains
`RunManifest` facts, declared model revisions, executed-test coverage, ordered occurrence
slots, and raw clash/object evidence (including the raw `ClashStatus`). Matching candidates,
selected matches, confidence, match evidence, lifecycle entries/evidence/statuses,
fingerprints, and persistent clash IDs are never stored in a run snapshot. Executed clash
tests and occurrences reference the snapshot's models array by explicit A/B model indexes,
and parsing reuses the exact manifest `ModelRevision` / `ModelIdentity` instances addressed
by those indexes. Model/test/occurrence/path-hierarchy order is preserved, and so are
duplicate occurrence slots and A/B orientation. `ClashObject.Properties` is the only
canonicalized collection: property entries are sorted by key with `StringComparer.Ordinal`
before serialization. Snapshot JSON property names are exact case-sensitive camelCase;
unknown and duplicate JSON properties are rejected. `Save` uses create-new semantics, never
overwrites an existing path, and writes UTF-8 without a BOM. Snapshot `schemaVersion` belongs
only to the persistence adapter; it is unrelated to the run-manifest `schemaVersion`. The
persistence adapter depends only on `Core` and never on the XML, run-manifest JSON, HTML, or
CLI adapters.

## Create-run snapshot CLI

The snapshot subcommand is an explicit composition workflow: Navisworks XML + RunManifest
JSON -> ExactSourceModelCoordinationRunAssembler -> CoordinationRun ->
JsonCoordinationRunSnapshotSerializer.Save.
The command is `orzioclash snapshot --xml <input.xml> --manifest <run-manifest.json> (-o <run-snapshot.json> | --output <run-snapshot.json>)`.
Output is mandatory and no filename/storage convention is inferred.
The CLI never constructs RunManifest, ModelRevision, ExecutedClashTest, ClashOccurrence, or
CoordinationRun manually in snapshot mode.
The persistence adapter remains the sole authority for snapshot serialization, canonical
JSON, UTF-8/no-BOM writing, create-new semantics, and overwrite refusal.
Success output is emitted only after Save succeeds.
Snapshot mode creates one immutable run snapshot only; it does not itself compare runs,
persist matching/lifecycle, create history, or create a ledger. Loading stored snapshots
for comparison belongs to the separate `compare-snapshots` command.
Legacy and compare command contracts remain unchanged.

## Compare persisted snapshots CLI

The `compare-snapshots` subcommand is an explicit evidence-only comparison workflow:
previous snapshot JSON + current snapshot JSON ->
`JsonCoordinationRunSnapshotSerializer.Load` ->
`CoordinationRun` previous/current ->
`ConservativeClashMatcher` ->
`DeterministicClashRunComparer` ->
`ConservativeClashLifecycleClassifier` ->
optional `HtmlLifecycleReportRenderer` + deterministic eleven-line console summary.
The command is `orzioclash compare-snapshots --previous-snapshot <previous.json> --current-snapshot <current.json> [-o <output.html> | --output <output.html>]`.
Previous/current remain explicit CLI roles and are never reordered by `CreatedAt`, `RunId`,
revision, filenames, or snapshot metadata. The same snapshot may be supplied in both roles
for synthetic smoke testing; that is not sequential real-model validation.
Matching and lifecycle are recalculated from immutable evidence every time; they are never
loaded from persisted derived state. `compare-snapshots` does not create a run collection,
index, history traversal, ledger, `Reopened`, or persistent clash ID.

## Ordered run index JSON

`OrzioClashReport.Persistence.RunIndexJson` owns a strict deterministic schema-v1 JSON
format for an ordered collection of persisted run-snapshot path references. The format
contains only `schemaVersion` and ordered `snapshotPaths`; it never duplicates run headers,
model revisions, clash evidence, matching, lifecycle, `Reopened`, or persistent clash IDs.
The stored references are canonical, relative to the run-index file directory, and always
persisted with `/` separators. Order is an explicit declaration and is never inferred from
`CreatedAt`, `RunId`, revisions, filenames, or filesystem metadata. Duplicate references are
preserved exactly as declared.

## Create ordered run index CLI

The `index-snapshots` subcommand is an explicit composition workflow:
ordered `--snapshot` CLI paths ->
`JsonCoordinationRunSnapshotSerializer.Load` for each supplied snapshot ->
`RunIndexSnapshotPathResolver.CreateReference` ->
`RunIndexDocument` ->
`JsonRunIndexSerializer.Save`.
The command is `orzioclash index-snapshots --snapshot <run-snapshot.json> [--snapshot <run-snapshot.json> ...] (-o <run-index.json> | --output <run-index.json>)`.
CLI order is the only source of index order; there is no automatic discovery, chronology
inference, latest/previous lookup, non-adjacent/all-vs-all comparison, multi-run lifecycle,
ledger, `Reopened`, or persistent clash ID. The snapshots remain the authority for immutable
run evidence, and matching/lifecycle remain recalculable instead of being persisted into the
index.

## Compare ordered run index CLI

The `compare-index` subcommand is an explicit evidence-only adjacent-traversal workflow:
run-index JSON ->
`JsonRunIndexSerializer.Load` ->
ordered `RunIndexDocument.SnapshotPaths` ->
`RunIndexSnapshotPathResolver.ResolveReference` for every entry ->
`JsonCoordinationRunSnapshotSerializer.Load` for every resolved snapshot ->
ordered `CoordinationRun` list ->
`ConservativeClashMatcher` ->
`DeterministicClashRunComparer` ->
`ConservativeClashLifecycleClassifier` ->
`DeterministicAdjacentClashRunSequenceComparer` ->
`DeterministicSelectedMatchContinuityProjector` ->
`DeterministicSelectedMatchContinuityPathAssembler` ->
`DeterministicClashRunSequenceAnalyzer` ->
`DeterministicClashRunSequencePresentationProjector` ->
`ClashRunSequencePresentationResult` ->
optional `HtmlLongitudinalClashReportRenderer.Render` + `File.WriteAllText` ->
the deterministic twelve-line longitudinal summary, then the existing deterministic
eleven-line pairwise summary reused once per adjacent transition.
The command is `orzioclash compare-index --index <run-index.json> [-o <output.html> | --output <output.html>]`.
The run-index order remains the sole sequence authority: the CLI never reorders by
`CreatedAt`, `RunId`, revision, filename, or filesystem metadata. Duplicate references are
preserved exactly as declared, so adjacent duplicate snapshots remain valid comparisons.
Every snapshot is loaded, every adjacent comparison is computed, continuity is projected,
continuity paths are assembled, analysis is completed, presentation is projected, and any
requested HTML render/write succeeds before the first stdout line. Without `-o`/`--output`,
stdout remains byte-identical to the twelve-line longitudinal prefix plus pairwise blocks
from the previous stage. With output, the only extra stdout line is
`Longitudinal report written to {OutputPath}`, emitted last after the file is written. The
command remains evidence-only and accepts indexes with two or more runs; automatic
discovery, chronology inference, latest/previous lookup, non-adjacent/all-vs-all comparison,
aggregate multi-run lifecycle, Clash Ledger, `Reopened`, persistent clash identity,
fingerprints, path IDs, aggregate path status/confidence, and persisted derived state still
do not exist.

The longitudinal summary prefix is exactly twelve lines, in this order:

1. `Indexed runs: {RunCount}`
2. `Adjacent comparisons: {AdjacentComparisonCount}`
3. `Selected matches: {SelectedMatchCount}`
4. `Continuity links: {ContinuityLinkCount}`
5. `Continuity paths: {ContinuityPathCount}`
6. `Standalone selected matches: {StandaloneSelectedMatchCount}`
7. `Lifecycle entries: {LifecycleEntryCount}`
8. `Non-path lifecycle entries: {NonPathLifecycleEntryCount}`
9. `StillOpen: {StillOpenCount}`
10. `New: {NewCount}`
11. `Resolved: {ResolvedCount}`
12. `Unverifiable: {UnverifiableCount}`

After that prefix, the existing pairwise blocks remain unchanged and are emitted in
transition order as `Comparison {i + 1}/{AdjacentComparisonCount}` plus the eleven-line
pairwise summary for that adjacent transition.

## Operational project catalog JSON

`OrzioClashReport.Persistence.ProjectCatalogJson` owns a strict deterministic schema-v1 JSON
format for operational project state. It contains only `schemaVersion`, `projectId`,
`displayName`, `runIndexPath`, and `longitudinalReportPath`. It never duplicates snapshots,
runs, models, clashes, matching, lifecycle, continuity links, continuity paths, summary
counts, `Reopened`, or persistent clash identity.

- Run snapshots remain immutable evidence only.
- Run index remains the sole authority for explicit sequence order.
- The project catalog stores only operational references relative to the catalog file
  directory, using canonical `/` separators and staying within that directory tree.
- The report path is only a derived-artifact destination; the HTML remains regenerable and
  is not persisted evidence.
- Matching, lifecycle, continuity links, continuity paths, and presentation remain fully
  recalculable and are never persisted into the project catalog.
- There is still no persistent clash identity, Clash Ledger, `Reopened`, database, or
  automatic chronology.

## Create project catalog CLI

The `create-project` subcommand is an explicit operational composition workflow:
project metadata + existing run-index JSON -> run-index validation -> snapshot validation ->
canonical project-catalog references -> `JsonProjectCatalogSerializer.Save`.
The command is
`orzioclash create-project --project-id <project-id> --name <display-name> --index <run-index.json> --report <longitudinal.html> (-o <project.json> | --output <project.json>)`.

The command validates the referenced run index and loads every referenced snapshot before
creating the project catalog. It does not generate HTML, does not mutate snapshots or the
run index, and does not infer chronology or defaults. A project catalog workflow requires
its run index, all resolved snapshots, and report destination to stay inside the project
catalog directory tree. The report destination must never resolve to the project catalog,
the run index, or any snapshot. The report file does not need to exist yet, but its parent
directory must already exist.

## Render project catalog CLI

The `render-project` subcommand is an explicit regeneration workflow:
project-catalog JSON -> resolved run-index JSON -> resolved snapshots ->
existing compare-index analysis/presentation pipeline ->
`HtmlLongitudinalClashReportRenderer.Render` -> `File.WriteAllText`.
The command is `orzioclash render-project --project <project.json>`.

`render-project` resolves `runIndexPath` and `longitudinalReportPath` relative to the
project catalog, reloads immutable snapshot evidence, recalculates all derived longitudinal
state, and rewrites the HTML report destination. It does not overwrite the project catalog,
does not overwrite the run index, does not mutate snapshots, and does not persist derived
matching, lifecycle, continuity, or presentation state. The same workspace rule applies
during rendering: the run index, all resolved snapshots, and the report destination must
stay inside the project catalog directory tree, and the report destination must never
resolve to the project catalog, the run index, or any snapshot.

## Append project snapshot CLI

The `append-project-snapshot` subcommand is an explicit operational append-only workflow:
project-catalog JSON -> resolved run-index JSON -> resolved existing snapshots ->
resolved appended snapshot path -> workspace validation -> appended snapshot validation ->
one new run-index reference at the end -> failure-safe replacement of the existing run-index
file.
The command is
`orzioclash append-project-snapshot --project <project.json> --snapshot <run-snapshot.json>`.

`append-project-snapshot` loads the existing project catalog, resolves its run index and
report destination, loads every already-indexed snapshot, validates the appended snapshot,
and only then replaces the run-index file in place. It preserves every existing
`snapshotPaths` entry exactly as loaded, in the same order, and appends exactly one new
reference at the end. It never reorders, deduplicates, removes, updates, or silently
normalizes earlier entries. Duplicate references remain allowed, including appending the
same snapshot again or appending a distinct snapshot that carries the same `RunId`.

The command mutates only the run index. It does not overwrite the project catalog, does not
mutate any snapshot, and does not regenerate the report automatically. Run
`render-project` separately when refreshed longitudinal HTML is needed. The same workspace
rule still applies: the run index, all existing snapshots, the appended snapshot, and the
report destination must stay inside the project catalog directory tree, and the appended
snapshot must not resolve to the project catalog, the run index, or the report destination.
There is still no automatic chronology, no removal or reordering of runs, and no
concurrent-writer support.

## Validate identity governance evidence CLI

The `validate-identity-governance` subcommand is an explicit, read-only evidence-validation
workflow: project-catalog JSON -> resolved run-index JSON -> resolved and loaded indexed
snapshots -> loaded identity-governance JSON -> `DeterministicIdentityGovernanceEvidenceValidator`
-> deterministic pass/fail summary. The command is
`orzioclash validate-identity-governance --project <project.json> --governance <identity-governance.json>`.

It reuses the same project-catalog workspace loading and protections as
`create-project`/`append-project-snapshot`/`render-project`: the run index and every
indexed snapshot must resolve inside the project catalog's directory tree. It never
requires at least two indexed snapshots -- zero decisions, one indexed snapshot, or many
indexed snapshots are all valid inputs, because this validates evidence, not longitudinal
comparison.

The command never writes, replaces, or creates any file (including the report destination
referenced by the project catalog, which is resolved for workspace-consistency checks only
and is never read or written), never renders HTML, and never runs matching, lifecycle
classification, or continuity analysis. On success it prints project id, indexed run
count, decision count, confirmation/rejection counts by `HumanIdentityDecisionKind`, and
evidence-endpoint count (twice the decision count), followed by
`Identity governance validation passed.`, exit code `0`. On a semantic validation failure it
prints `Identity governance validation failed.` plus a numbered, deterministic issue list to
stderr, stdout stays empty, and exit code is `1` -- usage is never printed for a semantic
failure, only for a parsing failure. On a load or format failure it prints
`Failed to validate identity governance: <message>` to stderr with no stack trace, no file
touched, exit code `1`.

`IIdentityGovernanceEvidenceValidator` / `DeterministicIdentityGovernanceEvidenceValidator`
(`src/OrzioClashReport.Core/Governance/`) are pure Core: they depend on no filesystem, JSON,
CLI, project-catalog/run-index/snapshot adapter, HTML, or Navisworks type, perform no I/O,
and never mutate the governance document or the indexed runs. `Validate` checks, in this
exact deterministic order: project id (ordinal, never normalized) against the expected
project id; duplicate indexed run ids in run-index order (one issue per occurrence after
the first, never resolved by picking a snapshot arbitrarily); then every decision in
persisted order, `Left` endpoint before `Right` endpoint, each requiring its `runId` to be
indexed exactly once and its `occurrenceIndex` to satisfy
`0 <= occurrenceIndex < run.Occurrences.Count`. An endpoint whose `runId` is one of the
duplicated run ids gets no separate issue -- the duplication issue alone already makes the
result invalid. Every issue carries an explicit
`IdentityGovernanceEvidenceValidationIssueKind` (`ProjectIdMismatch`,
`DuplicateIndexedRunId`, `RunNotIndexed`, `OccurrenceIndexOutOfRange`) plus the structured
fields relevant to it, so consumers never have to parse `Message` text.

This stage validates only project binding and evidence-endpoint existence. It never
validates matcher candidacy, run adjacency, left/right ordering intent, transitivity across
decisions, graph conflicts, identity merges, reopening, decision supersession, reviewer
identity, timestamps, or responsibility, and it creates no Clash Ledger.

## Render identity governance review report CLI

The `render-identity-governance-report` subcommand is an explicit, source-only, standalone
review workflow: project-catalog JSON -> resolved run-index JSON -> resolved and loaded
indexed snapshots -> loaded identity-governance JSON ->
`DeterministicIdentityGovernanceEvidenceValidator` -> pure
`DeterministicIdentityGovernanceReviewPresenter` -> `IdentityGovernanceReviewHtmlRenderer`
-> `DerivedHtmlReportWriter`. The command is
`orzioclash render-identity-governance-report --project <project.json> --governance <identity-governance.json> (-o <identity-governance.html> | --output <identity-governance.html>)`.

It reuses the same project-catalog workspace protections as Step 29C for the catalog, run
index, snapshots, and longitudinal report path, and it adds explicit collision protection:
the requested output must not resolve to the same file as the project catalog, run index,
any snapshot, the governance JSON, or the longitudinal report. The command validates all
inputs, runs the evidence validator, and refuses to render when any issue exists. Semantic
validation failure reuses the exact numbered issue format of `validate-identity-governance`,
with stdout empty, usage omitted, exit code `1`, existing output preserved byte-identically,
and no temporary file left behind.

The report is a derived, regenerable artifact only. It is not evidence, not identity
inference, and not longitudinal integration. It presents persisted decisions exactly in
persisted order, Left endpoint before Right endpoint, with endpoint resolution still based
only on `runId` + `occurrenceIndex`. It does not run matching, lifecycle, continuity,
grouping, propagation, transitivity, Clash Ledger, or `Reopened`, and it never changes the
project catalog schema or the existing longitudinal report.

## Adjacent run sequence comparer (Core)

`IClashRunSequenceComparer` formalizes, in Core, the adjacent-traversal orchestration that
previously lived directly in `Program.cs`'s `compare-index` loop. It takes an already
explicitly ordered `IReadOnlyList<CoordinationRun>` -- caller order is authoritative, and the
Core does not know about run-index JSON or any other persistence format -- and compares only
`[i] -> [i + 1]` pairs, never non-adjacent or reversed pairs, never sorting or deduplicating
runs. `DeterministicAdjacentClashRunSequenceComparer` is the sole current implementation: its
constructor takes an `IClashRunComparer` and an `IClashLifecycleClassifier`, and for each
adjacent pair it calls the injected run comparer then the injected lifecycle classifier, with
no cross-transition propagation of selected matches, confidence, or evidence. It requires at
least two runs, rejects a `null` sequence or any `null` entry, preserves duplicate run
references (e.g. `A, A, B`), and is fail-fast: an exception from either injected dependency
on any pair propagates immediately with no partial result returned.
`ClashRunSequenceComparisonResult` is the immutable output: the ordered `Runs` plus one
`ClashLifecycleResult` per adjacent transition in `Comparisons`. It validates only structural
continuity -- each `Comparisons[i]` must reference `Runs[i]`/`Runs[i + 1]` by exact object
reference, never by `RunId`, `CreatedAt`, or value equality -- and never recomputes matching
or lifecycle. It represents only an ordered collection of independently recalculated adjacent
pairwise lifecycle results: no history, no multi-run lifecycle, no persistent clash identity,
no Clash Ledger, no `Reopened`. `compare-index` is the only current consumer; `compare` and
`compare-snapshots` remain pairwise via the existing `CreateDerivedComparison` helper.

## Selected-match continuity projection (Core, consumed by compare-index analysis)

`IClashRunSequenceContinuityProjector` projects an already-derived
`ClashRunSequenceComparisonResult` onto the set of `SelectedMatchContinuityLink`s at its
consecutive comparison boundaries: at boundary `i` (between `Comparisons[i]` and
`Comparisons[i + 1]`, sharing the run at `Runs[i + 1]`), a link exists wherever a selected
match's `CurrentIndex` enters an occurrence slot and a selected match's `PreviousIndex`
leaves the exact same slot. The projector calls no `IClashMatcher`, `IClashRunComparer`,
`IClashLifecycleClassifier`, or `IClashRunSequenceComparer`, and knows nothing about
run-index JSON or snapshot files.

`DeterministicSelectedMatchContinuityProjector` is the sole current implementation, with a
public parameterless constructor (no dependencies). It considers only
`ClashRunMatchResult.SelectedMatches` -- `Candidates`, `AlternativeCandidates`,
`UnmatchedPrevious`, and `UnmatchedCurrent` never create a link, and a selected match's
`ClashLifecycleStatus` (even `Unverifiable`) never filters the projection. Only consecutive
boundaries are considered -- never `[0]` to `[2]`, never non-adjacent, never sorted or
deduplicated (duplicate run references and duplicate `RunId` values are preserved as-is).

`SelectedMatchContinuityLink` observes only that a selected match enters an exact
occurrence slot of a shared run and another selected match leaves the exact same slot
through the immediately following comparison. It stores `IncomingComparisonIndex` and
`SharedOccurrenceIndex`; `OutgoingComparisonIndex` and `SharedRunIndex` are derived
(`IncomingComparisonIndex + 1`). It requires exact object-reference continuity -- a
value-shaped-equivalent occurrence at a different slot never satisfies it -- and carries no
identifier, fingerprint, status, or aggregated confidence.

`ClashRunSequenceContinuityResult` is the immutable output: the exact `SequenceComparison`
reference plus the complete, canonically ordered (`IncomingComparisonIndex` ascending, then
`SharedOccurrenceIndex` ascending) set of `Links`. It independently re-validates every
link's exact selected-match membership (never an alternative candidate, never an
equivalent-but-distinct object), shared-run reference, shared-slot continuity, and
completeness against the sequence comparison alone, rejecting a missing link, an extra
link, a duplicate link, or any non-canonical order as a single structural check -- never
rematching.

This is the smallest possible longitudinal observation: a link never asserts the underlying
clash is the same clash. Derived maximal continuity path assembly exists (see below);
persistent tracking, ledger, identity, lifecycle aggregation, and `Reopened` still do not.
Links are derived and recalculable, never persisted. `compare-index` consumes this
projection indirectly through `DeterministicClashRunSequenceAnalyzer`; no HTML renderer
consumes it, and no CLI calls it directly outside that analyzer composition. Sequential
real Navisworks export validation remains unverified.

## Deterministic maximal continuity path assembly (Core, consumed by compare-index analysis)

`IClashRunSequenceContinuityPathAssembler` assembles an already-derived
`ClashRunSequenceContinuityResult` into the complete set of disjoint maximal continuity
paths implied by its links: two links belong to the same path only when the first link's
`OutgoingSelectedMatch` is the exact same object reference as the second link's
`IncomingSelectedMatch` at the immediately following comparison boundary
(`next.IncomingComparisonIndex == current.OutgoingComparisonIndex`). The assembler knows no
JSON, snapshot, or filesystem, and calls no `IClashMatcher`, `IClashRunComparer`,
`IClashLifecycleClassifier`, `IClashRunSequenceComparer`, or
`IClashRunSequenceContinuityProjector` -- matching, run comparison, lifecycle
classification, sequence comparison, and continuity projection have already happened by the
time it runs.

`DeterministicSelectedMatchContinuityPathAssembler` is the sole current implementation, with
a public parameterless constructor (no dependencies at all). For each link in
`ContinuityResult.Links` canonical order, it checks for an exact predecessor; a link with no
predecessor starts a new path, which then follows its chain of exact successors until none
remains. Connectivity never uses `RunId`, `CreatedAt`, candidate indices alone, occurrence
reference alone, candidate or assessment value equality, source clash GUID, confidence,
evidence, `ToString`, hash, or fingerprint -- only exact selected-match object-reference
identity. Zero links produce zero paths, and no zero-link path is ever created. Because the
current invariants guarantee at most one exact predecessor and one exact successor per link,
the assembler defensively detects more than one of either (impossible under normal
construction, but a guard against corruption or future regression) and throws
`InvalidOperationException` rather than silently picking the first match.

`SelectedMatchContinuityPath` is an immutable maximal sequence of
`SelectedMatchContinuityLink`s connected only by this exact-reference rule, with
`SelectedMatches` derived from `Links` (never supplied), and `StartComparisonIndex`/
`EndComparisonIndex`/`StartRunIndex`/`EndRunIndex` derived from the first and last link. It
carries no id, status, fingerprint, or aggregated confidence.

`ClashRunSequenceContinuityPathsResult` is the immutable output: the exact
`ContinuityResult` reference plus the complete, canonically ordered set of `Paths` (ordered
by each path's first link position in `ContinuityResult.Links`). It independently
re-validates the complete maximal partition, rejecting a missing path, an extra path, a
duplicate path, a wrong path order, a foreign or equivalent-but-distinct link, a missing or
extra link inside a path, duplicate link coverage, a split of one maximal path, a merge of
disconnected paths, and a non-maximal path, all through a single structural comparison --
never rematching or re-invoking the assembler.

A continuity path is a derived, maximal, and fully recalculable sequence of exact
selected-match continuity links; it is not a persistent clash identity, a stable clash
identity, a Clash Ledger, or a persistent track, has no history or multi-run lifecycle, and
does not imply `Reopened`. A selected match with no continuity link never appears in any
path, and no zero-link path is ever created. `compare-index` consumes this assembly
indirectly through `DeterministicClashRunSequenceAnalyzer`; no HTML renderer consumes it,
and no CLI calls it directly outside that analyzer composition. Sequential real Navisworks
export validation remains unverified.

## Longitudinal sequence analysis orchestrator (Core, consumed by compare-index)

`IClashRunSequenceAnalyzer` is the single Core boundary that composes the existing
longitudinal stages in caller-declared order:
`IClashRunSequenceComparer` -> `IClashRunSequenceContinuityProjector` ->
`IClashRunSequenceContinuityPathAssembler`. The caller's run order remains the only
authority; the analyzer never sorts, deduplicates, infers chronology, compares
non-adjacent runs, persists derived state, or creates history, a Clash Ledger,
multi-run lifecycle, `Reopened`, or stable/persistent clash identity.

`DeterministicClashRunSequenceAnalyzer` is the current implementation. It receives the
three ports through its constructor, rejects null dependencies, rejects null `runs` before
invoking any dependency, calls each stage exactly once in order, passes the exact result
reference from one stage to the next, and propagates exceptions without wrapping them or
returning a partial result. It is synchronous, deterministic, and performs no I/O, clock
access, network access, randomness, DI-container resolution, matching, lifecycle
classification, continuity projection, or path assembly of its own.

`ClashRunSequenceAnalysisResult` is the immutable aggregate: the exact `SequenceComparison`,
exact `ContinuityResult`, and exact `ContinuityPathsResult` references from one coherent
derived chain. Its internal constructor rejects null inputs and rejects
value-equivalent-but-distinct chains unless
`ReferenceEquals(ContinuityResult.SequenceComparison, SequenceComparison)` and
`ReferenceEquals(ContinuityPathsResult.ContinuityResult, ContinuityResult)` both hold. It
adds no ids, statuses, fingerprints, aggregate confidence, history, ledger, persistence
metadata, lifecycle aggregation, or aliases. `compare-index` consumes this analyzer after
loading every snapshot and before writing stdout; no HTML renderer consumes it.

## Longitudinal presentation model (Core, consumed by compare-index)

`IClashRunSequencePresentationProjector` projects an already-complete
`ClashRunSequenceAnalysisResult` onto a lossless presentation view -- ordered runs,
comparisons, continuity links, continuity paths, every lifecycle entry, and the entries and
selected matches that fall outside any continuity path -- without recomputing matching,
lifecycle classification, continuity projection, or path assembly. It only indexes and
relates the exact references it is given, in the exact order they already have.

`DeterministicClashRunSequencePresentationProjector` is the sole current implementation,
with a public parameterless constructor (no dependencies). It associates a selected match
with its continuity path only through an exact `ClashRunMatchCandidate` reference lookup
built from `ContinuityPathsResult.Paths[*].SelectedMatches` -- never `RunId`, `CreatedAt`,
GUID, value equality, `ToString`, hash, or fingerprint. The same exact selected-match
reference appearing in two different paths is a structural impossibility given
`ClashRunSequenceContinuityPathsResult`'s own disjoint partition; the projector fails fast
with `InvalidOperationException` rather than silently picking one.

Four new immutable model types carry the presentation, all with `internal` constructors:
`ClashRunSequenceLifecycleEntryPresentation` (one entry at its exact position, with an
optional exact continuity-path reference and no status/confidence of its own),
`ClashRunSequenceTransitionPresentation` (one comparison plus the complete ordered
presentation of every one of its entries), `ClashRunSequenceContinuityPathPresentation`
(one path plus the presentation of the lifecycle entry behind each of its selected
matches), and `ClashRunSequencePresentationResult`, the aggregate.

`ClashRunSequencePresentationResult` preserves `Runs`/`Comparisons`/`ContinuityLinks`/
`ContinuityPaths` as the exact canonical references from `AnalysisResult`, and exposes
indexed views: `Transitions`, the complete `LifecycleEntries` (no loss, duplication, or
reorder), `PathPresentations`, `StandaloneSelectedMatches`, and `NonPathLifecycleEntries`.
Every selected match belongs to exactly one of a path's `SelectedMatchEntries` or
`StandaloneSelectedMatches`; every entry outside a path -- including every unmatched `New`,
`Resolved`, and `Unverifiable` entry -- is in `NonPathLifecycleEntries`, of which
`StandaloneSelectedMatches` is an exact subset. Standalone and non-path views reuse the
exact same presentation item references as `LifecycleEntries`, never copies. Twelve derived
counters (run/comparison/selected-match/link/path/standalone/entry/non-path counts plus one
per lifecycle status) are computed exclusively from the already-projected collections, never
supplied by the caller. Canonical order follows `ComparisonIndex` then `EntryIndex`
ascending for entries, and the underlying `ContinuityPathsResult.Paths` order for paths --
never status, confidence, `RunId`, `CreatedAt`, GUID, or path length.

This is Core presentation derived from an already-complete analysis chain -- not history, a
ledger, or persistent/stable clash identity. A continuity path presented here is still just a
derived maximal sequence of exact-reference links, never a persistent clash. `compare-index`
consumes this projection for its deterministic twelve-line longitudinal stdout prefix and,
when `-o`/`--output` is supplied, for the longitudinal HTML renderer. There is still no
derived-state persistence, no aggregate multi-run lifecycle, no Clash Ledger, no persistent
clash identity, and no `Reopened`. Sequential real Navisworks export validation remains
unverified.

## Longitudinal lifecycle HTML

`HtmlLongitudinalClashReportRenderer` lives in `OrzioClashReport.Output.Html` and is a
public sealed concrete adapter renderer with a public parameterless constructor and a single
contract: `Render(ClashRunSequencePresentationResult result)`. It consumes the complete
presentation result directly and never calls the analyzer, comparer, matcher, lifecycle
classifier, continuity projector, path assembler, or presentation projector. It never
recomputes matching, lifecycle, links, paths, partitions, or summary counters, performs no
I/O, clock access, network access, randomness, or filesystem inspection, and returns one
complete deterministic HTML string; `Program.cs` remains responsible for `File.WriteAllText`.

The HTML is HTML5 (`<!doctype html>`), `lang="en"`, UTF-8, titled exactly
`Orzio Clash Longitudinal Report`, self-contained, deterministic byte-for-byte, responsive,
printable, has all CSS inline in one `<style>`, and has no JavaScript, external links,
fonts, images, stylesheets, or assets. Dynamic content is HTML-encoded. The renderer may
show run IDs, `CreatedAt` formatted with `"O"` and invariant culture, counts, declared
models in order, company/discipline/model/revision/source file name, clash tests and names,
elements, levels, distances, points, source clash GUID labeled evidence only, confidence,
lifecycle evidence, and match evidence with previous/current values. It must not show
`ModelRevision.SourceFilePath`, `ModelRevision.ContentHash`, `ClashObject.Properties`, local
or network paths, invented fields, generation timestamps, or index file paths.

The nine top-level sections are fixed and ordered by stable classes:
`longitudinal-header`, `longitudinal-summary-section`, `interpretation-warning`,
`run-sequence-section`, `continuity-paths-section`,
`standalone-selected-matches-section`, `non-path-lifecycle-section`,
`transition-sections`, and `longitudinal-classification-note`. The renderer preserves all
presentation order it receives: runs, paths, selected-match entries, standalone selected
matches, non-path entries, transitions, lifecycle entries, and evidence. Visible ordinals
for runs, paths, transitions, and entries are presentation ordinals only, never IDs.

The report presents the explicit run sequence and declared revisions, the twelve
longitudinal counters, interpretation warnings, maximal continuity paths, standalone
selected matches, lifecycle entries outside paths, and every adjacent transition with all
lifecycle entries, `New`, `StillOpen`, `Resolved`, `Unverifiable`, confidence, lifecycle
evidence, and match evidence. It states honestly that continuity paths are derived from
recalculated adjacent selected matches, do not prove identity, and are recalculable and not
persisted; `High` is not human confirmation; `Unverifiable` means available evidence or
candidate competition blocks safe automatic classification; source GUID is evidence only;
there is no Clash Ledger, `Reopened`, aggregate multi-run lifecycle, persistent clash
identity, fingerprint, path ID, aggregate path status, or aggregate path confidence.

## Anti-patterns that fail review

- Core with a `using System.Xml.Linq`, a Navisworks reference, or any HTML string.
- Core retargeted to `net8.0` "to use newer C#". This breaks the future 4.8 adapter.
- One project that both parses XML and renders HTML.
- A mutable `ClashResult` filled in field by field across the codebase.
- A `catch { }` or a `catch (Exception) { return null; }` with no log.
- Non-deterministic output that breaks the golden-file test, then deleting the test to "fix" it.
- Building the API adapter, images, or licensing before the MVP is validated on a real model.
