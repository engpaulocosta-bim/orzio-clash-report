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
"no persistence/history/database" scope gate: single-run snapshot persistence exists; run
collections, indexes, history traversal, ledgers, databases, and any CLI snapshot workflow
still do not.

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

## Two-run comparison CLI

The legacy single-XML HTML command remains supported.
The compare command receives previous/current XML and manifest paths explicitly; it never infers chronological order from `CreatedAt`, `RunId`, revision, or filenames.
The CLI is only the composition root: XML source → manifest source → coordination-run assembler → matcher → run comparer → lifecycle classifier.
`Program.cs` never recreates matching or lifecycle rules.
Compare mode currently writes a deterministic console summary only.
No revision-aware HTML is produced yet.
The same run or same file may be supplied in both roles for synthetic smoke testing; this is not sequential real-model validation.

## Language

Code, identifiers, comments, and commit messages in **English**. Conversation with the
author may be in Portuguese.
