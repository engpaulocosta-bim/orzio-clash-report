# OrzioClashReport

OrzioClashReport reads clashes exported from Navisworks Clash Detective and supports two
complementary workflows: a single-run HTML report grouped by clash test, discipline pair,
and level, and a revision-aware comparison between two coordination runs with a
deterministic console summary and optional lifecycle HTML.

Current source and package candidate version: `0.1.0-preview.3` for Windows `win-x64`.
Release availability is determined by the matching Git tag and GitHub prerelease. This
README describes the contents and behavior of version `0.1.0-preview.3` without asserting
its current publication state. This remains an internal controlled pilot, not a public
release, and not a fully validated longitudinal MVP. Single-run
parsing, grouping, and HTML were human-validated on one private real export. Longitudinal
matching, lifecycle, continuity links, continuity paths, and longitudinal HTML have not
been validated on three real historical exports and remain experimental.

The project-catalog workflow is included in version `0.1.0-preview.3`.
The identity-governance workflow is included in version `0.1.0-preview.3`.

## Architecture

The project follows Ports and Adapters, also known as hexagonal architecture. The core
targets `netstandard2.0`, has no third-party dependencies, and knows nothing about input
sources or output formats. Pluggable adapters live at the edges: the Clash Detective XML
parser is an input adapter, and the HTML renderer is an output adapter. This keeps the
domain stable when the data source changes later, for example to the Navisworks API, or
when another output format is added.

### Desktop launcher

`v0.2.0-launcher-preview.1` adds a Windows desktop application around the existing engine,
in four `net8.0` projects: `OrzioClashReport.Launcher.Contracts` (dependency-free DTOs and
ports), `.Application` (pure launcher policy), `.Infrastructure` (process, filesystem and
hashing adapters) and `.Desktop` (Avalonia UI, manual composition root, no dependency
injection container).

The launcher sits strictly outside the engine and depends on it only through the published
CLI contracts. No engine project references the launcher, Core stays `netstandard2.0`, and
`LauncherArchitectureTests` fails the build if either boundary is crossed. The CLI remains
available and unchanged.

The launcher never assembles a command line: arguments are passed element by element with
no shell intermediary, every `-o` destination is absolute, and the working directory is
never the installation directory. It verifies the engine's SHA-256 against the packaged
`engine-manifest.json` before running it, refuses to replace a snapshot, run index, project
catalog or governance document, and asks for an explicit decision before replacing a
derived HTML report.

See [docs/operations/desktop-pilot.md](docs/operations/desktop-pilot.md) for the private
pilot guide.

## Usage

Requires the .NET SDK pinned in `global.json` (8.0.420).

```bash
dotnet build
dotnet test
```

## Binary Quick Start

The internal preview release ships as a Windows `win-x64` self-contained executable:
`orzioclash.exe`. It is not a cross-platform binary. Other operating systems can still run
from source with the .NET SDK.

After downloading and verifying the ZIP, run the tool without the source repository:

```powershell
.\orzioclash.exe --version
.\orzioclash.exe --help
.\orzioclash.exe ".\inputs\clash-export.xml" -o ".\reports\single-run.html"
```

Project-catalog quick start from the packaged binary:

```powershell
.\orzioclash.exe create-project --project-id example-project --name "Example Coordination Project" --index ".\run-index.json" --report ".\reports\project-longitudinal.html" -o ".\project.json"
.\orzioclash.exe append-project-snapshot --project ".\project.json" --snapshot ".\snapshots\run-004.json"
.\orzioclash.exe render-project --project ".\project.json"
```

Identity-governance quick start from the packaged binary:

```powershell
.\orzioclash.exe create-identity-governance --project-id coordination-project -o ".\identity-governance.json"
.\orzioclash.exe append-identity-decision --governance ".\identity-governance.json" --decision-id decision-001 --decision-kind ConfirmSameIdentity --left-run-id run-001 --left-occurrence-index 0 --right-run-id run-002 --right-occurrence-index 0 --persistent-identity-id identity-001 --reviewer-alias coordinator-a --reason "Confirmed from review"
.\orzioclash.exe validate-identity-governance --project ".\project.json" --governance ".\identity-governance.json"
.\orzioclash.exe render-identity-governance-report --project ".\project.json" --governance ".\identity-governance.json" -o ".\reports\identity-governance-review.html"
```

For operational steps, checksum verification, manifest preparation, snapshot creation,
run-index creation, and longitudinal output, see
[docs/operations/internal-preview.md](docs/operations/internal-preview.md). For release
gates, see [docs/operations/release-checklist.md](docs/operations/release-checklist.md).
For controlled pilot execution and feedback, see
[docs/operations/pilot-evaluation.md](docs/operations/pilot-evaluation.md).

Persistent clash identity exists only through an explicit human `ConfirmSameIdentity`
decision carrying a `persistentIdentityId`. The packaged preview does not assign identity
automatically, propagate identity, infer transitivity, build a project-wide identity graph
or Clash Ledger, integrate persistent identities into longitudinal lifecycle, provide
`Reopened`, infer chronology automatically, or assign clash responsibility automatically.

## Identity Governance Workflow

Version history:

- `v0.1.0-preview.2` did not package the identity-governance workflow.
- `v0.1.0-preview.3` does package the identity-governance workflow for an internal
  controlled pilot.

The packaged preview.3 workflow includes:

- `ClashEvidenceEndpoint`, `HumanIdentityDecision`, and `IdentityGovernanceDocument`
- Strict schema-v1 JSON adapter `OrzioClashReport.Persistence.IdentityGovernanceJson`
- `create-identity-governance` for creating one empty governance document
- `append-identity-decision` for appending one explicit human decision to an existing
  governance file through safe replace-existing persistence
- `validate-identity-governance` for read-only evidence validation of a governance
  document's project binding and every decision's `runId` + `occurrenceIndex` evidence
  endpoints against one project's indexed, immutable snapshots
- `render-identity-governance-report` for rendering one standalone, self-contained HTML
  review of already-validated human decisions without changing the project catalog,
  snapshots, governance document, or longitudinal report

Snapshots remain immutable evidence. The identity-governance CLI does not infer or
propagate identity, is not an interactive review tool, is not a Clash Ledger, and does not
project decisions into the longitudinal report. `validate-identity-governance` is
read-only: it never writes, replaces, or creates any file, and it validates only project
binding and evidence existence -- never matcher candidacy, run adjacency, left/right
ordering intent, transitivity, graph conflicts, identity merges, reopening, or
responsibility. The standalone review report remains derived and regenerable, does not
project raw `ClashObject.SourceModel`, and does not alter the longitudinal report already
referenced by a project catalog.

Source example:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  create-identity-governance \
  --project-id coordination-project \
  -o identity-governance.json

dotnet run --project src/OrzioClashReport.Cli -- \
  append-identity-decision \
  --governance identity-governance.json \
  --decision-id decision-001 \
  --decision-kind ConfirmSameIdentity \
  --left-run-id run-001 \
  --left-occurrence-index 4 \
  --right-run-id run-002 \
  --right-occurrence-index 7 \
  --persistent-identity-id identity-001 \
  --reviewer-alias coordinator-a \
  --reason "Confirmed from model context"

dotnet run --project src/OrzioClashReport.Cli -- \
  validate-identity-governance \
  --project project.json \
  --governance identity-governance.json

dotnet run --project src/OrzioClashReport.Cli -- \
  render-identity-governance-report \
  --project project.json \
  --governance identity-governance.json \
  -o reports/identity-governance.html
```

Operational details for the authoring workflow live in
[docs/operations/identity-governance-cli.md](docs/operations/identity-governance-cli.md).
Operational details for the read-only evidence-validation workflow live in
[docs/operations/identity-governance-validation.md](docs/operations/identity-governance-validation.md).
Operational details for the standalone review report live in
[docs/operations/identity-governance-review-report.md](docs/operations/identity-governance-review-report.md).

Legal distribution terms remain an owner decision.

Generate a report from a Clash Detective XML export:

```bash
dotnet run --project src/OrzioClashReport.Cli -- <input.xml> -o <output.html>
```

Example with the fixtures in `samples/`:

```bash
dotnet run --project src/OrzioClashReport.Cli -- samples/sample-clash.xml -o report.html
```

The console output shows the raw-vs-grouped count, for example:

```text
1458 raw clashes -> 25 groups
Report written to report.html
```

Open the generated `report.html` in any browser. It is a single self-contained file with no
external CSS or JavaScript.

## Compare Two Coordination Runs

The `compare` command receives explicit previous/current roles. Without output, it prints
the deterministic console summary. With `-o`/`--output`, it prints the same summary and also
writes a self-contained revision-aware lifecycle HTML report:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  compare \
  --previous-xml <previous.xml> \
  --previous-manifest <previous.json> \
  --current-xml <current.xml> \
  --current-manifest <current.json> \
  -o comparison.html
```

The CLI composition pipeline is:

1. Previous/current are explicit command-line roles.
2. The CLI never reorders runs by timestamp, `RunId`, revision, or file name.
3. Each XML is parsed independently by `NavisworksXmlClashSource`.
4. Each manifest is loaded independently by `JsonRunManifestSource`.
5. `ExactSourceModelCoordinationRunAssembler` resolves `SourceModel` to `ModelRevision`.
6. `ConservativeClashMatcher` evaluates candidate relationships pair by pair.
7. `DeterministicClashRunComparer` selects a deterministic one-to-one subset.
8. `ConservativeClashLifecycleClassifier` produces the final statuses.
9. The console shows deterministic counts for candidates, matches, and lifecycle.
10. `-o`/`--output` is optional. Without output, only the summary is printed; with output,
    that same summary is followed by revision-aware lifecycle HTML.
11. Single-run snapshot persistence already exists, persisted snapshots can be compared
    explicitly with `compare-snapshots`, an explicit ordered run-index JSON can be created
    with `index-snapshots`, and that index can be consumed explicitly with `compare-index`.
    Index order remains the only sequence authority. There is no automatic discovery,
    chronological inference, ledger, or persisted multi-run lifecycle.
12. Comparing the same fixture on both sides is only a synthetic smoke test, not real
    sequential validation.

Compare two persisted snapshots without reprocessing XML or reloading manifests:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  compare-snapshots \
  --previous-snapshot <previous.json> \
  --current-snapshot <current.json> \
  -o comparison.html
```

This flow loads two persisted `CoordinationRun` snapshots, preserves the explicit
previous/current command-line roles, recalculates matching and lifecycle from immutable
evidence, and optionally writes the same revision-aware HTML used by `compare`. It does not
create a run collection, run index, history traversal, ledger, `Reopened`, or persistent
clash ID.

Create an explicit ordered run index from persisted snapshots:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  index-snapshots \
  --snapshot <run-001.json> \
  --snapshot <run-002.json> \
  -o run-index.json
```

This flow loads each supplied snapshot only to validate existence and contract, preserves
the exact CLI order of `--snapshot`, converts each path to a canonical relative reference
with `/`, and persists a run-index JSON containing only `schemaVersion` and `snapshotPaths`.
The index does not persist matching, lifecycle, run metadata, or any stable clash identity.

Consume an explicit run index and compare only adjacent transitions:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  compare-index \
  --index run-index.json \
  -o longitudinal-report.html
```

Contract rules:

1. `--index` is required and unique.
2. `JsonRunIndexSerializer.Load` is the only authority for loading the index.
3. `snapshotPaths` order is the only authoritative sequence.
4. Each reference is resolved by `RunIndexSnapshotPathResolver.ResolveReference`.
5. Each resolved snapshot is loaded by `JsonCoordinationRunSnapshotSerializer.Load`.
6. All snapshots are loaded before any output is written.
7. The CLI composes the pipeline explicitly in this order:
   `ConservativeClashMatcher` -> `DeterministicClashRunComparer` ->
   `ConservativeClashLifecycleClassifier` -> `DeterministicAdjacentClashRunSequenceComparer` ->
   `DeterministicSelectedMatchContinuityProjector` ->
   `DeterministicSelectedMatchContinuityPathAssembler` ->
   `DeterministicClashRunSequenceAnalyzer` ->
   `DeterministicClashRunSequencePresentationProjector`.
8. All snapshots are loaded, all adjacent comparisons are computed, continuity is
   projected, paths are assembled, analysis is completed, and presentation is projected
   before the first stdout line.
9. Pairs are exactly `[i] -> [i + 1]`, preserving duplicates and declared order.
10. Matching and lifecycle are recalculated independently for each adjacent transition.
11. The command writes the deterministic 12-line longitudinal summary first and then reuses
    the existing deterministic 11-line pairwise summary from `compare` and
    `compare-snapshots`, once per adjacent transition.
12. `compare-index` accepts optional `-o`/`--output` for self-contained longitudinal HTML.
    Without output, stdout remains byte-identical to the previous contract. With output,
    the only extra line is `Longitudinal report written to {OutputPath}`, emitted last and
    only after the file has been written successfully.
13. HTML rendering and writing, when requested, finish before the first stdout line.
14. There is no automatic discovery, chronological inference, latest/previous lookup,
    non-adjacent comparison, all-vs-all comparison, Clash Ledger, `Reopened`, persistent
    clash ID, path ID, fingerprint, aggregate path status/confidence, or persisted derived
    state.

The `compare-index` longitudinal prefix has exactly these 12 lines, in this order:

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

After that prefix, existing pairwise blocks remain unchanged and are emitted in transition
order as `Comparison {i + 1}/{AdjacentComparisonCount}` plus the 11-line pairwise summary
for that adjacent transition.

## Project Catalog Workflow

This workflow is included in version `0.1.0-preview.3`. This section describes the
contents and behavior of version `0.1.0-preview.3` without asserting its current
publication state.

Create an operational project catalog from an existing run index:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  create-project \
  --project-id example-project \
  --name "Example Coordination Project" \
  --index run-index.json \
  --report reports/longitudinal.html \
  -o project.json
```

Render the longitudinal HTML again from the immutable snapshots referenced by that project
catalog:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  render-project \
  --project project.json
```

Append one new persisted snapshot to the end of the existing project run index:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  append-project-snapshot \
  --project project.json \
  --snapshot snapshots/run-004.json
```

Contract rules:

1. The project catalog is operational state only. It stores project metadata, one run-index
   reference, and one longitudinal-report destination.
2. Run snapshots remain immutable evidence.
3. Run index remains the only order authority.
4. The report path is only a regenerable derived-artifact destination.
5. `append-project-snapshot` preserves every existing run-index reference exactly as loaded,
   appends exactly one new reference at the end, allows duplicates, and never reorders,
   deduplicates, removes, or silently normalizes earlier entries.
6. `append-project-snapshot` updates only the run index. It does not overwrite the project
   catalog, mutate any snapshot, or regenerate the report automatically. Run
   `render-project` separately when you want refreshed HTML.
7. Matching, lifecycle, continuity links, continuity paths, and presentation are always
   recalculated and are never persisted into the project catalog.
8. There is still no persistent clash identity, Clash Ledger, `Reopened`, database, or
   automatic chronology.

For a recommended layout and operational notes, see
[docs/operations/project-catalog.md](docs/operations/project-catalog.md).

### Adjacent Run Sequence Comparer

`IClashRunSequenceComparer` (`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceComparer.cs`)
formalizes the adjacent traversal in Core. It takes an already explicitly ordered
`IReadOnlyList<CoordinationRun>` from the caller. Core does not know about run-index JSON or
any other persistence format. It compares only `[i] -> [i + 1]` pairs, never non-adjacent or
reversed pairs.

`DeterministicAdjacentClashRunSequenceComparer` is the only current implementation. Its
constructor receives an `IClashRunComparer` and an `IClashLifecycleClassifier`; for each
adjacent pair, it calls the injected run comparer and then the injected lifecycle
classifier, with no propagation of selected matches, confidence, or evidence across
transitions. It requires at least two runs, rejects a null sequence, and rejects any null
entry. Duplicate run references, for example `A, A, B`, are preserved and never
deduplicated. Traversal is synchronous, sequential, and fail-fast: an exception from either
injected dependency on any pair propagates immediately, and no partial
`ClashRunSequenceComparisonResult` is returned.

`ClashRunSequenceComparisonResult` is the immutable output: ordered `Runs` plus one
`ClashLifecycleResult` per adjacent transition in `Comparisons`, in the same order. It
validates only structural continuity: every `Comparisons[i]` must reference `Runs[i]` and
`Runs[i + 1]` by exact object reference, not by `RunId`, `CreatedAt`, or value equality, as
its previous/current sides. It never recomputes matching or lifecycle. It represents only
an ordered collection of independently recalculated adjacent pairwise lifecycle results:
there is no history, multi-run lifecycle, persistent clash identity, Clash Ledger, or
`Reopened`. `compare-index` is the only current consumer; `compare` and `compare-snapshots`
remain pairwise and keep using the existing `CreateDerivedComparison` helper in
`Program.cs`.

### Selected-Match Continuity Projection

`IClashRunSequenceContinuityProjector`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceContinuityProjector.cs`) projects
an already-derived `ClashRunSequenceComparisonResult` onto the set of
`SelectedMatchContinuityLink`s that exist at consecutive comparison boundaries. At boundary
`i`, between `Comparisons[i]` and `Comparisons[i + 1]`, sharing the run at `Runs[i + 1]`, a
link exists whenever a selected match's `CurrentIndex` enters an occurrence slot and a
selected match's `PreviousIndex` leaves that exact same slot. The projector knows nothing
about run-index JSON, does not load snapshots, and calls no matcher, comparer, classifier,
sequence comparer, or path assembler.

`DeterministicSelectedMatchContinuityProjector` is the only current implementation and has
a public parameterless constructor. It considers only `ClashRunMatchResult.SelectedMatches`.
`Candidates`, `AlternativeCandidates`, `UnmatchedPrevious`, and `UnmatchedCurrent` never
create a link, and `ClashLifecycleStatus` never filters projection. A selected match
classified `Unverifiable` may still produce a link. Only consecutive boundaries are
considered. Duplicate run references and duplicate `RunId` values are never deduplicated.

`SelectedMatchContinuityLink` observes only that one selected match enters an exact
occurrence slot of a shared run and another selected match leaves that exact same slot
through the immediately following comparison. It stores `IncomingComparisonIndex` and
`SharedOccurrenceIndex`; `OutgoingComparisonIndex` and `SharedRunIndex` are derived as
`IncomingComparisonIndex + 1`. It validates exact slot and exact object-reference
continuity. Value-shaped equivalence at a different slot never satisfies it. It carries no
identifier, fingerprint, status, or aggregate confidence.

`ClashRunSequenceContinuityResult` is the immutable output: the exact
`SequenceComparison` reference plus the complete canonically ordered set of `Links`, sorted
by `IncomingComparisonIndex` and then `SharedOccurrenceIndex`. It independently revalidates
every link's exact membership, shared-run reference, shared-slot continuity, and
completeness by recomputing the full expected set from `SequenceComparison` alone. That
structural validation rejects missing links, extra links, duplicate links, and non-canonical
order at once. It never rematches.

This is the smallest useful longitudinal observation and stops well before clash identity:
a link never asserts that the underlying clash is the same clash. Links are derived,
fully recalculable, and never persisted. `compare-index` consumes this projection
indirectly through `DeterministicClashRunSequenceAnalyzer`; no HTML renderer consumes it
directly, and no CLI calls it directly outside that analyzer composition. Sequential real
Navisworks export validation remains unverified.

### Maximal Continuity Path Assembly

`IClashRunSequenceContinuityPathAssembler`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceContinuityPathAssembler.cs`)
assembles an already-derived `ClashRunSequenceContinuityResult` into the complete set of
disjoint maximal continuity paths implied by its links. Two links belong to the same path
only when the first link's `OutgoingSelectedMatch` is the exact same object reference as
the second link's `IncomingSelectedMatch` at the immediately following comparison boundary
(`next.IncomingComparisonIndex == current.OutgoingComparisonIndex`). The assembler knows no
JSON, snapshot, or filesystem and calls no matcher, run comparer, classifier, sequence
comparer, continuity projector, or presentation projector.

`DeterministicSelectedMatchContinuityPathAssembler` is the only current implementation and
has a public parameterless constructor. For each link in canonical
`ContinuityResult.Links` order, it checks for an exact predecessor. A link with no
predecessor starts a new path, which then follows exact successors until none remains.
Connectivity never uses `RunId`, `CreatedAt`, candidate indices alone, occurrence
reference alone, candidate or assessment value equality, source clash GUID, confidence,
evidence, `ToString`, hash, or fingerprint. It uses only exact selected-match object
reference identity. Zero links produce zero paths. If more than one exact predecessor or
successor is found, the assembler throws `InvalidOperationException` instead of silently
choosing one.

`SelectedMatchContinuityPath`
(`src/OrzioClashReport.Core/Model/SelectedMatchContinuityPath.cs`) is an immutable maximal
sequence of `SelectedMatchContinuityLink`s connected only by that exact-reference rule. Its
internal constructor rejects null or empty links, null link slots, repeated or reversed
boundaries, boundary gaps, and distinct but value-equivalent candidate references at any
join. `SelectedMatches` is derived, never supplied: `Links[0].IncomingSelectedMatch`
followed by each link's `OutgoingSelectedMatch`, so `SelectedMatches.Count == Links.Count +
1`. `StartComparisonIndex`, `EndComparisonIndex`, `StartRunIndex`, and `EndRunIndex` are
derived from the first and last links. The path carries no ID, status, fingerprint, or
aggregate confidence.

`ClashRunSequenceContinuityPathsResult`
(`src/OrzioClashReport.Core/Model/ClashRunSequenceContinuityPathsResult.cs`) is the
immutable output: the exact `ContinuityResult` reference plus the complete canonically
ordered set of `Paths`. Canonical order is the position of each path's first link in
`ContinuityResult.Links`, never path length, `RunId`, `CreatedAt`, confidence, source GUID,
or occurrence detail. It revalidates the complete maximal partition from
`ContinuityResult.Links` alone and requires `Paths` to match exactly. That structural check
rejects missing paths, extra paths, duplicate paths, wrong path order, foreign or
equivalent-but-distinct links, missing or extra links inside a path, duplicate link
coverage, split maximal paths, merged disconnected paths, and non-maximal paths. It never
rematches or reinvokes the assembler.

A continuity path is a derived, maximal, fully recalculable sequence of exact selected-match
continuity links. It is not persistent clash identity, stable clash identity, a Clash
Ledger, or a persistent track; it has no history, no multi-run lifecycle, and no implication
of `Reopened`. A selected match with no continuity link never appears in any path, and no
empty path is created. `compare-index` consumes this assembly indirectly through
`DeterministicClashRunSequenceAnalyzer`. Sequential real Navisworks export validation
remains unverified.

### Longitudinal Sequence Analyzer

`IClashRunSequenceAnalyzer`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequenceAnalyzer.cs`) is the single Core
boundary that composes, in caller-declared order:
`IClashRunSequenceComparer` -> `IClashRunSequenceContinuityProjector` ->
`IClashRunSequenceContinuityPathAssembler`. The received order remains the only authority.
The analyzer does not sort, deduplicate, infer chronology, compare non-adjacent runs,
persist derived state, or create history, Clash Ledger, multi-run lifecycle, `Reopened`, or
stable/persistent clash identity.

`DeterministicClashRunSequenceAnalyzer`
(`src/OrzioClashReport.Core/Analysis/DeterministicClashRunSequenceAnalyzer.cs`) receives the
three ports in its constructor, rejects null dependencies, rejects null `runs` before
calling any dependency, calls each stage exactly once in the defined order, passes the exact
result reference from one stage to the next, and propagates exceptions without wrapping or
returning partial results. It is synchronous and deterministic and performs no I/O, clock,
network, randomness, DI container work, matching, lifecycle classification, continuity
projection, or path assembly by itself.

`ClashRunSequenceAnalysisResult`
(`src/OrzioClashReport.Core/Model/ClashRunSequenceAnalysisResult.cs`) is the immutable
aggregate. It preserves the exact references to `SequenceComparison`, `ContinuityResult`,
and `ContinuityPathsResult` from one coherent derived chain. Its internal constructor
rejects nulls and rejects value-equivalent chains made of different references by requiring
`ReferenceEquals(ContinuityResult.SequenceComparison, SequenceComparison)` and
`ReferenceEquals(ContinuityPathsResult.ContinuityResult, ContinuityResult)`. It adds no IDs,
status, fingerprint, aggregate confidence, history, ledger, persistence metadata, aggregate
lifecycle, or aliases. `compare-index` consumes this analyzer after loading all snapshots
and before writing stdout.

### Longitudinal Presentation Model

`IClashRunSequencePresentationProjector`
(`src/OrzioClashReport.Core/Abstractions/IClashRunSequencePresentationProjector.cs`)
projects a complete `ClashRunSequenceAnalysisResult` into a lossless presentation view:
ordered runs, adjacent comparisons, continuity links, continuity paths, all lifecycle
entries, and the entries or selected matches that are outside any path. It does this
without recalculating matching, lifecycle, continuity projection, or path assembly. The
projection only indexes and relates the exact references it receives, preserving existing
order.

`DeterministicClashRunSequencePresentationProjector`
(`src/OrzioClashReport.Core/Presentation/DeterministicClashRunSequencePresentationProjector.cs`)
is the only current implementation and has a public parameterless constructor. Association
between a selected match and its continuity path uses only `ReferenceEquals` over
`ContinuityPathsResult.Paths[*].SelectedMatches`, never `RunId`, `CreatedAt`, GUID, value
equality, `ToString`, hash, or fingerprint. If the same exact selected match appears in two
different paths, projection fails fast with `InvalidOperationException` instead of silently
choosing one.

Four immutable types carry the presentation, all with `internal` constructors:

- `ClashRunSequenceLifecycleEntryPresentation`: a lifecycle entry at its exact
  `ComparisonIndex`/`EntryIndex`, with an optional exact reference to its continuity path
  (`IsInContinuityPath`). It adds no status, confidence, or identity of its own; the
  original entry remains the authority.
- `ClashRunSequenceTransitionPresentation`: one comparison plus the complete ordered
  presentation of all its entries.
- `ClashRunSequenceContinuityPathPresentation`: one continuity path plus the presentation
  of the lifecycle entry behind each selected match in path order.
- `ClashRunSequencePresentationResult`: the aggregate. `Runs`, `Comparisons`,
  `ContinuityLinks`, and `ContinuityPaths` are the exact canonical references from
  `AnalysisResult`. `Transitions`, `LifecycleEntries`, `PathPresentations`,
  `StandaloneSelectedMatches`, and `NonPathLifecycleEntries` are indexed views. Every
  selected match belongs to exactly one group: path or standalone. Every entry outside a
  path, including `New`, `Resolved`, and unmatched `Unverifiable` entries, is included in
  `NonPathLifecycleEntries`, of which `StandaloneSelectedMatches` is an exact subset.
  Standalone and non-path views reuse the same presentation item references from
  `LifecycleEntries`; they are not copies. Twelve derived counts are calculated only from
  projected collections and are never supplied by the caller.

Canonical order: `Transitions` by increasing `ComparisonIndex`; `LifecycleEntries` by
increasing `ComparisonIndex` and then `EntryIndex`; `PathPresentations` by
`ContinuityPathsResult.Paths` order; each path's `SelectedMatchEntries` by
`SelectedMatchContinuityPath.SelectedMatches` order; `StandaloneSelectedMatches` and
`NonPathLifecycleEntries` by their order in the global `LifecycleEntries` list. Ordering is
never by status, confidence, `RunId`, `CreatedAt`, GUID, or path length.

This is Core presentation derived from a complete analysis chain, not history, ledger, or
persistent/stable clash identity. A displayed continuity path is still only a maximal
sequence derived from exact-reference links, never a persistent clash. `compare-index` uses
this projection for the deterministic 12-line longitudinal stdout prefix and, when
`-o`/`--output` is provided, for the longitudinal HTML renderer. There is still no persisted
derived state, aggregate multi-run lifecycle, Clash Ledger, persistent clash ID, or
`Reopened`. Sequential validation against real Navisworks exports remains unverified.

### Self-Contained Longitudinal HTML

`HtmlLongitudinalClashReportRenderer` lives in `OrzioClashReport.Output.Html` and has one
public contract: `Render(ClashRunSequencePresentationResult result)`. It consumes the
complete `ClashRunSequencePresentationResult` it receives. It never recalculates matching,
lifecycle, links, paths, partitions, or counts, and it does not call the analyzer,
comparer, classifier, continuity projector, path assembler, or presentation projector. The
method is synchronous and performs no I/O, clock, network, randomness, or filesystem reads;
`compare-index` remains responsible for `File.WriteAllText`.

The generated document is HTML5, `lang="en"`, UTF-8, titled `Orzio Clash Longitudinal
Report`, byte-for-byte deterministic, self-contained, responsive, printable, with all CSS
inline in one `<style>`, no JavaScript, and no external links, fonts, images, stylesheets,
or assets. All dynamic content is HTML-encoded. The renderer may show `RunId`, `CreatedAt`
in `"O"` format with invariant culture, counts, declared models in order,
Company/Discipline/ModelName/Revision/SourceFileName, clash tests and clashes, elements,
levels, distance, point, source clash GUID labeled as evidence only, confidence, lifecycle
evidence, and match evidence with previous/current values. It does not show
`ModelRevision.SourceFilePath`, `ModelRevision.ContentHash`, `ClashObject.Properties`,
local or network paths, invented fields, generation timestamps, or the index path.

The nine main sections always appear in this order and with stable classes:
`longitudinal-header`, `longitudinal-summary-section`, `interpretation-warning`,
`run-sequence-section`, `continuity-paths-section`, `standalone-selected-matches-section`,
`non-path-lifecycle-section`, `transition-sections`, and
`longitudinal-classification-note`. The renderer preserves the received order of runs,
paths, selected matches in paths, standalone selected matches, non-path lifecycle entries,
transitions, lifecycle entries, and evidence. Visual ordinals are presentation positions,
never IDs.

The HTML presents the explicit run sequence, declared revisions, longitudinal summary,
interpretation warnings, maximal continuity paths, standalone selected matches, lifecycle
entries outside paths, and all adjacent transitions with `New`, `StillOpen`, `Resolved`,
`Unverifiable`, confidence, lifecycle evidence, and match evidence. The copy makes clear
that continuity paths are derived from selected matches recalculated in adjacent
comparisons, do not prove identity, are recalculable, and are not persisted; `High` is not
human confirmation; `Unverifiable` means insufficient evidence or candidate competition;
source GUID is evidence only; and there is still no Clash Ledger, `Reopened`, aggregate
multi-run lifecycle, persistent clash identity, fingerprint, path ID, aggregate path
status, or aggregate path confidence.

The revision-aware HTML presents:

1. previous/current run metadata;
2. model revisions declared in the manifest;
3. lifecycle summary;
4. matching summary;
5. one card per `ClashLifecycleEntry`;
6. previous/current occurrence evidence;
7. selected-match confidence;
8. lifecycle evidence;
9. match evidence.

Important limits of the revision-aware flow:

1. `High` confidence is not human confirmation.
2. Source clash GUID is displayed only as evidence, not as stable identity.
3. `Reopened` does not exist yet.
4. Persistent clash ID does not exist yet.
5. Single-run snapshot persistence already exists as immutable evidence, along with
   explicit snapshot comparison, explicit ordered run-index creation, and adjacent
   traversal over that index; there is still no ledger, history, multi-run lifecycle,
   `Reopened`, or persistent clash ID.

### Group Identity

A group (`ClashGroup`) is identified by the combination of three facts, in this order:

1. **Clash test** (`<clashtest name="...">` in the export): clashes from different clash
   tests are never mixed into the same group, even if they have the same discipline pair
   and level.
2. **Discipline pair**, normalized independently from A/B order.
3. **Level (`LevelKey`)**: when both clash elements are on the same level, that level is
   used; when only one side has a level, that level is used; when both sides have different
   levels, the result is the stable `LevelA x LevelB` combination, independent from order;
   when neither side has a level, the group has no level.

The clash test name appears in `ClashGroup.ClashTestName`, in the stable group key, and in
the generated HTML report.

### Discipline Resolution

Grouping by discipline pair uses `PathHierarchyDisciplineResolver` in
`OrzioClashReport.Core` as the default heuristic: it tries the nested NWD model name from
the export `pathlink` and falls back to the `Item Source File Name` property when absent.
Because discipline naming varies by project, this is a pluggable `IDisciplineResolver`
implementation. Replace it if the heuristic does not match a project's conventions.

## Samples

The fixtures in `samples/` (`sample-clash.xml`, `sample-clash2.xml`) contain synthetic,
anonymized data. Real project names, company names, network paths, and file names were
replaced with fictitious values while preserving the XML structure and relationships needed
by parsing and grouping tests. See [samples/README.md](samples/README.md).

## Validation Status

- **Compiles**: `dotnet build -c Release` passes without warnings.
- **Runs**: `dotnet test -c Release` is green and the CLI generates HTML from the fixtures
  in `samples/`, including the synthetic compare smoke test with lifecycle HTML.
- **Validated on a real model, single-run execution**: yes. One private real export from
  the final revision was human-validated for parsing, raw count, group coverage, grouping,
  model labels, levels, legibility, self-contained HTML, and determinism. See
  [docs/validation/real-r01-single-run.md](docs/validation/real-r01-single-run.md).
- **Validated longitudinally on sequential real exports**: not yet. Matching between runs,
  lifecycle classification, continuity links, continuity paths, and longitudinal HTML have
  not been validated against three real historical exports.
- **Desktop launcher, built and tested**: yes. `dotnet build -c Release` is clean and the
  launcher's own suites are green, including headless tests that boot the real shell and
  integration tests that drive a real child process through success, failure, timeout,
  cancellation, oversized output and undecodable output.
- **Desktop launcher, installed and run on Windows**: not yet. The Inno Setup script and
  the publish, package and smoke scripts exist and are covered by contract tests, but the
  installer has not been compiled or executed, and `scripts/smoke-launcher.ps1` has not
  been run on a clean machine.

## CI

`.github/workflows/ci.yml` runs `dotnet build` and `dotnet test` in Release on Ubuntu for
every push and pull request, using the SDK pinned in `global.json`. It also runs a separate
Windows packaging smoke job that publishes the `win-x64` self-contained single-file
`orzioclash.exe` and executes `scripts/smoke-release.ps1`.

`.github/workflows/release.yml` packages the internal preview on Windows. Manual
`workflow_dispatch` runs are packaging dry runs only and never create a GitHub Release.
Tag-triggered runs for matching `v*` tags verify that the tag matches `orzioclash
--version`, verify that the tagged commit belongs to `origin/master`, package the ZIP and
checksum, and create a prerelease without overwriting an existing release.

## Run Manifest

The run manifest (`RunManifest`) is an explicit and auditable declaration of which models
and revisions participated in a coordination run, and which clash tests were executed in
that run. It is not inferred from file name, path, Navisworks XML, Autodesk Forma, or ACC.
At this stage of the project, that information is always declared manually.

Example (`samples/run-manifest.sample.json`, schema v2):

```json
{
  "schemaVersion": 2,
  "runId": "coordination-2026-07-10-0900",
  "createdAt": "2026-07-10T09:00:00+01:00",
  "models": [
    {
      "company": "Sigma",
      "discipline": "Structure",
      "modelName": "Sigma_Structure",
      "revision": "R04",
      "sourceFileName": "Sigma_Structure_R04.nwc"
    },
    {
      "company": "Alfa",
      "discipline": "HVAC",
      "modelName": "Alfa_HVAC",
      "revision": "R07",
      "sourceFileName": "Alfa_HVAC_R07.nwc"
    }
  ],
  "executedClashTests": [
    {
      "name": "HVAC vs Structure",
      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
    }
  ]
}
```

Required fields for every item in `models`: `company`, `discipline`, `modelName`,
`revision`, and `sourceFileName`. Optional fields: `sourceFilePath`, `contentHash`, and
`publishedAt`.

The revision (`revision`) is always declared manually. It is never extracted automatically
from file name or any convention. Within the same run, each `ModelIdentity` (company +
discipline + modelName, case-insensitive) may have at most one revision. A manifest that
declares two revisions for the same identity is rejected.

### Explicit Executed Clash Test Coverage

`executedClashTests` is a manual declaration of which clash tests ran in this run. It is
separate from, and independent of, observed clash occurrences:

1. Each item has `name` and an ordered `modelA`/`modelB` pair. Each model identity contains
   `company`, `discipline`, and `modelName`, with no revision, source file, or hash. The
   pair is always revision-free, so the same declaration covers future revisions of those
   models.
2. Consumers compare the declared pair as unordered, so swapped A/B represents the same
   coverage, but the raw object preserves A/B order exactly as declared.
3. Every `ClashOccurrence` in a `CoordinationRun` must correspond to a declared
   `executedClashTests` entry with the same name, ignoring case, and the same model pair,
   direct or swapped. An occurrence without declared coverage is rejected.
4. A declaration may exist without any corresponding occurrence. This is valid, and it is
   how a run proves a clash test executed and returned zero clashes rather than never
   executing at all.
5. The lifecycle classifier uses only this explicit declaration to decide whether a clash
   test was observed in the other run. It never scans occurrences for coverage.

The parser (`OrzioClashReport.Input.RunManifestJson`) validates JSON structure and schema,
then builds Core `RunManifest`, `ModelRevision`, `ModelIdentity`, and `ExecutedClashTest`
objects. The CLI `compare` command explicitly loads one previous and one current manifest;
the legacy HTML command still operates on XML only.

### Schema v2 Replaces v1

Schema v2 (`schemaVersion: 2`) is the only accepted version. Schema v1
(`schemaVersion: 1`) did not declare `executedClashTests` and is intentionally rejected
with a clear message saying the supported version is 2. There is no automatic migration or
legacy mode. Silently migrating a v1 manifest to an empty `executedClashTests` list would
confuse "no test was executed" with "we do not know which tests were executed", which are
different facts.

## Coordination Run Snapshot

1. `RunManifest` declares which model revisions participate in a run and which clash tests
   were executed in it.
2. `ClashOccurrence` binds one raw `ClashResult` from XML to the exact model revisions on
   sides A and B within a specific clash test.
3. `CoordinationRun` is the immutable snapshot: the `RunManifest` plus the ordered list of
   observed `ClashOccurrence`s. Every revision used by an occurrence must be declared
   exactly in the manifest, and every occurrence must correspond to a declared
   `executedClashTests` entry.

`CoordinationRun` remains the isolated snapshot of one run. Matching, comparison, and
lifecycle live outside it. Association between XML elements and manifest revisions
(`ClashObject.SourceModel` -> `ModelRevision`) is performed by
`ExactSourceModelCoordinationRunAssembler`, and the CLI `compare` command explicitly
assembles one previous run and one current run without inferring temporal order.

## Immutable Coordination Run Snapshot

There are two distinct JSON contracts, with distinct adapters, that must not be confused:

- **RunManifest JSON** (`OrzioClashReport.Input.RunManifestJson`, `schemaVersion: 2`) is an
  explicit pre-assembly input contract: it manually declares models, revisions, and
  executed clash tests before the run is assembled.
- **CoordinationRun snapshot JSON** (`OrzioClashReport.Persistence.RunSnapshotJson`,
  `schemaVersion: 1`) is a post-assembly evidence snapshot: it persists an assembled
  `CoordinationRun` so comparison and lifecycle can be recalculated later.

These are different schemas and independent adapters. One contract's `schemaVersion` number
has no relationship to the other's.

The public adapter is `JsonCoordinationRunSnapshotSerializer`, with four methods:

- `Serialize(CoordinationRun) -> string`: deterministic canonical JSON.
- `Parse(string) -> CoordinationRun`: rehydrates with strict validation.
- `Save(CoordinationRun, filePath)`: writes a new immutable file.
- `Load(filePath) -> CoordinationRun`: reads and rehydrates.

Snapshot characteristics (`schemaVersion` 1):

1. The `models` array order is preserved.
2. `executedClashTests` references models by `modelAIndex`/`modelBIndex`.
3. `occurrences` references models by `modelAIndex`/`modelBIndex`.
4. Occurrence order and duplicate slots are preserved.
5. Rehydration reuses the exact `ModelRevision`/`ModelIdentity` instances addressed by
   those indexes.
6. Raw `ClashStatus` from the source is persisted as the exact enum string.
7. Matching, selection, confidence, evidence, and lifecycle are not persisted. They are
   recalculable and are never frozen into the evidence layer. Raw `ClashStatus.Resolved` is
   source evidence, not lifecycle status.
8. `ClashObject.Properties` is the only canonicalized collection: entries are sorted by key
   with `StringComparer.Ordinal` before serialization.
9. JSON property names are exact, case-sensitive camelCase. Unknown or duplicate JSON
   properties are rejected. Timestamps require an explicit offset or `Z`.
10. `Save` uses create-new semantics: it never overwrites an existing file, even with
    byte-identical content. It writes UTF-8 without BOM, and a serialization failure
    creates no file.

Reduced example:

```json
{
  "schemaVersion": 1,
  "runId": "coordination-run-001",
  "createdAt": "2026-07-14T09:00:00.0000000+01:00",
  "models": [
    {
      "company": "Sigma",
      "discipline": "Structure",
      "modelName": "Main",
      "revision": "R04",
      "sourceFileName": "Sigma_Main_R04.nwc",
      "sourceFilePath": null,
      "contentHash": null,
      "publishedAt": null
    }
  ],
  "executedClashTests": [
    {
      "name": "Structure self clash",
      "modelAIndex": 0,
      "modelBIndex": 0
    }
  ],
  "occurrences": []
}
```

Create a snapshot from the CLI:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  snapshot \
  --xml samples/sample-clash.xml \
  --manifest samples/sample-clash.run-manifest.json \
  -o run-snapshot.json
```

The `snapshot` command explicitly composes the real pipeline:

1. `NavisworksXmlClashSource` reads the XML.
2. `JsonRunManifestSource` loads the manifest.
3. `ExactSourceModelCoordinationRunAssembler` assembles the `CoordinationRun`.
4. `JsonCoordinationRunSnapshotSerializer.Save` persists the canonical immutable snapshot.

CLI contract rules:

1. `-o`/`--output` is required.
2. The CLI does not infer file names or storage conventions.
3. The CLI does not create the output parent directory.
4. An existing output path is rejected.
5. The success summary is printed only after `Save` succeeds.

Expected success summary with the real fixture:

```text
Run snapshot: coordination-sample-clash-xml
Models: 2
Executed clash tests: 2
Occurrences: 5
Snapshot written to run-snapshot.json
```

This command creates one immutable run snapshot only. It does not compare snapshots, add a
run to a collection or history, create a ledger, or persist matching or lifecycle. Those
facts remain recalculable.

Compare persisted snapshots from the CLI:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  compare-snapshots \
  --previous-snapshot previous-run.json \
  --current-snapshot current-run.json \
  --output comparison.html
```

Contract rules:

1. `--previous-snapshot` and `--current-snapshot` are required.
2. Previous/current remain explicit roles and are never reordered by `CreatedAt`, `RunId`,
   revision, file name, or snapshot metadata.
3. `JsonCoordinationRunSnapshotSerializer.Load` remains the authority for snapshot parsing
   and validation.
4. Matching and lifecycle are always recalculated from persisted evidence. There is no
   persisted HTML, persisted lifecycle, or persisted derived state in the snapshot.
5. Without `-o`/`--output`, the command prints only the deterministic 11-line summary.
6. With `-o`/`--output`, that same summary is followed by `Comparison report written to ...`
   and the freshly rendered revision-aware HTML.
7. The command accepts the same snapshot in both roles only as a synthetic smoke test.
8. There is still no automatic snapshot discovery, chronological inference,
   latest/previous lookup, multi-run lifecycle, Clash Ledger, `Reopened`, or persistent
   clash ID.

Create an ordered run index from the CLI:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  index-snapshots \
  --snapshot runs/run-001.json \
  --snapshot runs/run-002.json \
  --output run-index.json
```

Run-index format:

```json
{
  "schemaVersion": 1,
  "snapshotPaths": [
    "runs/run-001.json",
    "runs/run-002.json"
  ]
}
```

Contract rules:

1. `--snapshot` is required, repeatable, order-preserving, and duplicate-preserving.
2. `-o`/`--output` is required.
3. Index order comes only from the explicit order of `--snapshot` arguments.
4. The index persists only canonical references relative to the index file's own
   directory, always with `/` separators.
5. `JsonCoordinationRunSnapshotSerializer.Load` remains the authority for validating input
   snapshots. The run-index adapter does not deserialize snapshots or inspect their DTOs.
6. Snapshots remain the authority for immutable run evidence.
7. Matching and lifecycle are not persisted in the index.
8. There is still no automatic discovery, chronological inference, latest/previous lookup,
   non-adjacent or all-vs-all comparison, multi-run lifecycle, Clash Ledger, `Reopened`, or
   persistent clash ID.

Honest limits of this stage:

- Snapshot creation, explicit two-snapshot comparison, explicit ordered run-index creation,
  and explicit adjacent traversal over that index already exist. Index order remains the
  only sequence authority, and all snapshots and comparisons are loaded/calculated before
  the first output.
- `compare-index` adjacent traversal is now formalized and presented by an explicit chain:
  sequence comparer, continuity projector, continuity path assembler, sequence analyzer,
  and presentation projector. Stdout without output keeps the deterministic 12-line
  longitudinal prefix; existing pairwise blocks remain preserved after that prefix in
  transition order.
- `compare-index` accepts optional `-o`/`--output` to write self-contained longitudinal
  HTML. Rendering and writing finish before the first stdout line, and the only extra line
  is `Longitudinal report written to ...` at the end.
- There is still no automatic discovery, chronological inference, latest/previous lookup,
  non-adjacent comparison, all-vs-all comparison, multi-run lifecycle, or persisted derived
  state.
- There is still no Clash Ledger.
- There is still no `Reopened`.
- There is still no persistent clash ID.
- There is still no sequential validation against real exports.

## Matching Vocabulary

1. `ClashMatchConfidence`, `MatchEvidence`, and `ClashMatchAssessment` do not execute
   matching by themselves. They are only the vocabulary a future implementation uses to
   record and justify an assessment.
2. `ClashMatchConfidence.High` does not mean "exact" or "human-confirmed"; it is only the
   highest level of evidence corroboration.
3. The source clash GUID (`MatchEvidenceKind.SourceClashGuid`) is only one more piece of
   evidence. It has not yet been proven stable across sequential real exports.
4. Each `MatchEvidence` has a verdict: `Supports`, `Contradicts`, or `Unavailable`.
   `ClashMatchAssessment` allows mixed verdicts and does not recalculate confidence from
   them.
5. There is no numeric score, threshold, lifecycle status, or automatic decision in these
   contracts.
6. The `compare` command uses these contracts through the existing matcher, comparer, and
   classifier; the CLI only presents the result.

## Pairwise Matcher Port

1. `IClashMatcher` (`src/OrzioClashReport.Core/Abstractions/IClashMatcher.cs`) evaluates
   one ordered previous/current pair of `ClashOccurrence`s and nothing else.
2. A non-null return (`ClashMatchAssessment`) is a candidate assessment with auditable
   confidence and evidence.
3. `null` means the matcher produced no candidate for that pair, whether due to
   insufficient evidence, incompatible signals, or an unmet strategy precondition.
4. `Low` is not equivalent to `null`. `Low` is a real candidate assessment with at least
   one piece of evidence; `null` is the absence of an assessment.
5. The port does not define the concrete algorithm. The current production implementation
   is `ConservativeClashMatcher`.
6. The port does not compare complete runs (`CoordinationRun`) or receive occurrence lists.
7. One-to-one selection among competing candidates, conflict resolution, and lifecycle
   status remain the responsibility of a run comparer.
8. The `compare` command uses the matcher only through `DeterministicClashRunComparer`.

## Conservative Pairwise Matcher

`ConservativeClashMatcher` (`src/OrzioClashReport.Core/Matching/ConservativeClashMatcher.cs`)
is the first concrete `IClashMatcher` implementation.

1. It requires three mandatory signals at the same time: same `ClashTestName` using ordinal
   case-insensitive comparison, same revision-free `ModelIdentity` pair, and the
   `ElementId` pair aligned to those models.
2. Revisions (`ModelRevision.Revision`, `SourceFileName`, `SourceFilePath`, `ContentHash`,
   `PublishedAt`) are completely ignored during matching, so a candidate survives
   `R03 -> R04`.
3. A/B inversion between exports is accepted: the two models may swap sides between
   previous and current runs as long as the elements follow the same swap.
4. `ElementId` and source GUID are treated as opaque identifiers and compared with
   `StringComparison.Ordinal`, case-sensitively, never `OrdinalIgnoreCase`.
5. Source GUID is supplemental evidence. An equal GUID raises confidence from `Medium` to
   `High`; a different or missing GUID does not create or destroy a candidate.
6. A `High` result requires the three mandatory signals plus an equal GUID. `Medium` occurs
   when the three mandatory signals pass but GUID is missing or contradicts.
7. This matcher never produces `Low`; it only accepts candidates when all three mandatory
   signals are favorable. Weaker candidates are left for a future strategy.
8. Complete-run comparison and lifecycle classification happen in separate components; this
   matcher remains strictly pairwise.
9. The `compare` command uses this matcher in the current revision-aware composition.

## Deterministic Run Comparer

`DeterministicClashRunComparer` (`src/OrzioClashReport.Core/Matching/DeterministicClashRunComparer.cs`)
implements `IClashRunComparer`, the first orchestrator between two `CoordinationRun`s.

1. It receives explicit `previousRun` and `currentRun`; it never infers which is which from
   `CreatedAt` or `RunId`.
2. It evaluates all pairs (`previous x current`) through an injected `IClashMatcher`.
3. It preserves all generated candidates (`Candidates`), including non-selected ones.
4. It selects a deterministic one-to-one subset (`SelectedMatches`): no previous or current
   index repeats among selected matches.
5. Selection precedence is `High > Medium > Low`.
6. Ties are broken by increasing `PreviousIndex`, then increasing `CurrentIndex`.
7. Non-selected candidates remain visible and auditable in `AlternativeCandidates`; they are
   never treated as false.
8. `UnmatchedPrevious`/`UnmatchedCurrent` is not lifecycle status. An occurrence without a
   selected match may still have alternative candidates.
9. The policy is greedy and not globally optimal: an early selection can block two
   candidates that an optimal assignment could have paired. That is acceptable at this
   stage because the policy is deterministic, precedence is explicit, and lifecycle
   classification is not produced from the comparer result alone.
10. The `compare` command uses this comparer with explicit previous/current roles and no
    temporal inference.

## Conservative Lifecycle Classification

`ConservativeClashLifecycleClassifier` (`src/OrzioClashReport.Core/Lifecycle/ConservativeClashLifecycleClassifier.cs`)
implements `IClashLifecycleClassifier`. It classifies every slot of an already-produced
`ClashRunMatchResult` without rerunning `IClashMatcher` or `IClashRunComparer`.

1. **`StillOpen`**: a selected match with `Medium` or `High` confidence and no alternative
   candidate sharing its `PreviousIndex` or `CurrentIndex`.
2. **`Resolved`**: a previous occurrence with no selected match, no alternative candidate
   referencing its index, and both revision-free `ModelIdentity` values plus the clash test
   observed in the current run.
3. **`New`**: the symmetric rule for a current occurrence with no selected match, no
   alternative, and models plus clash test observed in the previous run.
4. **`Unverifiable`**: anything that does not satisfy the rules above: `Low` confidence,
   competing alternative candidate, missing model, or clash test not observed.
5. Coverage is always revision-free: `ModelRevision.Revision`, `SourceFileName`,
   `SourceFilePath`, `ContentHash`, and `PublishedAt` never participate.
6. A clash test is considered observed in a run only when that run's
   `RunManifest.ExecutedClashTests` explicitly declares that name, using ordinal
   case-insensitive comparison, for the same `ModelIdentity` pair, direct or swapped. The
   classifier never scans `CoordinationRun.Occurrences`. This is what allows a run to prove
   that a test executed and returned zero clashes.
7. Raw `ClashStatus` from Clash Detective never participates in lifecycle decisions.
8. `Reopened` does not exist. Distinguishing a genuinely new clash from a reopened clash
   requires more than two runs of history and remains out of scope.
9. The `compare` command uses this classifier and prints only a deterministic console
   summary.

## Coordination Run Assembly

`ExactSourceModelCoordinationRunAssembler`
(`src/OrzioClashReport.Core/Assembly/ExactSourceModelCoordinationRunAssembler.cs`)
implements `ICoordinationRunAssembler`. It is the first assembler connecting the existing
XML and JSON adapters, producing a `CoordinationRun` from a `ClashReportDocument` and a
`RunManifest`.

1. The XML parser produces `ClashReportDocument`; the manifest JSON adapter produces
   `RunManifest`. `ExactSourceModelCoordinationRunAssembler` combines them. Neither adapter
   depends on the other, and the assembler lives entirely in Core with no I/O.
2. Each clash side (`ClashResult.ElementA`/`ElementB`) is resolved exclusively through
   `ClashObject.SourceModel`, compared against `ModelRevision.SourceFileName` or
   `SourceFilePath` for each model declared in the manifest.
3. The only allowed normalization is `Trim()`, and comparison uses
   `StringComparison.OrdinalIgnoreCase`. No file-name heuristic is applied: no
   `Path.GetFileName`, extension removal, directory separator normalization,
   substring/prefix/suffix, regex, fuzzy matching, or revision/discipline/company inference
   from the token.
4. Zero matching models in the manifest is a failure (`CoordinationRunAssemblyException`).
5. More than one distinct `ModelRevision` matching the same `SourceModel` is also a
   failure. Ambiguity is never resolved by "first candidate" or any other automatic rule.
6. Document order (batch-major, clash-minor) and A/B orientation are always preserved.
   Duplicates in the document become duplicate `ClashOccurrence`s and are never deduplicated.
7. `CoordinationRun` remains the final authority for validating `ExecutedClashTest`
   coverage. The assembler does not duplicate that rule; it lets final `CoordinationRun`
   construction apply it.
8. A synthetic companion manifest exists for the real XML fixture
   (`samples/sample-clash.run-manifest.json`, for `samples/sample-clash.xml`).
9. The `compare` command runs this revision-aware pipeline for each side explicitly.
10. It has not yet been validated on real sequential model exports.

### Companion Manifest for `sample-clash.xml`

`samples/sample-clash.run-manifest.json` manually declares the models and clash tests needed
for `NavisworksXmlClashSource` to assemble a `CoordinationRun` from
`samples/sample-clash.xml` through `ExactSourceModelCoordinationRunAssembler`. Inspection of
the fixture with the corrected parser showed:

- 1 batch: `"Teste 01"`, with 5 clashes.
- One distinct `SourceModel` token across all 5 clashes on both sides:
  `"Project_A_HVAC_PD_R00.rvt"`, so the fixture is a self-clash scenario.

The manifest declares `ModelRevision.SourceFileName = "Project_A_HVAC_PD_R00.rvt"`, exactly
matching the parser token, and a self-clash `ExecutedClashTest` for `"Teste 01"`. A second
synthetic model (`Beta_Architecture_R10.nwc`) and a zero-occurrence `ExecutedClashTest`
between it and the HVAC model are also declared only to illustrate the zero-result
functionality introduced in Step 10. Neither is needed for binding the real fixture. See
`samples/README.md`.

## Backlog

This section records requests that are outside the MVP.

- Embedded clash images in the report
- PDF export
- Licensing
- WPF UI
- Navisworks API adapter (.NET API, live reading)
- Clash status editing inside the tool
- CDE (Common Data Environment) integration
- Future report ordering with same-discipline internal clashes before cross-discipline
  clashes
- Explicit mapping between source model, canonical discipline, and model/system display
  name to replace labels derived directly from the source model name
- Discipline resolver hardening without fuzzy inference
