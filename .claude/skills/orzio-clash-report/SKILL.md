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
`DeterministicAdjacentClashRunSequenceComparer.Compare` (adjacent pairs `[i] -> [i + 1]` only,
via `ConservativeClashMatcher` -> `DeterministicClashRunComparer` ->
`ConservativeClashLifecycleClassifier` for each pair) ->
`ClashRunSequenceComparisonResult` ->
the existing deterministic eleven-line pairwise summary reused once per adjacent transition.
The command is `orzioclash compare-index --index <run-index.json>`.
The run-index order remains the sole sequence authority: the CLI never reorders by
`CreatedAt`, `RunId`, revision, filename, or filesystem metadata. Duplicate references are
preserved exactly as declared, so adjacent duplicate snapshots remain valid comparisons.
Every snapshot is loaded before output, the Core sequence comparer computes every adjacent
comparison before output, and the command is console-only in this stage: no `-o`/`--output`,
no lifecycle HTML, no history JSON, and no persisted derived state. Explicit ordered index
consumption and adjacent-pair traversal now exist; automatic discovery, chronology
inference, latest/previous lookup, non-adjacent/all-vs-all comparison, multi-run lifecycle,
Clash Ledger, `Reopened`, and persistent clash identity still do not.

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

## Selected-match continuity projection (Core-only, not yet wired into any CLI)

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
Links are derived and recalculable, never persisted. No CLI command and no HTML renderer
consumes this projection yet -- `compare-index` stdout is unchanged and `Program.cs` is
untouched by it. Sequential real Navisworks export validation remains unverified.

## Deterministic maximal continuity path assembly (Core-only, not yet wired into any CLI)

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
path, and no zero-link path is ever created. This is Core-only: no CLI command and no HTML
renderer consumes it yet, `compare-index`'s stdout is unchanged, and `Program.cs` is
untouched. Sequential real Navisworks export validation remains unverified.

## Longitudinal sequence analysis orchestrator (Core-only, not yet wired into any CLI)

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
metadata, lifecycle aggregation, or aliases. No CLI command and no HTML renderer consumes
this analyzer yet.

## Anti-patterns that fail review

- Core with a `using System.Xml.Linq`, a Navisworks reference, or any HTML string.
- Core retargeted to `net8.0` "to use newer C#". This breaks the future 4.8 adapter.
- One project that both parses XML and renders HTML.
- A mutable `ClashResult` filled in field by field across the codebase.
- A `catch { }` or a `catch (Exception) { return null; }` with no log.
- Non-deterministic output that breaks the golden-file test, then deleting the test to "fix" it.
- Building the API adapter, images, or licensing before the MVP is validated on a real model.
