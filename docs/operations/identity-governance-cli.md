# Identity Governance CLI

## Scope

This document describes the source-only Step 29A/29B identity-governance CLI workflow that
exists in the repository as of July 28, 2026. It is not part of the published
`v0.1.0-preview.2` binary contract.

The workflow is intentionally narrow:

- Create one empty governance document
- Append one explicit human decision to an existing governance document
- Preserve immutable snapshots as evidence only
- Preserve existing decisions and their order
- Replace the governance file only after a complete temporary write succeeds

It does not validate against snapshots, load project catalogs or run indexes for identity
semantics, infer or propagate identity, act as an interactive review workflow, or serve as
a Clash Ledger.

## Create One Empty Governance File

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  create-identity-governance \
  --project-id coordination-project \
  -o identity-governance.json
```

Contract:

- Creates `IdentityGovernanceDocument` with the supplied `projectId`
- Persists `decisions: []`
- Uses create-new save semantics and refuses an existing output path
- Does not create the parent directory
- Does not generate ids, timestamps, users, or any other metadata

Success stdout:

```text
Project: coordination-project
Decisions: 0
Identity governance written to identity-governance.json
```

## Append One Explicit Decision

Confirm:

```bash
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
```

Reject:

```bash
dotnet run --project src/OrzioClashReport.Cli -- \
  append-identity-decision \
  --governance identity-governance.json \
  --decision-id decision-002 \
  --decision-kind RejectSameIdentity \
  --left-run-id run-002 \
  --left-occurrence-index 9 \
  --right-run-id run-003 \
  --right-occurrence-index 3 \
  --reviewer-alias coordinator-a \
  --reason "Different physical clashes"
```

Contract:

- Loads strict schema-v1 governance JSON
- Uses the project id already stored in the governance file
- Preserves all existing decisions and their order
- Appends exactly one new decision at the end
- Preserves the supplied left/right endpoint orientation
- Replaces the existing file only after a complete temporary write succeeds
- Leaves the original bytes untouched on conflict or any failure before replacement

Success stdout:

```text
Project: coordination-project
Decision: decision-001
Decision kind: ConfirmSameIdentity
Decisions: 1
Identity governance updated: identity-governance.json
```

## Parsing Rules

- `--decision-kind` is case-sensitive and accepts only `ConfirmSameIdentity` or
  `RejectSameIdentity`
- `--left-occurrence-index` and `--right-occurrence-index` must be decimal non-negative
  integers
- `ConfirmSameIdentity` requires `--persistent-identity-id`
- `RejectSameIdentity` forbids `--persistent-identity-id`
- Unknown options, duplicate options, missing values, missing required options, and extra
  arguments are rejected with usage on stderr

## Deliberate Non-Goals

- No validation that `runId` exists in a run index
- No validation that `occurrenceIndex` exists in a snapshot
- No loading of project catalogs, run indexes, or snapshots for governance semantics
- No report projection
- No automatic identity propagation or transitivity
- No update, delete, revoke, supersede, pending, or conflict-resolution workflow
