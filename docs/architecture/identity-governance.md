# Identity Governance

## Purpose

Steps 29A and 29B add a narrow, explicit, source-only human identity-governance workflow.
It exists in the source tree as of July 28, 2026 and is not part of the published
`v0.1.0-preview.2` binary contract.

## Evidence, Suggestion, Decision

- A `CoordinationRun` snapshot remains immutable evidence only.
- Algorithmic matching remains suggestion only. It can support review, but it is never
  persisted as truth.
- Persistent clash identity exists only when a human writes an explicit
  `ConfirmSameIdentity` decision.

## Allowed Decision Types

- `ConfirmSameIdentity`
- `RejectSameIdentity`

No other decision kind exists in this stage.

## Persistent Identity Rule

- `ConfirmSameIdentity` requires an explicit `persistentIdentityId`.
- `RejectSameIdentity` must not carry a persistent identity id.
- Absence of a decision means only absence of a decision. No persisted `Pending`,
  `NeedsReview`, `Superseded`, `Revoked`, or equivalent state exists.

## Evidence Endpoint Rule

Each decision references two explicit evidence endpoints. The workflow reuses immutable
snapshot vocabulary: `runId` plus preserved `occurrenceIndex` inside that run's ordered
occurrence list. No fingerprint, inferred clash key, synthetic cross-run identity, or
automatic left/right inversion is introduced.

## Provenance Rule

`reviewerAlias` is an operational alias such as `coordinator-a`. The domain does not
require email, personal name, external login, or any other personal data. This stage does
not attach automatic timestamps.

## CLI Authoring Scope

Step 29B adds only two explicit non-interactive source workflows:

- `create-identity-governance` creates one empty governance document for one project.
- `append-identity-decision` appends one explicit human decision to the end of an existing
  governance document.

The CLI does not load project catalogs, run indexes, or snapshots for validation. It uses
the project id already stored in the governance file, preserves existing decisions and
their order, and replaces the existing file only after a complete temporary write succeeds.

## JSON Persistence Scope

`OrzioClashReport.Persistence.IdentityGovernanceJson` owns a strict deterministic
schema-v1 JSON contract for a project-scoped set of explicit human identity decisions.
It uses exact camelCase property names, UTF-8 without BOM, LF line endings, strict enum
reading, duplicate-property rejection, create-new save semantics, and replace-existing
semantics that preserve the original file on failure.

## Non-Goals in This Stage

- No automatic identity assignment
- No automatic identity propagation to other clashes
- No automatic transitivity
- No automatic chronology
- No automatic responsibility or assignee data
- No Clash Ledger
- No `Reopened`
- No interactive review UI or prompt flow
- No snapshot validation of `runId` or `occurrenceIndex`
- No report projection of human decisions
- No project-catalog, run-index, or snapshot mutation

## Current Limits

This stage is intentionally narrow:

- Domain model for explicit human identity decisions
- Deterministic Core validation
- Deterministic schema-v1 JSON serialization and parsing
- Safe replace-existing persistence for governance files
- Explicit CLI creation and append workflows
- Unit and contract tests

It does not change published preview.2 behavior, snapshots, run indexes, project catalogs,
lifecycle classification, continuity projection, or existing HTML renderers.
