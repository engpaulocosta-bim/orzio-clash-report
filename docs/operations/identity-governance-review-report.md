# Identity Governance Review Report

## Scope

This document describes the source-only Step 30A standalone identity-governance review
report that exists in the repository as of July 29, 2026. It is not part of the published
`v0.1.0-preview.2` binary contract.

The workflow is intentionally narrow:

- Render one standalone, self-contained HTML review of explicit human identity decisions
- Require governance evidence to validate successfully before any HTML is produced
- Preserve immutable snapshots as evidence only
- Preserve the project catalog as minimal operational state
- Preserve the existing longitudinal report path and bytes unchanged

It does not infer identity, propagate identity, build a graph, group decisions by
`persistentIdentityId`, implement transitivity, introduce a Clash Ledger, implement
`Reopened`, or mutate any source input.

## Command

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  render-identity-governance-report \
  --project project.json \
  --governance identity-governance.json \
  -o reports/identity-governance.html
```

```powershell
.\orzioclash.exe render-identity-governance-report `
  --project .\project.json `
  --governance .\identity-governance.json `
  -o .\reports\identity-governance.html
```

Usage:

```text
Usage: orzioclash render-identity-governance-report --project <project.json> --governance <identity-governance.json> (-o <identity-governance.html> | --output <identity-governance.html>)
```

## Required Flow

1. Load the project catalog with `JsonProjectCatalogSerializer`.
2. Resolve and load the run index and every referenced snapshot in run-index order.
3. Reuse the existing project-workspace protections for the catalog, run index, snapshots,
   and longitudinal report path.
4. Load the governance document with `JsonIdentityGovernanceSerializer`.
5. Run `DeterministicIdentityGovernanceEvidenceValidator`.
6. Refuse to render when validation returns any issue.
7. Project the validated domain objects through the pure Core
   `DeterministicIdentityGovernanceReviewPresenter`.
8. Render one self-contained HTML document with `IdentityGovernanceReviewHtmlRenderer`.
9. Write only the explicitly requested output path with safe replace/create semantics.
10. Never touch the project catalog, run index, snapshots, governance JSON, or longitudinal
    report bytes.

The command never runs matching, lifecycle classification, continuity analysis,
longitudinal grouping, identity inference, propagation, or transitivity.

## Evidence Gate

HTML is produced only when `IdentityGovernanceEvidenceValidationResult.IsValid == true`.

Invalid governance produces:

```text
Identity governance validation failed.
Issues: 2
1. ...
2. ...
```

Contract:

- Exit code `1`
- Stdout empty
- Usage not printed
- No new output file when the destination did not already exist
- Existing output preserved byte-identically
- No temporary file left behind

The issue format is identical to `validate-identity-governance`.

## Success Output

```text
Project: coordination-project
Indexed runs: 3
Decisions: 2
Confirmations: 1
Rejections: 1
Evidence endpoints: 4
Identity governance review written to <output-path>
```

Exit code `0`, stderr empty.

## Review Content

The report is deterministic, self-contained, LF-only, UTF-8 without BOM when written to
disk, and contains no JavaScript, CDN, external fonts, or external assets.

It shows:

- Title `Identity Governance Review`
- Project display name
- Project id
- Indexed-run count
- Decision count
- Confirmation count
- Rejection count
- Evidence-endpoint count
- Empty-state message `No human identity decisions have been recorded.` when appropriate
- Each decision in persisted order, with a visible ordinal, decision id, decision kind,
  reviewer alias, optional reason, optional persistent identity id, and the resolved Left
  and Right endpoints
- For each endpoint: run id, occurrence index, run `CreatedAt` using invariant `"O"`
  formatting, clash test name, clash status, model A/B human identifiers, and minimal clash
  object A/B identifiers using only data already present in `CoordinationRun` and
  `ClashOccurrence`

Optional fields render as `Not provided`. Dynamic content is HTML-encoded.

## Collision Protections

The output path must not resolve to the same file as:

- The project catalog
- The run index
- Any resolved snapshot
- The governance JSON document
- The longitudinal report referenced by the project catalog

Collisions fail with exit code `1`, no stack trace, no mutation, and no leftover temporary
file.

## Deliberate Non-Goals

- No project-catalog schema change
- No governance path or review-report path added to the project catalog
- No longitudinal report mutation or integration
- No identity inference or propagation
- No transitivity
- No Clash Ledger
- No `Reopened`
- No automatic timestamps
- No interactive review UI
- No database or external service
