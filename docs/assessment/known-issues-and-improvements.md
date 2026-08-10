# Known Issues and Improvement Backlog

## Scope and honesty statement

This document records defects, risks, and improvement opportunities found by a full static
review of the repository at commit `57713cc`, source version `0.1.0-preview.3`.

**Verification level: read-only static analysis.** The review environment had no .NET SDK
and no network access to obtain one, so **nothing here was compiled, executed, or
test-covered**. Every finding was derived by reading source, project files, workflows, and
documentation. Findings are stated with the evidence that supports them (file and line), and
each one carries an explicit confidence marker:

- **Confirmed** — provable from the source text alone (a missing property, a duplicated
  file, an unreferenced code path).
- **Reasoned** — a behavioral claim derived from reading the control flow, not observed at
  runtime. These need a test to close.

This separation follows the project's own three-claim rule in `AGENTS.md`: *compiles*,
*runs*, and *validated on a real model* are different claims. This review makes none of the
three. It is a code-reading pass.

Finding numbers are stable identifiers and do not change. They are **not** a priority order —
see the ranking immediately below, which supersedes the numbering.

---

## Priority ranking

The four findings that matter, in order of real impact:

| Rank | Finding | Why it ranks here |
|---|---|---|
| 1 | **#3** — self-clash may degrade to `Unverifiable` | Contaminates confidence in everything else. See below. |
| 2 | **#1** — silent duplicate collapsing | Most embarrassing: the output looks right and is wrong by omission. |
| 3 | **#2** — three HTML writers bypass the atomic path | Silent corruption in an audit tool, but the fix is trivial. |
| 4 | **#4** — 2732-line `Program.cs` | No correctness impact. Costs credibility with anyone who opens the repo. |

**Finding 3 outranks finding 1**, which is not obvious and is worth stating explicitly.

The project's only real-export validation fixture is a self-clash scenario
(`README.md:1144-1146`). If self-clash occurrences can degrade to `Unverifiable` through the
double-candidate path described in finding 3, then the lifecycle classifier has only ever
been exercised on a **degraded** path. Every claim currently made about classification
correctness would rest on ground that may not represent the normal two-model case.

That makes finding 3 a de facto blocking issue. It is not one defect among thirteen; it is
the one that determines whether the other validation claims mean anything. Until it is
settled, the tool cannot honestly be described as classifying correctly — in a README, in an
interview, or anywhere else.

The remaining nine findings are public backlog. They are recorded here so the gap is visible
and dated, not because they are queued for work.

---

## 1. Duplicate collapsing is silent, undocumented, and unauditable

**Severity: high (product-level). Confidence: Confirmed.**

`RuleBasedGrouper.CollapseDuplicates` discards clashes before grouping:
`src/OrzioClashReport.Core/Grouping/RuleBasedGrouper.cs:63-87`. A clash is dropped when it
shares an unordered element-id pair with an already-kept clash and its point falls within
tolerance.

Nothing anywhere reports how many clashes were discarded:

- `GroupedClashReport` (`src/OrzioClashReport.Core/Model/GroupedClashReport.cs:11-12`)
  exposes only `RawCount` and `GroupCount`. There is no `CollapsedCount` and no retained
  total.
- The console prints `{RawCount} raw clashes -> {GroupCount} groups`
  (`src/OrzioClashReport.Cli/Program.cs:187`).
- The HTML header prints the same two numbers
  (`src/OrzioClashReport.Output.Html/HtmlReportRenderer.cs:76-80`), and each group prints
  its own `Members.Count` (`:98-100`).
- `IAppLog` is never injected into the grouper, so not even a warning is emitted.

The consequence is a report a coordinator cannot reconcile. Summing the per-group
"N clash(es)" counters yields a number **lower** than the "raw clashes" figure in the same
header, with no line anywhere explaining the difference. The reader has no way to learn that
collapsing happened, how many items it removed, or which ones.

This matters more here than it would in most tools. The stated core value proposition —
`AGENTS.md:13` and `.claude/skills/orzio-clash-report/SKILL.md:22` — is **honest grouping**.
A silent discard step is the one thing that proposition cannot afford. It is also in tension
with law 6 (`SKILL.md`): *"it never produces a silently-wrong report."*

Compounding this: `README.md` documents grouping in detail (the "Group Identity" section,
`README.md:640-654`) but **never mentions that clashes are discarded at all**. The rule is
documented only in the agent-facing `AGENTS.md:46-47`, which users never read.

### Recommended fix

1. Add `CollapsedCount` (and ideally `RetainedCount`) to `GroupedClashReport`, computed in
   the grouper rather than inferred by callers.
2. Print it in the console summary and render it in the HTML header, so
   `raw = retained + collapsed` reconciles on the page.
3. Document the collapsing rule in `README.md` next to "Group Identity", including that
   clashes with no point are never collapsed
   (`RuleBasedGrouper.cs:73` — `clash.Point.HasValue &&`).
4. Consider an opt-out flag (`--no-collapse`) so a coordinator can audit against the raw
   export.

---

## 2. Three of four HTML write paths bypass the safe writer

**Severity: high. Confidence: Confirmed.**

`DerivedHtmlReportWriter` (`src/OrzioClashReport.Output.Html/DerivedHtmlReportWriter.cs`)
implements a careful write: resolve the full path, verify the parent directory exists, reject
an existing-directory destination, write to a temporary file, then atomically `File.Replace`
or `File.Move`. On failure the destination is left untouched.

It is called from exactly one place — the identity-governance review report
(`Program.cs:577`). The other three HTML outputs use a bare `File.WriteAllText`:

| Output | Site | Writer |
|---|---|---|
| Single-run report | `Program.cs:185` | `File.WriteAllText` |
| Longitudinal report (`compare-index`) | `Program.cs:844` | `File.WriteAllText` |
| Pairwise comparison report (`compare`, `compare-snapshots`) | `Program.cs:1074` | `File.WriteAllText` |
| Identity-governance review | `Program.cs:577` | `DerivedHtmlReportWriter` |

So the three reports a coordinator actually looks at every day get none of the protection,
while the narrowest, most specialized one gets all of it. Practical consequences of the bare
path: a write that fails partway leaves a truncated HTML file that still opens in a browser
and still looks like a report; a missing parent directory produces a raw
`DirectoryNotFoundException` message instead of the tool's own diagnostic; and an existing
directory at the destination path fails late and unclearly.

The same asymmetry appears in the JSON commands, but in the opposite direction: `snapshot`,
`index-snapshots`, `create-project`, and `create-identity-governance` all go through
serializer `Save` methods with create-new semantics
(`Program.cs:296, 373, 399, 728`). Only HTML is unprotected.

### Recommended fix

Route all four HTML writes through `DerivedHtmlReportWriter`. This is a small, mechanical
change and it makes the CLI's output-path behavior uniform and describable in one sentence
in the README.

---

## 3. Self-clash tests can degrade selected matches to `Unverifiable`

**Severity: blocking (ranked #1). Confidence: Reasoned — needs a test to confirm.**

`ConservativeClashMatcher.DetermineModelAlignment`
(`src/OrzioClashReport.Core/Matching/ConservativeClashMatcher.cs:109-132`) returns
`Ambiguous` when both `direct` and `swapped` hold — that is, when both sides of the clash
share the same revision-free `ModelIdentity`. That is precisely a **self-clash test**: one
model checked against itself.

Under `Ambiguous`, both element-alignment branches stay enabled (`:58-64`):

```csharp
bool directElementsMatch  = modelAlignment != ModelAlignment.Swapped && ...;
bool swappedElementsMatch = modelAlignment != ModelAlignment.Direct  && ...;
```

So a previous occurrence `(E1, E2)` produces a candidate against a current occurrence
`(E1, E2)` **and** against a current occurrence `(E2, E1)`.

Whether that causes harm depends on whether both orientations reach the matcher. They can:
`ExactSourceModelCoordinationRunAssembler` preserves document order and A/B orientation and
never deduplicates (`README.md:1126-1127`). The grouper's order-independent pair key
(`RuleBasedGrouper.cs:128-129`) *would* collapse reversed pairs — but the grouper is not in
the compare/snapshot pipeline at all. It runs only in the single-run report path.

When both orientations are present, the chain is:

1. One occurrence yields two candidates (`DeterministicClashRunComparer.cs:79-116`).
2. Greedy one-to-one selection keeps one and leaves the other in `AlternativeCandidates`
   (`:119-144`).
3. `ConservativeClashLifecycleClassifier` requires, for `StillOpen`, that no alternative
   candidate shares the selected match's `PreviousIndex` or `CurrentIndex`
   (`README.md:1082-1084`). That condition now fails.
4. The entry falls through to `Unverifiable` (`README.md:1089-1090`).

The result would be a longitudinal report where genuinely-continuing self-clashes read as
`Unverifiable` rather than `StillOpen`. This is conservative rather than wrong — the tool
under-claims, which matches its design philosophy — but it is a silent quality cliff on a
common case, and it is worth knowing about before the longitudinal validation runs.

### Why this contaminates the other validation claims

This is not hypothetical for this repository. The project's **only** real-export validation
fixture is a self-clash scenario: `README.md:1144-1146` records that all five clashes in
`samples/sample-clash.xml` share one `SourceModel` token on both sides.

That is the whole problem. Every fixture-backed exercise of the lifecycle classifier has run
against the `Ambiguous` alignment branch. If that branch degrades as described, then the
classifier has been validated **only on a degraded path**, and nothing currently known about
its correctness generalizes to the normal case of two distinct models — which is the case
every real coordination project actually runs.

So this is not one defect among thirteen. It is the finding that determines whether the
project's other correctness claims carry weight. Until it is settled, "the tool classifies
correctly" is not a statement the repository can support.

### Recommended fix

1. **Get a two-model fixture.** Either a real anonymized export covering two distinct
   models, or a credible synthetic one with two different `ModelName` values. This is the
   actual deliverable; the test below is how you read its result.
2. Run a full sequential comparison against it. Completion criterion: `New`, `StillOpen`, and
   `Resolved` all appear and are all correct, with no `Unverifiable` that cannot be
   justified from the evidence.
3. Add a focused unit test alongside it: two runs, a self-clash test, the same element pair
   present in both A/B orientations, asserting the resulting statuses. This pins the
   degradation behavior whichever way it turns out.
4. If degradation is confirmed, canonicalize the element pair for self-clash occurrences
   (order-independent key, as the grouper already does at `RuleBasedGrouper.cs:128-129`) so
   a pair produces one candidate rather than two.
5. Document the self-clash behavior either way. Right now `Ambiguous` is described in
   evidence strings (`:138-139`, `:147-148`), but its selection consequences appear nowhere.

---

## 4. `Program.cs` is a 2732-line god file that holds real logic

**Severity: high (maintainability). Confidence: Confirmed.**

`src/OrzioClashReport.Cli/Program.cs` is 2732 lines. Two distinct problems live inside it.

### 4a. Business logic in the CLI

Law 11 (`SKILL.md:60-61`) is explicit: *"No business logic in UI or CLI. `Program.cs` and any
future WPF/dockable panel only wire `IClashSource -> IClashGrouper -> IReportRenderer`. All
logic lives in Core."*

`Program.cs` currently holds:

- `ValidateProjectCatalogWorkspace` (`:891-989`) — roughly 100 lines of workspace containment
  **policy**: the run index must stay inside the catalog tree, the report destination must
  stay inside the tree, every snapshot must stay inside the tree, the report must not collide
  with a snapshot. These are product rules, not argument parsing.
- `IsPathWithinDirectory` (`:1047`), `PathsEqual` (`:1039`) — the primitives that policy
  rests on.
- `ValidateIdentityGovernanceReviewOutputPath` (`:990-1038`).
- `CreateDerivedComparison` (`:1057`), `CreateLongitudinalPresentationResult` (`:819`),
  `LoadPresentationResultFromRunIndex` (`:780`) — pipeline composition.

Because this lives in an `internal static` class in the executable, none of it is unit
testable except through the full CLI surface. The test suite reflects that: the coverage for
these rules sits in CLI-level tests (`ProjectCatalogAppendCliTests`,
`IdentityGovernanceReviewCliTests`, and similar), which are slower and less precise than
testing the policy directly.

### 4b. Thirteen hand-rolled argument parsers

There are 13 `TryParse*Arguments` methods and 12 matching `IsRecognized*Option` predicates,
spanning roughly `:1087-2343` — about **1250 lines, half the file**. They are structurally
near-identical: loop over `args`, reject unrecognized tokens, reject a missing or
option-shaped value, reject duplicates, then check required options one by one.

The duplication has already produced an inconsistency. `TryParseLegacyArguments`
(`:1087-1128`) is the odd one out: it takes a positional input path, does **not** use the
`IsRecognized*` / `RequiresNonOptionLikeValue` machinery every other command uses, and is the
only parser that checks `File.Exists` inline (`:1121`) rather than deferring to a separate
`Validate*Paths` step. Any future hardening applied to "all commands" will silently skip the
default single-run workflow — which is the most-used command in the tool.

### Recommended fix

1. Extract one small `CommandLineParser` helper (a spec of option names, arity, required
   flags) and express each command as a declarative spec. This alone should remove most of
   the 1250 lines. It requires no third-party dependency, so the zero-dependency law is
   untouched — and the law binds Core anyway, not the CLI.
2. Move `ValidateProjectCatalogWorkspace` and its path primitives into Core (for example
   `OrzioClashReport.Core/Workspace/`) behind a testable type, leaving `Program.cs` to call
   it. Note this needs care: `Path.GetFullPath` semantics differ between `netstandard2.0`
   and `net8.0`, so either target the policy type at the adapter layer or inject a path
   resolver.
3. Split the remaining `Program.cs` into one file per command group.

---

## 5. `TreatWarningsAsErrors` is missing on exactly the two projects that need it most

**Severity: medium. Confidence: Confirmed.**

All eight library projects set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. The two
that do not are:

- `src/OrzioClashReport.Cli/OrzioClashReport.Cli.csproj` — the 2732-line file
- `tests/OrzioClashReport.Tests/OrzioClashReport.Tests.csproj`

There is no `Directory.Build.props`, so the setting is copy-pasted into each of the eight
csproj files and a new project starts without it by default.

This undercuts a published claim. `README.md:673` states: *"**Compiles**: `dotnet build -c
Release` passes without warnings."* CI (`.github/workflows/ci.yml`) runs
`dotnet build --configuration Release --no-restore` with no `-warnaserror`, so for the CLI
and test projects that claim is enforced by nothing and can regress without any signal.

### Recommended fix

Add a `Directory.Build.props` at the repository root setting `Nullable`, `LangVersion`, and
`TreatWarningsAsErrors` once; delete the eight copies. Add `-warnaserror` to the CI build
step so the README's claim is actually gated.

---

## 6. The XML parser can report zero clashes on a structurally unexpected export

**Severity: medium. Confidence: Reasoned.**

`NavisworksXmlClashSource.Read` (`src/OrzioClashReport.Input.NavisworksXml/NavisworksXmlClashSource.cs:43-48`)
navigates a fixed, unnamespaced element path:

```csharp
exchange.Elements("batchtest").Elements("clashtests").Elements("clashtest")
```

The only structural guard is on the root element (`:35-39`). If an export nests differently,
or carries an XML namespace, or renames any level, this expression yields an empty sequence.
The tool then produces a valid `ClashReportDocument` with zero batches, prints
`0 raw clashes -> 0 groups`, writes an empty-but-well-formed HTML report, and **exits 0**.

The same silent-empty pattern repeats one level down: a missing `<clashresults>` element
yields an empty clash list via `?? new List<ClashResult>()` (`:58-62`) with no warning.

Law 6 (`SKILL.md`) requires the opposite: *"Malformed input fails loudly with a clear
message, it never produces a silently-wrong report."* An empty report that exits successfully
is the silently-wrong report that law is written to prevent — and it is the failure mode most
likely to be believed, because "no clashes" is a plausible answer.

Note the parser is otherwise well-built: `ResolveSourceModel` (`:158-171`) is exact-match
only with a documented rationale against fuzzy inference, and unknown statuses are warned and
mapped rather than guessed (`:173-187`). This finding is about one specific gap, not the
parser's general quality.

### Recommended fix

1. Warn via `IAppLog` when `<batchtest>`, `<clashtests>`, or `<clashtest>` is absent, naming
   which level was missing.
2. Warn when a `<clashtest>` has no `<clashresults>` element (distinct from having an empty
   one — those are different facts, exactly as the schema-v1-rejection rationale at
   `README.md:771-778` argues for manifests).
3. Consider a non-zero exit code, or at minimum a prominent console line, when a parse
   yields zero batches from a non-empty file.

---

## 7. Five drifting copies of `DuplicatePropertyValidator`

**Severity: medium. Confidence: Confirmed.**

Verbatim-ish copies of the same helper exist in five adapters, all with different content
hashes:

```
src/OrzioClashReport.Input.RunManifestJson/DuplicatePropertyValidator.cs
src/OrzioClashReport.Persistence.IdentityGovernanceJson/DuplicatePropertyValidator.cs
src/OrzioClashReport.Persistence.ProjectCatalogJson/DuplicatePropertyValidator.cs
src/OrzioClashReport.Persistence.RunIndexJson/DuplicatePropertyValidator.cs
src/OrzioClashReport.Persistence.RunSnapshotJson/DuplicatePropertyValidator.cs
```

`StrictIso8601DateTimeOffsetConverter` exists in two copies (RunManifestJson,
RunSnapshotJson), also differing.

**The duplication is deliberate, and the reason is sound.** The RunSnapshotJson copy
documents it: *"This is an independent, adapter-local copy: the persistence adapter never
depends on the run manifest adapter."* That upholds law 1 — adapters depend on Core, never on
each other.

The problem is the mechanism, not the principle. Today the copies differ only in namespace,
exception type, doc comment, and a `using` (the RunIndexJson copy writes
`System.StringComparer.Ordinal` inline where others import `System`). That is benign drift.
But nothing prevents a real behavioral fix landing in one copy and not the other four, and
nothing would detect it — each adapter's tests only exercise its own copy.

A near-identical situation exists for the atomic-write helper, which appears three times:
`DerivedHtmlReportWriter`, `RunIndexFileReplacer`, and `IdentityGovernanceFileReplacer` all
implement the same temp-file-then-replace dance.

### Recommended fix

Keep adapter assembly independence; stop keeping five source copies. Two options that both
preserve law 1:

- **Shared compile items:** one `src/Shared/JsonGuards/*.cs` folder, included in each adapter
  csproj via `<Compile Include="..\Shared\..." Link="..." />`. Each assembly still compiles
  its own copy with no assembly reference between adapters, but there is one file to edit.
- **A source-only package or `internal` shared project** with the same property.

Either way, add one test suite over the shared source so a fix is verified once.

---

## 8. The discipline color palette silently collides past six disciplines

**Severity: medium. Confidence: Confirmed.**

`HtmlReportRenderer.BuildDisciplineColorMap` (`src/OrzioClashReport.Output.Html/HtmlReportRenderer.cs:49-64`)
maps disciplines onto a six-entry palette with `Palette[i % Palette.Length]` (`:60`).

From the seventh distinct discipline onward, colors repeat. Two unrelated disciplines get an
identical badge color with nothing to distinguish them. A real coordination project routinely
exceeds six — Architecture, Structure, HVAC, Plumbing, Electrical, Fire Protection,
Sprinklers, Telecom is eight before specialist packages.

"Per-discipline color accents" is named as an in-scope MVP deliverable in both `AGENTS.md:79`
and `SKILL.md:68`, so a wraparound that quietly defeats the accent is a real gap in a
committed feature, not a cosmetic nitpick.

### Recommended fix

Generate colors procedurally (evenly spaced HSL hues at a fixed saturation and lightness,
ordered by the same deterministic discipline sort already in place) so the palette scales to
any count while staying byte-deterministic for the golden-file tests. Keep contrast fixed so
white badge text stays readable.

---

## 9. Diagnostics exist in one adapter and nowhere else

**Severity: medium. Confidence: Confirmed.**

`IAppLog` is consumed in exactly one type — `NavisworksXmlClashSource`, at three call sites
(`:100`, `:120`, `:185`). No other component in `src/` takes an `IAppLog`.

That leaves the entire Core pipeline silent: grouper, assembler, matcher, comparer, lifecycle
classifier, continuity projector, path assembler, analyzer, presentation projector, all four
renderers, and all five JSON adapters. There is no diagnostic channel for the grouper's
discards (finding 1), for assembler ambiguity, or for matcher candidate competition.

`ConsoleAppLog` (`src/OrzioClashReport.Cli/ConsoleAppLog.cs`) is also minimal: two methods,
both unconditional writes to stderr, with no level filter and no way to silence output. For a
tool with published byte-exact stdout contracts, having no `--quiet` or `--verbose` control
over stderr is a gap that will be felt as soon as a large real export produces a warning per
clash.

### Recommended fix

1. Inject `IAppLog` into `RuleBasedGrouper` first — that is where the missing information
   costs the most.
2. Add a level or a `--quiet` flag to `ConsoleAppLog`. Keep stdout untouched so the published
   contracts hold.

---

## 10. Silent catch in temporary-file cleanup, in three copies

**Severity: low-medium. Confidence: Confirmed.**

`DeleteTemporaryFileIfPresent` swallows both `IOException` and `UnauthorizedAccessException`
with empty bodies, in three places:

- `src/OrzioClashReport.Output.Html/DerivedHtmlReportWriter.cs:95-100`
- `src/OrzioClashReport.Persistence.RunIndexJson/RunIndexFileReplacer.cs:97-102`
- `src/OrzioClashReport.Persistence.IdentityGovernanceJson/IdentityGovernanceFileReplacer.cs:98-103`

Law 7 (`SKILL.md`) is unambiguous: *"No silent catch. Catch, log with context, then decide:
skip this element or fail the run. Swallowing an exception is a defect."*

Best-effort cleanup in a `finally` is a defensible exception to that law — failing a
successful write because a temp file could not be deleted would be worse. But the current
code takes the exception without stating it. The observable cost is stray
`.derived-html-report-<guid>.tmp` files accumulating in the user's output directory with no
diagnostic explaining where they came from.

### Recommended fix

Either log the swallow through `IAppLog`, or add a comment at each site recording that this
is a deliberate, bounded exception to law 7 and why. Since these three sites should be
deduplicated anyway (finding 7), fixing it once in the shared helper resolves all three.

---

## 11. One malformed clash object aborts the entire export

**Severity: low-medium. Confidence: Confirmed.**

`ParseClashObject` throws when a clash object carries no `GUID` objectattribute
(`NavisworksXmlClashSource.cs:118-124`). One bad element out of a 1458-clash export therefore
produces no report at all.

Law 7 frames this as a choice — *"decide: skip this element or fail the run"* — and failing
loudly is a legitimate pick. But it sits awkwardly next to law 6's *"Missing optional field
becomes null"*, and for a coordinator mid-deadline, losing a whole report to one malformed
element is a harsh outcome when skipping-and-reporting would deliver 1457 usable clashes plus
an explicit warning.

Two smaller issues at the same site:

- The identical message is both logged as `ERROR:` (`:120-121`) and thrown (`:122-123`). The
  CLI catch at `Program.cs:191-195` then prints it a second time as `Failed to generate
  report: {message}`. The user sees the same sentence twice on stderr.
- Duplicate smarttag names are silently collapsed by `g.First()`
  (`:143-144`) — a second value for the same key is discarded with no warning.

### Recommended fix

Decide the policy explicitly and document it in the README. If fail-fast stays, remove the
duplicated log-then-throw. If skip-and-warn is chosen, collect skipped elements and print a
count in the summary.

---

## 12. Matching cost is quadratic and recomputed per transition

**Severity: low (a scaling note, not a present defect). Confidence: Reasoned.**

`DeterministicClashRunComparer.GenerateCandidates` (`:79-116`) calls the matcher for every
previous × current pair. `ConservativeClashMatcher.Assess` exits early on the clash-test name
(`:37-40`), so the constant factor is small, but the pair count is not:

- The README's own example export has 1458 clashes. Two such runs is ~2.1M `Assess` calls per
  adjacent transition.
- `compare-index` recomputes matching and lifecycle independently for each adjacent
  transition (`README.md:274`). A 10-run index is ~19M calls.
- `render-project` recomputes the whole chain from snapshots on every invocation
  (`README.md:356-357`).

Nothing here is persisted or cached, by deliberate design — derived state is recalculable and
never frozen. That is the right call for correctness and auditability. This finding only
notes that the design has a cost curve that has not been measured, and no benchmark exists in
the test suite.

### Recommended fix

Measure before optimizing. Add one timing test over a realistically-sized synthetic pair.
If it proves too slow, the cheap and safe win is to bucket occurrences by clash-test name
before the nested loop, which preserves determinism and every existing contract while cutting
the pair count by roughly the number of clash tests.

---

## 13. Smaller items

**All Confirmed.**

- **`ClashResult.GridLocation` is dead surface.** Declared at
  `src/OrzioClashReport.Core/Model/ClashResult.cs:11`, always passed `null` by the only
  parser (`NavisworksXmlClashSource.cs:107`), never rendered by any of the four renderers.
  Either populate it or remove it — an always-null public property is a standing invitation
  to a wrong assumption.

- **Dedup radius is borrowed from the clash-test tolerance.** `RuleBasedGrouper.cs:35` passes
  `batch.Tolerance` as the spatial de-duplication radius. In Clash Detective, that attribute
  is the *detection* tolerance, a different concept. For a hard-clash test with a small
  tolerance the effect is negligible; for a clearance test configured with a large tolerance,
  the collapse radius grows with it and may merge genuinely distinct clashes on the same
  element pair. The fallback constant `DefaultTolerance = 1e-6` (`:12`) applies only when the
  attribute is absent. Worth an explicit, separately-configurable dedup radius.

- **`Bucket` is a `readonly struct` holding a mutable `List`.** `RuleBasedGrouper.cs:44-60`.
  `buckets.TryGetValue` returns a *copy* of the struct, and `bucket.Members.Add(clash)`
  (`:111`) works only because `Members` is a reference type. Correct today; silently broken
  the day someone adds a value-typed field (say a counter) and mutates it the same way. A
  sealed class removes the trap at no cost.

- **No `Directory.Build.props`, no lockfiles, no Dependabot, no CodeQL.** Package versions in
  the test csproj are floating-free pins with no `packages.lock.json`, so CI restores are not
  byte-reproducible. The CI workflow correctly pins its GitHub Actions by SHA — good practice
  already in place — but nothing keeps those SHAs fresh.

- **`samples/sample-clash2.xml` is 16 MB.** It is cloned by every checkout and every CI run.
  It backs `ParsingLargeSampleTests` and `GroupingRealFixtureTests`, so it is genuinely load-
  bearing — but a generated-at-test-time fixture, or Git LFS, would keep the same coverage
  without putting 16 MB in every clone forever.

---

## Closed scope: one weekend, 8-10 hours

This is a **closed** scope, not a starting point. Thirteen findings is a list to publish, not
a list to work through. Four items are in; everything else is explicitly out.

1. **Install the .NET SDK, build, run the tests, and confirm or discard these findings.**
   This comes first, and the order matters. Every finding here is static, and the blocking
   one (#3) is Reasoned rather than Confirmed. Starting with the fixture would risk hours
   spent chasing a degradation that may not occur in a real run. If this step discards #3,
   it has removed the most expensive item in the list before any of it was paid for.
2. **Two-model fixture, and confirm classification does not degrade** (finding 3) — only if
   step 1 confirms the degradation. Completion criterion: one real sequential run producing
   correct `New`, `StillOpen`, and `Resolved`, with no unjustified `Unverifiable`.
3. **Expose the collapsed-duplicate count** in the console summary, the HTML header, and the
   README (finding 1).
4. **Route the three remaining HTML writers through `DerivedHtmlReportWriter`** (finding 2).
   Estimated under two hours.

**Out of scope: finding 4 and the remaining nine.** They stay in this document as public
backlog. Do not touch them. `Program.cs` in particular is a refactor that improves how the
repository reads, not whether it works, and it will consume the entire budget if allowed to
start.

**Kill rule:** if the four items are not closed at 10 hours, commit whatever exists and stop.
The deadline protects time allocated elsewhere, not the code. An unfinished item returns to
the backlog above; it does not get an extension.

**On how to read the rest of this document:** a public backlog is a record, not a plan. A
list of thirteen findings with a suggested sequence is a work queue wearing documentation's
clothes, and it invites exactly the drift the closed scope above exists to prevent. If a list
is not meant to be executed, it has to say so in writing — which is what this section is for.

---

## What this review did not cover

- No build, no test run, no execution of any kind (no SDK available; see the scope statement).
- The 68-file test suite was inventoried but not read in depth; assertions about test coverage
  are inferences from file names and from which types are `internal`.
- The four HTML renderers were reviewed for encoding and determinism only. `WebUtility.HtmlEncode`
  is applied consistently to dynamic content in `HtmlReportRenderer`, and colors come from
  constants rather than input, so no injection path was found there — but the other three
  renderers were not audited line by line.
- The golden files were not regenerated or diffed.
- No security review of the JSON adapters' strictness guarantees beyond reading their intent.
