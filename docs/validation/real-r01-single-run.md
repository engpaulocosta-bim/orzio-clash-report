# Real Project R01 single-run validation

## Scope

This document records the human validation of Phase 5B / Step 26A for one real
single-run Navisworks Clash Detective XML export.

Public alias: Real Project R01.

The source XML, generated HTML, and PDF copy are private validation artifacts. They remain
outside Git and are not authorized for publication.

## Human approval

Step 26A human approval: PASS.

Validation date: 2026-07-21.

## Validation evidence

- Source: PASS. The XML corresponds to the final real revision used for validation.
- Raw count: PASS. Navisworks, XML, and CLI all report 227 clashes.
- Clash tests: PASS. `Test 1` is the correct clash test.
- Group coverage: PASS. The 18 groups contain all 227 clashes; no clash was removed as a
  duplicate.
- Grouping: PASS. The 18 groups correctly separate the real coordination issues.
- Disciplines: PASS. The displayed names correctly identify the submodels because they
  follow the naming convention defined by the project author.
- Levels: PASS. The displayed levels are coherent with the project.
- Duplicates: PASS. All 227 clashes remain displayed.
- Legibility: PASS. The report is readable, with no clipping or overlapping content.
- Self-contained output: PASS. The HTML opens locally without network access or external
  assets.
- Determinism: PASS. Two renders are byte-for-byte identical.
- Privacy: PASS. XML, HTML, and PDF artifacts remained outside Git and are not authorized
  for publication.

Human sampling: all 18 groups were reviewed in the report, including visual inspection of
the tables and the presented organization.

## Validation level

Validated on a real model.

This validation was performed by a human reviewer on private real-project artifacts. The
agent may record this approval, but the private artifacts themselves are not committed.

## Observed limitations for next hardening

These observations do not invalidate the validation and are not implementation scope for
this step.

- The report should eventually present same-discipline internal clashes before
  cross-discipline clashes.
- The current resolver uses source model names as discipline labels. This worked for Real
  Project R01 because the models use meaningful naming, but an arbitrary model name would
  also be reproduced in the report.
- A future hardening pass should add an explicit mapping between source model, canonical
  discipline, and model or system display name, without fuzzy inference.
