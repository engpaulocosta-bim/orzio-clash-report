# Identity Governance Evidence Validation

## Scope

This document describes the source-only Step 29C identity-governance evidence-validation
CLI workflow that exists in the repository as of July 28, 2026. It is not part of the
published `v0.1.0-preview.2` binary contract.

The workflow is intentionally narrow: `validate-identity-governance` answers, for one
project, whether every explicit human identity decision's `runId` + `occurrenceIndex`
evidence endpoints point at a real occurrence slot inside a snapshot indexed by that
project.

It is read-only. It does not write, replace, or create any file; does not render HTML;
does not run matching, lifecycle classification, or continuity analysis; does not infer or
propagate identity; does not project decisions into a report; and does not implement a
Clash Ledger.

## Command

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  validate-identity-governance \
  --project project.json \
  --governance identity-governance.json
```

```powershell
.\orzioclash.exe validate-identity-governance `
  --project .\project.json `
  --governance .\identity-governance.json
```

## Required Flow

1. Load the project catalog with `JsonProjectCatalogSerializer`.
2. Resolve the project catalog's run index with `ProjectCatalogPathResolver`.
3. Load the run index with `JsonRunIndexSerializer`, preserving its explicit order.
4. Resolve and load every referenced snapshot with `RunIndexSnapshotPathResolver` and
   `JsonCoordinationRunSnapshotSerializer`.
5. Load the identity-governance document with `JsonIdentityGovernanceSerializer`.
6. Compare the governance document's `projectId` against the project catalog's `projectId`.
7. Validate every decision's evidence endpoints against the loaded snapshots.
8. Never write, replace, or create any file.
9. Never render HTML.
10. Never run matching, lifecycle classification, or continuity analysis.
11. Never reorder runs, decisions, or endpoints.
12. Return a deterministic result.

The command works identically with zero decisions, one indexed snapshot, or many indexed
snapshots. It does not require at least two indexed snapshots: this is evidence
validation, not longitudinal comparison. Decisions may reference adjacent or non-adjacent
indexed runs identically.

## Core Validator

`OrzioClashReport.Core.Governance.IIdentityGovernanceEvidenceValidator`, implemented by
`DeterministicIdentityGovernanceEvidenceValidator`, is a pure Core component with the
signature:

```csharp
IdentityGovernanceEvidenceValidationResult Validate(
    string expectedProjectId,
    IdentityGovernanceDocument governance,
    IReadOnlyList<CoordinationRun> indexedRuns);
```

It depends on no filesystem, JSON, CLI, project-catalog adapter, run-index adapter,
snapshot adapter, HTML, or Navisworks type. It performs no I/O, introduces no randomness,
and never mutates its inputs. For the same inputs, the result is deterministic.

### Validation Rules

1. **Project id.** The document is invalid when `governance.ProjectId != expectedProjectId`.
   The comparison is ordinal; case, whitespace, and punctuation are never normalized.
2. **Duplicate indexed run id.** Two or more indexed snapshots sharing the same run id make
   resolution ambiguous. One issue is produced for every occurrence after the first, in
   run-index order. The run ids are never reordered or silently deduplicated, and no
   snapshot is picked arbitrarily.
3. **Run not indexed.** Every endpoint's `runId` must exist exactly once among the indexed
   runs. When an endpoint's `runId` is one of the duplicated run ids, no additional issue is
   produced for it -- the duplication issue alone already marks the result invalid, since
   resolving it arbitrarily would be a silent choice.
4. **Occurrence index out of range.** When a `runId` is indexed exactly once,
   `0 <= occurrenceIndex < run.Occurrences.Count` must hold. An index equal to
   `Occurrences.Count` is invalid. The domain already rejects a negative index at
   construction; the validator keeps the explicit lower-bound check anyway, for defensive
   robustness.
5. **Deterministic issue order.** Issues appear in exactly this order: project-id mismatch
   first; then duplicate indexed run ids, in run-index order; then decisions, in persisted
   order; and within each decision, the `Left` endpoint before the `Right` endpoint. Issues
   are never ordered by message text, timestamp, revision, or file name.

### Issue Types

`IdentityGovernanceEvidenceValidationResult` exposes `IsValid` and an immutable `Issues`
list. Each `IdentityGovernanceEvidenceValidationIssue` carries an explicit
`IdentityGovernanceEvidenceValidationIssueKind` plus the structured fields relevant to that
kind (`DecisionId`, `Side`, `RunId`, `OccurrenceIndex`, `OccurrenceCount`) so consumers can
branch without parsing `Message`. The four kinds are `ProjectIdMismatch`,
`DuplicateIndexedRunId`, `RunNotIndexed`, and `OccurrenceIndexOutOfRange`. `Message` is
deterministic and contains no stack trace and no file-system path.

## Success Output

Valid document with confirmations and rejections:

```text
Project: coordination-project
Indexed runs: 3
Decisions: 2
Confirmations: 1
Rejections: 1
Evidence endpoints: 4
Identity governance validation passed.
```

Valid empty document:

```text
Project: coordination-project
Indexed runs: 3
Decisions: 0
Confirmations: 0
Rejections: 0
Evidence endpoints: 0
Identity governance validation passed.
```

Exit code `0`, stderr empty.

## Invalid-Result Output

```text
Identity governance validation failed.
Issues: 2
1. Decision 'decision-1' Left endpoint references run id 'run-missing' at occurrence index 0, which is not indexed.
2. Decision 'decision-1' Right endpoint references occurrence index 999 in run 'run-002', which has 5 occurrence(s).
```

Exit code `1`, stdout empty, and usage is not printed -- usage is for parsing errors only.
No file is written, replaced, or created.

## Load Or Format Failure Output

```text
Failed to validate identity governance: <controlled message>
```

Exit code `1`, stdout empty, no stack trace. No file is written, replaced, or created, and
the report referenced by the project catalog is never touched.

## Parsing Rules

- `--project` and `--governance` are both required.
- Unknown options, duplicate options, missing values, a value token starting with `-`, and
  extra positional arguments are all rejected with usage on stderr.
- Option names are case-sensitive.
- Usage:

  ```text
  Usage: orzioclash validate-identity-governance --project <project.json> --governance <identity-governance.json>
  ```

## Workspace Protections Reused

The command reuses the same project-catalog workspace protections already applied to
`create-project`, `append-project-snapshot`, and `render-project`:

- The run index resolved from the project catalog must stay inside the project catalog's
  directory tree.
- Every resolved snapshot must stay inside that same directory tree.
- The report destination is resolved for workspace-consistency checks only; it is never
  read, written, or otherwise touched by this command.

## Deliberate Non-Goals

- No matcher-candidacy validation
- No run-adjacency validation
- No left/right-inversion validation
- No transitivity validation across decisions
- No graph-conflict detection
- No identity-merge validation
- No `Reopened` detection
- No decision supersession, revocation, or update workflow
- No reviewer-identity, timestamp, or responsibility validation
- No report projection
- No Clash Ledger
- No project-catalog, run-index, snapshot, or governance-file mutation
