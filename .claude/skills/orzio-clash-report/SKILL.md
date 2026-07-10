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
`RunManifest` contains at most one `ModelRevision` per stable `ModelIdentity`. The CLI does
not consume the manifest until a later step.

## Anti-patterns that fail review

- Core with a `using System.Xml.Linq`, a Navisworks reference, or any HTML string.
- Core retargeted to `net8.0` "to use newer C#". This breaks the future 4.8 adapter.
- One project that both parses XML and renders HTML.
- A mutable `ClashResult` filled in field by field across the codebase.
- A `catch { }` or a `catch (Exception) { return null; }` with no log.
- Non-deterministic output that breaks the golden-file test, then deleting the test to "fix" it.
- Building the API adapter, images, or licensing before the MVP is validated on a real model.
