# AGENTS.md — OrzioClashReport

> This file is the agent contract for **any** coding assistant working in this repo
> (Codex, Claude Code, or otherwise). It mirrors the architecture guardrail in
> `.claude/skills/orzio-clash-report/SKILL.md`. If the two ever disagree, they are both
> wrong — fix them together. The full kickoff brief lives in
> [docs/claude-code-init-prompt.md](docs/claude-code-init-prompt.md); read it before Step 1.

## What this tool is

OrzioClashReport reads Autodesk Navisworks Clash Detective results and produces a clean,
grouped HTML coordination report. The value is **not** detection (Navisworks already
detects). The value is **honest grouping**: hundreds of raw clashes collapse to tens of
real problems. Treat it as a production product, not a script.

Two facts pin the whole design:
- MVP input is the Navisworks **Clash Detective XML export**. Nothing else in the MVP.
- The near-certain next step reads clashes live from the Navisworks **.NET API**
  (`Autodesk.Navisworks.Api`, `DocumentClash`, `ClashTests`, `ClashResults`), which is
  **.NET Framework 4.8 only** and compatible only within one Navisworks major version
  (2024 / 2025 / 2026 need separate binaries). The core must absorb that later with
  **zero** change.

## The twelve laws (non-negotiable)

1. **Ports and Adapters. Dependencies point inward only.** `Core` knows nothing about XML,
   HTML, or Navisworks. Adapters depend on `Core`, never on each other.
2. **Core targets `netstandard2.0`.** So it is consumable by both the `net8.0` CLI today
   and a `net48` Navisworks-API adapter tomorrow. Never retarget the core to `net8.0`.
3. **Core has zero third-party dependencies.** No Newtonsoft, no logging framework. XML
   parsing lives in the XML adapter using `System.Xml.Linq`, never in Core.
4. **Domain types are immutable and source-agnostic.** `ClashReportDocument`, `ClashBatch`,
   `ClashResult`, `ClashObject`, `ClashPoint`, `ClashStatus`, `GroupedClashReport`,
   `ClashGroup`. No public mutable setters after construction. A `ClashResult` must be
   identical whether it came from XML or from the live API.
5. **No invented members.** Never reference an XML element/attribute or a Navisworks API
   member you have not seen in a fixture or verified in docs. Unseen fields are optional.
6. **Defensive parsing.** Missing optional field becomes null. Unrecognized status maps to
   `ClashStatus.Unknown` and is logged. Malformed input fails loudly with a clear message;
   it never produces a silently-wrong report.
7. **No silent catch.** Catch, log with context, then decide: skip this element or fail the
   run. Swallowing an exception is a defect.
8. **Deterministic rendering.** Same input produces byte-identical HTML. No `DateTime.Now`
   in the body, no unordered dictionaries, stable sort keys. This is what makes golden-file
   tests possible.
9. **The grouper is the differentiator, and it is rule-based.** Collapse exact duplicates
   (same element-id pair, same point within tolerance), then group by discipline-pair plus
   level. No LLM/AI grouping in the MVP.
10. **Discipline resolution is pluggable.** Element-to-discipline mapping goes behind
    `IDisciplineResolver`, never hardcoded in the grouper or renderer.
11. **No business logic in UI or CLI.** `Program.cs` and any future WPF/dockable panel only
    wire `IClashSource -> IClashGrouper -> IReportRenderer`. All logic lives in Core.
12. **Reserve, do not build, the future.** The Navisworks-API adapter, PDF output, WPF UI,
    and image enrichment each get a reserved interface and a `// FUTURE:` seam comment, and
    nothing more, until the MVP is validated on a real model.

## Solution layout (target)

```
OrzioClashReport.sln
├── src/
│   ├── OrzioClashReport.Core/                netstandard2.0, ZERO third-party deps, nullable ON, warnings-as-errors
│   ├── OrzioClashReport.Input.NavisworksXml/ netstandard2.0, System.Xml.Linq
│   ├── OrzioClashReport.Input.RunManifestJson/ net8.0, System.Text.Json, run manifest input adapter
│   ├── OrzioClashReport.Output.Html/         netstandard2.0, deterministic HTML
│   ├── OrzioClashReport.Persistence.RunIndexJson/ net8.0, System.Text.Json, ordered explicit run index
│   ├── OrzioClashReport.Persistence.RunSnapshotJson/ net8.0, System.Text.Json, immutable run snapshot
│   └── OrzioClashReport.Cli/                 net8.0 console entry point
├── tests/
│   └── OrzioClashReport.Tests/               net8.0, xUnit
├── samples/                                  sample-clash.xml fixture (author-provided)
├── docs/
└── AGENTS.md / README.md
```

## Scope gate (reject creep, log to README "Backlog")

**In scope (MVP):** XML parsing, rule-based grouping, deterministic light-theme HTML with
per-discipline color accents, CLI, tests.

**Out of scope until explicitly reopened:** clash images / viewpoint capture, PDF,
licensing / keys / payment, WPF or any GUI, the live Navisworks-API adapter, status editing,
responsible-party assignment, comments, history, database, CDE / BIM 360 integration, LLM
grouping, DI container, config framework. When image support eventually lands, it reuses
Navisworks-exported viewpoint images; it does not generate images from scratch.

## Build order (do in sequence; stop and show after each)

1. Scaffold solution + all projects with the exact TFMs above. Compiles, empty.
2. Domain model + `ClashStatus`. Compiles, no logic.
3. `IClashSource` + `NavisworksXmlClashSource` parsing `samples/sample-clash.xml` + `ParsingTests`.
4. `IClashGrouper` + `RuleBasedGrouper` + `GroupingTests` (hand-built objects, no XML).
5. `IReportRenderer` + `HtmlReportRenderer` + golden-file test.
6. `Cli` wiring end to end: `orzioclash samples/sample-clash.xml -o report.html`.

Do not write parsing, grouping, or rendering logic before the sample XML fixture exists.

## Honest validation (state which one you actually did)

Three different claims. Never conflate them, never report a higher one than you verified:

- **Compiles**: `dotnet build` succeeds.
- **Runs**: `dotnet test` is green AND the CLI produced an HTML file from the sample fixture.
- **Validated on a real model**: a human ran it on a real, anonymized Navisworks export and
  confirmed the grouped report matches reality.

An agent can claim the first two. Only the human can claim the third.

## Internal preview release contract

Current internal preview target: `0.1.0-preview.1`, distributed as `orzioclash.exe` for
Windows `win-x64`. The future release tag is `v0.1.0-preview.1`; release tags must point to
commits contained in `master`.

README and user-facing documentation remain English. Published CLI stdout/stderr contracts
must not be silently broken after release.

No real/private artifacts enter Git or release packages: validation XML, HTML, PDF, local
paths, project names, model names, personal names, NWD, NWF, NWC, RVT, and images stay out.
Private validation stays documented only through public aliases and scoped claims.

Validation status is intentionally narrow: single-run parsing, grouping, and HTML were
human-validated on one private real export. Longitudinal matching, lifecycle, continuity
links, continuity paths, and longitudinal HTML remain unvalidated against three real
historical exports and are experimental.

The preview does not provide persistent clash identity, Clash Ledger, `Reopened`,
aggregate multi-run lifecycle, automatic chronology, or automatic clash responsibility.
`workflow_dispatch` release runs are packaging dry runs only. Responsibility/authorship
remains deferred to a later human-governance stage.

## Anti-patterns that fail review

- Core with a `using System.Xml.Linq`, a Navisworks reference, or any HTML string.
- Core retargeted to `net8.0` "to use newer C#".
- One project that both parses XML and renders HTML.
- A mutable `ClashResult` filled in field by field across the codebase.
- A `catch { }` or `catch (Exception) { return null; }` with no log.
- Non-deterministic output that breaks the golden-file test, then deleting the test to "fix" it.
- Building the API adapter, images, or licensing before the MVP is validated on a real model.
- Inventing XML element/attribute names not present in `samples/sample-clash.xml`.

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
declared exactly in the manifest. Do not create fingerprints, matching, deduplication, or
source-model inference in these types.

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
`New` only under the symmetric conditions in the previous run. Raw `ClashStatus` never
drives lifecycle. `Reopened` requires longer history and is out of scope.

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

## Explicit executed clash test coverage

`RunManifest` explicitly declares `ExecutedClashTests`. Each declaration contains a test
name and an ordered pair of revision-free `ModelIdentity` values. Coverage lookup is
case-insensitive by test name and unordered by model pair, while the declaration preserves
A/B order. Every `ClashOccurrence` in a `CoordinationRun` must correspond to a declared
executed test and model pair. An executed test may have zero occurrences -- that is how a
run proves it executed a test and got zero results, rather than never running it at all.
Lifecycle test coverage must come only from the explicit manifest declaration, never from
observed occurrences: `ConservativeClashLifecycleClassifier` consults
`RunManifest.ExecutedClashTests`, not `CoordinationRun.Occurrences`, to decide whether a
clash test is "observed" in the other run. Manifest JSON `schemaVersion` 2 is required;
`schemaVersion` 1 is intentionally rejected because it never declared executed-test
coverage, and silently treating it as declaring zero tests would be indistinguishable from
"we don't know what ran."

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

`NavisworksXmlClashSource` now populates `ClashObject.SourceModel` (see the "Explicit
executed clash test coverage" adapter and the `Item Source File` / `Item Source File Name`
precedence documented at the parser). `samples/sample-clash.run-manifest.json` is the
companion manifest for `samples/sample-clash.xml`, letting the full real pipeline
(`NavisworksXmlClashSource` → `JsonRunManifestSource` → `ExactSourceModelCoordinationRunAssembler`)
run end to end in tests.

## Immutable coordination-run JSON snapshots

A `CoordinationRun` may now be persisted as a deterministic schema-v1 JSON snapshot by
`OrzioClashReport.Persistence.RunSnapshotJson` (`JsonCoordinationRunSnapshotSerializer` with
`Serialize`/`Parse`/`Save`/`Load`). This is the one deliberate, narrow exception to the
"no persistence/history/database" scope gate: single-run snapshot persistence, explicit
snapshot creation CLI, explicit snapshot-to-snapshot comparison CLI, explicit ordered
run-index persistence CLI, and explicit ordered run-index consumption CLI for adjacent-pair
traversal exist; automatic discovery, chronology inference, latest/previous lookup,
all-vs-all comparison, multi-run lifecycle, ledgers, `Reopened`, persistent clash identity,
and databases still do not.

- The snapshot is evidence storage, not a persisted lifecycle decision. It contains
  `RunManifest` facts, declared model revisions, executed-test coverage, ordered occurrence
  slots, and raw clash/object evidence (including the raw `ClashStatus`).
- Matching candidates, selected matches, confidence, match evidence, lifecycle
  entries/evidence/statuses, fingerprints, and persistent clash IDs are never stored in a run
  snapshot. They are recalculable; freezing them into the evidence layer is forbidden. Raw
  `ClashStatus.Resolved` is source evidence and is not a lifecycle status.
- Executed clash tests and occurrences reference the snapshot's `models` array by explicit
  A/B model indexes. Parsing reuses the exact manifest `ModelRevision` / `ModelIdentity`
  instances addressed by those indexes.
- Model, executed-test, occurrence, and path-hierarchy order is preserved; duplicate
  occurrence slots and A/B orientation are preserved. `ClashObject.Properties` is the only
  canonicalized collection: property entries are sorted by key with `StringComparer.Ordinal`
  before serialization.
- Snapshot JSON property names are exact, case-sensitive camelCase; unknown and duplicate
  JSON properties are rejected (recursively). Timestamps require an explicit offset or `Z`.
- `Save` uses `FileMode.CreateNew` semantics and never overwrites an existing path, even with
  byte-identical content; there is no `--force` and no idempotent overwrite. It writes UTF-8
  without a BOM, and a serialization failure creates no file.
- Snapshot `schemaVersion` belongs only to this adapter and is unrelated to the run-manifest
  `schemaVersion` (the run-manifest adapter is at schema v2). The Core does not know
  `schemaVersion` exists.
- The persistence adapter depends only on `Core` and never on the XML, run-manifest JSON,
  HTML, or CLI adapters. Its `DuplicatePropertyValidator` and
  `StrictIso8601DateTimeOffsetConverter` are independent copies, not shared with the manifest
  adapter.

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

## Adjacent run sequence comparer (Core)

`IClashRunSequenceComparer` (`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceComparer.cs`)
formalizes, in Core, the adjacent-traversal orchestration that previously lived directly in
`Program.cs`'s `compare-index` loop. It takes an already explicitly ordered
`IReadOnlyList<CoordinationRun>` -- caller order is authoritative, and the Core does not know
about run-index JSON or any other persistence format -- and compares only `[i] -> [i + 1]`
pairs, never non-adjacent or reversed pairs.

`DeterministicAdjacentClashRunSequenceComparer` is the sole current implementation. Its
constructor takes an `IClashRunComparer` and an `IClashLifecycleClassifier`; for each adjacent
pair it calls the injected run comparer, then the injected lifecycle classifier, with no
cross-transition propagation of selected matches, confidence, or evidence between pairs.
Requires at least two runs and rejects any `null` entry; rejects a `null` sequence outright.
Duplicate run references (e.g. `A, A, B`) are preserved, never deduplicated. The traversal is
synchronous, sequential, and fails fast: an exception from either injected dependency on any
pair propagates immediately and no partial `ClashRunSequenceComparisonResult` is returned.

`ClashRunSequenceComparisonResult` is the immutable output: the ordered `Runs` plus one
`ClashLifecycleResult` per adjacent transition in `Comparisons`, same order. It validates
only structural continuity -- every `Comparisons[i]` must reference `Runs[i]` and
`Runs[i + 1]` by **exact object reference** (not `RunId`, not `CreatedAt`, not value
equality) as its previous/current sides -- and never recomputes matching or lifecycle. It
represents only an ordered collection of independently recalculated adjacent pairwise
lifecycle results: it creates no history, no multi-run lifecycle, no persistent clash
identity, no Clash Ledger, and no `Reopened`. `compare-index` is the only current consumer;
`compare` and `compare-snapshots` remain pairwise and continue using the existing
`CreateDerivedComparison` helper in `Program.cs`, not this sequence comparer.

## Selected-match continuity projection (Core, consumed by compare-index analysis)

`IClashRunSequenceContinuityProjector` (`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceContinuityProjector.cs`)
projects an already-derived `ClashRunSequenceComparisonResult` onto the set of
`SelectedMatchContinuityLink`s that exist at its consecutive comparison boundaries: for
boundary `i` (between `Comparisons[i]` and `Comparisons[i + 1]`, sharing the run at
`Runs[i + 1]`), a link exists wherever a selected match's `CurrentIndex` enters an
occurrence slot and a selected match's `PreviousIndex` leaves the exact same slot. The
projector does not know run-index JSON, does not load snapshots, and calls no
`IClashMatcher`, `IClashRunComparer`, `IClashLifecycleClassifier`, or
`IClashRunSequenceComparer` -- matching, run comparison, lifecycle classification, and
sequence comparison have already happened by the time it runs.

`DeterministicSelectedMatchContinuityProjector` (`src/OrzioClashReport.Core/Continuity/DeterministicSelectedMatchContinuityProjector.cs`)
is the sole current implementation, with a public parameterless constructor (no
dependencies at all). It considers only `ClashRunMatchResult.SelectedMatches` --
`Candidates`, `AlternativeCandidates`, `UnmatchedPrevious`, and `UnmatchedCurrent` never
create a link, and neither does a selected match's `ClashLifecycleStatus` (a selected match
classified `Unverifiable` may still produce a link; lifecycle status is never used as a
filter). Only consecutive boundaries are considered -- there is no non-adjacent or `[0]` to
`[2]`-style comparison, and no run is ever compared against a run it is not immediately
adjacent to. Duplicate run references and duplicate `RunId` values are never deduplicated.

`SelectedMatchContinuityLink` (`src/OrzioClashReport.Core/Model/SelectedMatchContinuityLink.cs`)
observes only that one selected match enters an exact occurrence slot of a shared run and
another selected match leaves that exact same slot through the immediately following
comparison. It stores `IncomingComparisonIndex` and `SharedOccurrenceIndex`; `OutgoingComparisonIndex`
and `SharedRunIndex` are derived (`IncomingComparisonIndex + 1` in both cases). It validates
exact slot and exact object-reference continuity between the two selected matches and the
shared occurrence -- value-shaped equivalence at a different slot never satisfies it. It
carries no identifier, fingerprint, status, or aggregated confidence.

`ClashRunSequenceContinuityResult` (`src/OrzioClashReport.Core/Model/ClashRunSequenceContinuityResult.cs`)
is the immutable output: the exact `SequenceComparison` reference plus the complete,
canonically ordered (`IncomingComparisonIndex` ascending, then `SharedOccurrenceIndex`
ascending) set of `Links`. It independently re-validates every link's membership (exact
selected-match reference, never an alternative or an equivalent-but-distinct object),
shared-run reference, shared-slot continuity, and completeness -- it recomputes, from
`SequenceComparison` alone, the full expected set of (boundary, slot) pairs and requires
`Links` to match it exactly, position for position, which is what rejects a missing link,
an extra link, a duplicate link, and any non-canonical order all at once. This is
structural validation, never rematching.

This is the smallest possible longitudinal observation and stops well short of clash
identity: a link never asserts that the underlying clash is the same clash. Derived maximal
continuity path assembly exists (see below); persistent tracking, ledger, identity,
lifecycle aggregation, and `Reopened` still do not. Links are derived and fully
recalculable; they are never persisted. `compare-index` consumes this projection indirectly
through `DeterministicClashRunSequenceAnalyzer`; no HTML renderer consumes it, and no CLI
calls it directly outside that analyzer composition. Sequential real Navisworks export
validation remains unverified.

## Deterministic maximal continuity path assembly (Core, consumed by compare-index analysis)

`IClashRunSequenceContinuityPathAssembler`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceContinuityPathAssembler.cs`)
assembles an already-derived `ClashRunSequenceContinuityResult` into the complete set of
disjoint maximal continuity paths implied by its links: two links belong to the same path
only when the first link's `OutgoingSelectedMatch` is the exact same object reference as the
second link's `IncomingSelectedMatch` at the immediately following comparison boundary
(`next.IncomingComparisonIndex == current.OutgoingComparisonIndex`). The assembler knows no
JSON, snapshot, or filesystem, and calls no `IClashMatcher`, `IClashRunComparer`,
`IClashLifecycleClassifier`, `IClashRunSequenceComparer`, or
`IClashRunSequenceContinuityProjector` -- matching, run comparison, lifecycle
classification, sequence comparison, and continuity projection have already happened by the
time it runs.

`DeterministicSelectedMatchContinuityPathAssembler`
(`src/OrzioClashReport.Core/Continuity/DeterministicSelectedMatchContinuityPathAssembler.cs`)
is the sole current implementation, with a public parameterless constructor (no
dependencies at all). For each link in `ContinuityResult.Links` canonical order, it checks
for an exact predecessor (a link whose `OutgoingComparisonIndex` and `OutgoingSelectedMatch`
reference exactly match the current link's `IncomingComparisonIndex` and
`IncomingSelectedMatch`); a link with no predecessor starts a new path, which then follows
its chain of exact successors until none remains. Connectivity never uses `RunId`,
`CreatedAt`, candidate indices alone, occurrence reference alone, candidate or assessment
value equality, source clash GUID, confidence, evidence, `ToString`, hash, or fingerprint --
only exact selected-match object-reference identity. Zero links produce zero paths, and no
zero-link path is ever created. Because the current invariants guarantee at most one exact
predecessor and one exact successor per link, the assembler defensively detects more than
one of either (impossible under normal construction, but a guard against corruption or
future regression) and throws `InvalidOperationException` rather than silently picking the
first match.

`SelectedMatchContinuityPath`
(`src/OrzioClashReport.Core/Model/SelectedMatchContinuityPath.cs`) is an immutable maximal
sequence of `SelectedMatchContinuityLink`s connected only by this exact-reference rule. Its
internal constructor rejects null/empty links, a null link slot, a repeated or inverted
boundary, a boundary gap, and a value-equivalent-but-distinct candidate reference at any
join. `SelectedMatches` is derived, never supplied: `Links[0].IncomingSelectedMatch` followed
by every link's `OutgoingSelectedMatch`, so `SelectedMatches.Count == Links.Count + 1`.
`StartComparisonIndex`/`EndComparisonIndex`/`StartRunIndex`/`EndRunIndex` are derived from the
first and last link, never stored redundantly. The path carries no id, status, fingerprint,
or aggregated confidence -- it asserts only that these exact continuity links form one
maximal exact-reference-connected sequence, never that the underlying clash is a single
persistent entity, nor that the path has stable identity or survives recalculation by id.

`ClashRunSequenceContinuityPathsResult`
(`src/OrzioClashReport.Core/Model/ClashRunSequenceContinuityPathsResult.cs`) is the immutable
output: the exact `ContinuityResult` reference plus the complete, canonically ordered set of
`Paths`. Canonical order is the position of each path's first link in
`ContinuityResult.Links` -- never path length, `RunId`, `CreatedAt`, confidence, source GUID,
or occurrence details. It independently re-validates the complete maximal partition by
recomputing, from `ContinuityResult.Links` alone, the same predecessor/successor
connectivity the assembler uses, and requires `Paths` to match it exactly: same path count,
same canonical order, same link count per path, and the same exact link references in every
position. This single structural comparison is what rejects a missing path, an extra path, a
duplicate path, a wrong path order, a foreign or equivalent-but-distinct link, a missing or
extra link inside a path, duplicate link coverage, a split of one maximal path, a merge of
disconnected paths, and a non-maximal path, all at once -- it never rematches or re-invokes
the assembler.

A continuity path is a derived, maximal, and fully recalculable sequence of exact
selected-match continuity links; it is not a persistent clash identity, a stable clash
identity, a Clash Ledger, or a persistent track, has no history or multi-run lifecycle, and
does not imply `Reopened`. A selected match with no continuity link never appears in any
path, and no zero-link path is ever created. `compare-index` consumes this assembly
indirectly through `DeterministicClashRunSequenceAnalyzer`; no HTML renderer consumes it, and
no CLI calls it directly outside that analyzer composition. Sequential real Navisworks
export validation remains unverified.

## Longitudinal sequence analysis orchestrator (Core, consumed by compare-index)

`IClashRunSequenceAnalyzer`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceAnalyzer.cs`) is the single Core
boundary that composes the existing longitudinal stages in caller-declared order:
`IClashRunSequenceComparer` -> `IClashRunSequenceContinuityProjector` ->
`IClashRunSequenceContinuityPathAssembler`. The caller's run order remains the only
authority; the analyzer never sorts, deduplicates, infers chronology, compares
non-adjacent runs, persists derived state, or creates history, a Clash Ledger,
multi-run lifecycle, `Reopened`, or stable/persistent clash identity.

`DeterministicClashRunSequenceAnalyzer`
(`src/OrzioClashReport.Core/Analysis/DeterministicClashRunSequenceAnalyzer.cs`) is the
current implementation. It receives the three ports through its constructor, rejects null
dependencies, rejects null `runs` before invoking any dependency, calls each stage exactly
once in order, passes the exact result reference from one stage to the next, and propagates
exceptions without wrapping them or returning a partial result. It is synchronous,
deterministic, and performs no I/O, clock access, network access, randomness, DI-container
resolution, matching, lifecycle classification, continuity projection, or path assembly of
its own.

`ClashRunSequenceAnalysisResult`
(`src/OrzioClashReport.Core/Model/ClashRunSequenceAnalysisResult.cs`) is the immutable
aggregate: the exact `SequenceComparison`, exact `ContinuityResult`, and exact
`ContinuityPathsResult` references from one coherent derived chain. Its internal
constructor rejects null inputs and rejects value-equivalent-but-distinct chains unless
`ReferenceEquals(ContinuityResult.SequenceComparison, SequenceComparison)` and
`ReferenceEquals(ContinuityPathsResult.ContinuityResult, ContinuityResult)` both hold. It
adds no ids, statuses, fingerprints, aggregate confidence, history, ledger, persistence
metadata, lifecycle aggregation, or aliases. `compare-index` consumes this analyzer after
loading every snapshot and before writing stdout; no HTML renderer consumes it.

## Longitudinal presentation model (Core, consumed by compare-index)

`IClashRunSequencePresentationProjector`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequencePresentationProjector.cs`) projects an
already-complete `ClashRunSequenceAnalysisResult` onto a lossless presentation view without recomputing
matching, lifecycle classification, continuity projection, or path assembly. It only indexes and relates
the exact references it is given, preserving run/comparison/entry/link/path order exactly.

`DeterministicClashRunSequencePresentationProjector`
(`src/OrzioClashReport.Core/Presentation/DeterministicClashRunSequencePresentationProjector.cs`) is the
sole current implementation, with a public parameterless constructor (no dependencies). For every
`ClashLifecycleResult` in `SequenceComparison.Comparisons` order, and every `ClashLifecycleEntry` in that
comparison's `Entries` order, it builds one presentation item; a selected match is associated with its
continuity path only by an exact `ClashRunMatchCandidate` reference lookup built from
`ContinuityPathsResult.Paths[*].SelectedMatches` -- never `RunId`, `CreatedAt`, GUID, value equality,
`ToString`, hash, or fingerprint. If the same exact selected-match reference were ever found in two
different paths (a structural impossibility given `ClashRunSequenceContinuityPathsResult`'s own disjoint
partition), it fails fast with `InvalidOperationException` instead of silently picking one.

Four new immutable model types (`src/OrzioClashReport.Core/Model/`), all with `internal` constructors:

- `ClashRunSequenceLifecycleEntryPresentation`: one lifecycle entry at its exact `(ComparisonIndex,
  EntryIndex)` position, with an optional exact `ContinuityPath` reference
  (`IsInContinuityPath => ContinuityPath != null`). Adds no id, status, or confidence of its own --
  `LifecycleEntry` stays the sole authority.
- `ClashRunSequenceTransitionPresentation`: one comparison plus the complete, ordered presentation of
  every one of its entries.
- `ClashRunSequenceContinuityPathPresentation`: one continuity path plus the presentation of the
  lifecycle entry behind each of its selected matches, in path order.
- `ClashRunSequencePresentationResult`: the aggregate. `Runs`/`Comparisons`/`ContinuityLinks`/
  `ContinuityPaths` are the exact canonical references from `AnalysisResult`. `Transitions`,
  `LifecycleEntries` (complete, no loss/duplication/reorder), `PathPresentations`,
  `StandaloneSelectedMatches`, and `NonPathLifecycleEntries` are the indexed presentation views. Every
  selected match belongs to exactly one of `PathPresentations[*].SelectedMatchEntries` or
  `StandaloneSelectedMatches`; every entry outside a path (including every unmatched `New`, `Resolved`,
  and `Unverifiable` entry) is in `NonPathLifecycleEntries`; `StandaloneSelectedMatches` is an exact
  subset of it. Standalone and non-path views reuse the exact same presentation item references as
  `LifecycleEntries`, never copies. Twelve derived counters (`RunCount`, `AdjacentComparisonCount`,
  `SelectedMatchCount`, `ContinuityLinkCount`, `ContinuityPathCount`, `StandaloneSelectedMatchCount`,
  `LifecycleEntryCount`, `NonPathLifecycleEntryCount`, `StillOpenCount`, `NewCount`, `ResolvedCount`,
  `UnverifiableCount`) are computed exclusively from the already-projected collections, never supplied by
  the caller.

Canonical order: `Transitions` by `ComparisonIndex` ascending; `LifecycleEntries` by `ComparisonIndex`
ascending then `EntryIndex` ascending; `PathPresentations` by `ContinuityPathsResult.Paths` order;
`SelectedMatchEntries` of each path by `SelectedMatchContinuityPath.SelectedMatches` order;
`StandaloneSelectedMatches` and `NonPathLifecycleEntries` by the order they appear in the global
`LifecycleEntries` list. Never by status, confidence, `RunId`, `CreatedAt`, GUID, or path length.

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

## Two-run comparison CLI

The legacy single-XML HTML command remains supported.
The compare command receives previous/current XML and manifest paths explicitly; it never infers chronological order from `CreatedAt`, `RunId`, revision, or filenames.
The CLI is only the composition root: XML source → manifest source → coordination-run assembler → matcher → run comparer → lifecycle classifier.
`Program.cs` never recreates matching or lifecycle rules.
Compare mode writes the same deterministic eleven-line console summary as before and
may optionally write the revision-aware lifecycle HTML with `-o`/`--output`.
The same run or same file may be supplied in both roles for synthetic smoke testing; this is not sequential real-model validation.

## Language

Code, identifiers, comments, and commit messages in **English**. Conversation with the
author may be in Portuguese.
