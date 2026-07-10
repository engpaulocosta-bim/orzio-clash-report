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
│   ├── OrzioClashReport.Output.Html/         netstandard2.0, deterministic HTML
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
`RunManifest` contains at most one `ModelRevision` per stable `ModelIdentity`. The CLI does
not consume the manifest until a later step.

## Coordination run snapshot

`ClashOccurrence` is run-specific evidence, not cross-run identity. It preserves source A/B
order and references exact `ModelRevision` values. `CoordinationRun` is an immutable
snapshot of `RunManifest` plus ordered occurrences. Every occurrence revision must be
declared exactly in the manifest. Do not create fingerprints, matching, deduplication, or
source-model inference in these types.

## Language

Code, identifiers, comments, and commit messages in **English**. Conversation with the
author may be in Portuguese.
