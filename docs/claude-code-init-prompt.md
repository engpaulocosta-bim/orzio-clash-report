# Claude Code — Initial Prompt: OrzioClashReport

> Paste this whole block into Claude Code as the first message of a fresh project.
> Rename `OrzioClashReport` freely. Do not change the architecture rules.

---

## ROLE

You are a senior .NET software engineer building a real production tool, not a throwaway script. Optimize for correctness, testability, and a clean seam that lets us swap the data source later without rewriting anything. Prefer simple, robust code with clear return over clever code. When you are unsure about a Navisworks XML field or an API member, parse defensively and log it as unknown. Never invent an XML element, attribute, or API member that you have not seen in the sample fixture.

## PRODUCT CONTEXT

We are building **OrzioClashReport**: a tool that reads Autodesk Navisworks Clash Detective results and produces a clean, grouped HTML coordination report. The pain it kills: a BIM coordinator spends 4+ hours a week manually turning hundreds of raw clashes into a readable report. The differentiator is not detection (Navisworks already detects). It is **honest grouping**: 400 raw clashes are usually ~30 real problems. We collapse duplicates and group by system pair so a human sees signal, not noise.

Constraints that shape every decision:
- The author has ~1 hour/day. Scope discipline is a feature, not a nicety.
- MVP input is the **Navisworks Clash Detective XML export**. No other input in the MVP.
- The **near-certain** next step after MVP is reading clashes directly from the Navisworks .NET API (`Autodesk.Navisworks.Api`, `DocumentClash`, `ClashTests`, `ClashResults`). That API is **.NET Framework 4.8 only**, and is compatible within a single Navisworks major version (2024, 2025, 2026 need separate binaries). The architecture must absorb this later with zero changes to the core.

## MANDATORY ARCHITECTURE (Ports & Adapters / Hexagonal)

Dependencies point **inward only**. The core knows nothing about XML, HTML, or Navisworks.

```
OrzioClashReport.sln
├── src/
│   ├── OrzioClashReport.Core/                (TFM: netstandard2.0, ZERO third-party deps)
│   │   ├── Model/                            immutable domain types (see DOMAIN MODEL)
│   │   ├── Abstractions/
│   │   │   ├── IClashSource.cs               reads clashes from anywhere -> ClashReportDocument
│   │   │   ├── IClashGrouper.cs              ClashReportDocument -> grouped view
│   │   │   ├── IReportRenderer.cs            grouped view -> output (string/stream)
│   │   │   ├── IDisciplineResolver.cs        element -> discipline label (naming varies per project)
│   │   │   └── IAppLog.cs                     minimal logging seam (no framework)
│   │   └── Grouping/
│   │       └── RuleBasedGrouper.cs           dup collapse + system-pair + level grouping
│   ├── OrzioClashReport.Input.NavisworksXml/ (TFM: netstandard2.0)
│   │   └── NavisworksXmlClashSource.cs       implements IClashSource via System.Xml.Linq
│   ├── OrzioClashReport.Output.Html/         (TFM: netstandard2.0)
│   │   └── HtmlReportRenderer.cs             implements IReportRenderer, deterministic output
│   └── OrzioClashReport.Cli/                 (TFM: net8.0) console entry point
│       └── Program.cs                        wires Xml -> Grouper -> Html
├── tests/
│   └── OrzioClashReport.Tests/               (TFM: net8.0, xUnit)
│       ├── ParsingTests.cs                   assert counts/fields from sample fixture
│       ├── GroupingTests.cs                  hand-built domain objects, no XML needed
│       └── RenderingTests.cs                 golden-file: same input -> byte-identical HTML
├── samples/
│   └── sample-clash.xml                      small anonymized real export (author provides)
└── README.md
```

Why these framework choices (do not change without asking):
- **Core + adapters = `netstandard2.0`** so they are consumable by BOTH the net8.0 CLI today AND a future .NET Framework 4.8 Navisworks-API adapter. This is the single most important decision. Do not target net8.0 in the core.
- **CLI = net8.0** because it has no Navisworks dependency and gets modern tooling.
- **Core has zero third-party packages.** No Newtonsoft, no logging libs. XML parsing lives in the XML adapter using `System.Xml.Linq`.

Hard rule: `OrzioClashReport.Core` must not reference any adapter, any Navisworks assembly, `System.Xml.Linq` types in its public surface, or anything HTML. If the core needs to `using System.Xml`, you did it wrong.

## DOMAIN MODEL (source-agnostic, immutable)

Base these on the real Navisworks Clash XML shape. Use `IReadOnlyList`, init-only or constructor-set properties, and value semantics where sensible.

- `ClashReportDocument` : `SourceName`, `ExportedAt` (nullable), `IReadOnlyList<ClashBatch> Batches`
- `ClashBatch` : `Name`, `Tolerance` (nullable double), `IReadOnlyList<ClashResult> Clashes`  (maps to `<clashtest>`)
- `ClashResult` : `Name`, `Status` (enum), `Distance` (nullable double), `GridLocation`, `Point` (ClashPoint, nullable), `ElementA` (ClashObject), `ElementB` (ClashObject), `Guid` (nullable)
- `ClashObject` : `ElementId`, `ElementName`, `Level`, `SourceModel`, `PathHierarchy` (IReadOnlyList<string>)
- `ClashPoint` : `X`, `Y`, `Z` (doubles)
- `ClashStatus` enum : `New, Active, Reviewed, Approved, Resolved, Unknown` (map unrecognized strings to `Unknown` and log, never throw)

Grouped view (produced by the grouper, consumed by the renderer):
- `GroupedClashReport` : `Document` summary, `IReadOnlyList<ClashGroup> Groups`, `RawCount`, `GroupCount`
- `ClashGroup` : `Key` (e.g. discipline-pair + level), `DisciplineA`, `DisciplineB`, `Level`, `IReadOnlyList<ClashResult> Members`, `RepresentativeClash`

## MVP SCOPE — BUILD

1. Solution + all projects wired, nullable reference types ON, warnings-as-errors ON in the core.
2. Domain model + enum.
3. `NavisworksXmlClashSource`: parse `samples/sample-clash.xml` into `ClashReportDocument`. Defensive: missing optional fields become null, unknown status -> `Unknown` + log.
4. `RuleBasedGrouper`: (a) collapse exact duplicates (same element-id pair, same point within tolerance), (b) group remaining by discipline-pair + level, (c) compute raw vs grouped counts.
5. `HtmlReportRenderer`: single self-contained HTML file, **light theme**, colored accents per discipline (green/blue/orange/red/purple/yellow palette). Header shows "N raw clashes -> M groups". Deterministic output (same input -> identical bytes; no timestamps in the body, no random ordering — sort groups by a stable key).
6. `Cli`: `orzioclash <input.xml> -o <output.html>`. Clear errors on bad path / malformed XML.
7. Tests: parsing counts, grouping behavior, golden-file render.

## MVP SCOPE — DO NOT BUILD (reject scope creep, log requests to README "Backlog")

- Clash images / viewpoint capture (this is the hardest piece — it belongs to the 90-day version, and only by reusing Navisworks' own exported viewpoint images, not by generating them).
- PDF generation (the browser prints HTML to PDF for free).
- Licensing, license keys, DRM, payment, sales page.
- WPF / any GUI. The CLI is the MVP surface.
- The Navisworks .NET API adapter. Reserve the interface, do not implement.
- Status editing, responsible-party assignment, comments, history, database, CDE/BIM 360 integration. That is issue-management (Revizto territory); out of scope.
- LLM/AI grouping. Rule-based handles ~80%; AI adds cost and non-determinism for no MVP value.
- DI container, config framework, plugin loader. Manual constructor wiring in `Program.cs` is enough.

## CODING LAWS (non-negotiable)

1. Dependencies point inward. Core references nothing outward.
2. Core: zero third-party deps; `netstandard2.0`; nullable ON; warnings-as-errors.
3. Immutable domain types. No public setters that mutate after construction.
4. No invented API members or XML fields. If it is not in the fixture, treat it as optional/unknown and log.
5. No silent catch. Catch -> log with context -> decide (skip this element vs fail the run). Never swallow.
6. Deterministic rendering. No `DateTime.Now`, no unordered dictionaries, in the output body.
7. Every public type has a one-line XML doc comment stating its single responsibility.
8. Small files, single responsibility. If a class does two jobs, split it.
9. Adapters depend on Core only, never on each other.
10. Names in English, code and identifiers in English.

## VALIDATION HONESTY (state which one you actually did)

These are three different claims; never conflate them in your reports to me:
- **Compiles**: `dotnet build` succeeds.
- **Runs**: `dotnet test` green AND the CLI produced an HTML file from the sample fixture.
- **Validated on a real model**: the author ran it on a real (anonymized) Navisworks export and the grouped report matched reality.

You can only ever claim the first two. The third is the author's job. When you finish a step, report exactly what you verified and what remains unverified.

## BUILD ORDER (do these in sequence; stop and show me after each)

- **Step 1** — Scaffold the solution and all projects with the exact TFMs above. Compiles, empty. Show `dotnet build` output.
- **Step 2** — Domain model + `ClashStatus`. Compiles. No logic.
- **Step 3** — `IClashSource` + `NavisworksXmlClashSource`. I will give you `samples/sample-clash.xml`. Parse it. Add `ParsingTests` asserting batch/clash counts and a couple of field values. Show test output.
- **Step 4** — `IClashGrouper` + `RuleBasedGrouper` + `GroupingTests` (hand-built objects, no XML). Show raw-vs-grouped counts on the sample.
- **Step 5** — `IReportRenderer` + `HtmlReportRenderer` + golden-file test. Produce `report.html` from the sample. Show me the file.
- **Step 6** — `Cli` wiring end to end. `orzioclash samples/sample-clash.xml -o report.html`.

## FUTURE EXTENSION POINTS (reserve interfaces, DO NOT implement now)

Design so these drop in later without touching Core:
- `OrzioClashReport.Input.NavisworksApi` (**net48**, per-Navisworks-version binaries): implements the same `IClashSource` reading `DocumentClash`/`ClashTests`/`ClashResults` live inside Navisworks. Packaged as a Navisworks plugin bundle (`vXX` folders). Because Core is `netstandard2.0`, this 4.8 project can reference it unchanged.
- `OrzioClashReport.Output.Pdf`: only if ever justified. Note browser-print already covers it.
- `OrzioClashReport.Ui.Wpf` or a Navisworks dockable panel: wraps the same Core pipeline. No business logic in the UI.
- Image enrichment: an optional `IClashImageProvider` that attaches viewpoint images to `ClashResult`s by reusing Navisworks-exported viewpoint images. Renderer already leaves a slot for an optional image per group.

When you scaffold, leave a short `// FUTURE:` comment at each seam pointing to the matching interface, so the extension path is obvious to the next agent.

## FIRST ACTION

Start with **Step 1** only. Create the solution and the project skeletons with the exact target frameworks, confirm `dotnet build` passes on an empty solution, and show me the folder tree and the build output. Do not write parsing, grouping, or rendering logic yet. Wait for me to hand you the sample XML before Step 3.
