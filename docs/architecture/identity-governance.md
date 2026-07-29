# Identity Governance

## Purpose

Steps 29A, 29B, 29C, and 30A add a narrow, explicit, source-only human identity-governance
workflow. It exists in the source tree as of July 29, 2026 and is not part of the published
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

The authoring CLI itself does not load project catalogs, run indexes, or snapshots for
validation. It uses the project id already stored in the governance file, preserves
existing decisions and their order, and replaces the existing file only after a complete
temporary write succeeds.

## CLI Evidence Validation Scope

Step 29C adds one explicit, read-only source workflow: `validate-identity-governance`. It
loads a project catalog, resolves and loads the project's indexed run snapshots, loads a
governance document, and validates two things only:

- The governance document's declared project id matches the project catalog's project id.
- Every decision's `runId` + `occurrenceIndex` evidence endpoint resolves to a real
  occurrence slot inside exactly one indexed snapshot.

It never writes, replaces, or creates any file; never renders HTML; never runs matching,
lifecycle classification, or continuity analysis; never reorders runs, decisions, or
endpoints; and never requires a minimum number of indexed snapshots -- it validates
evidence, not longitudinal comparison. It works correctly with zero decisions, one indexed
run, or many indexed runs, and decisions may reference adjacent or non-adjacent runs
identically, since only project binding and endpoint existence are in scope.

## Standalone Review Report Scope

Step 30A adds one explicit, source-only, derived HTML review workflow:
`render-identity-governance-report`. It loads one project catalog, its indexed snapshots,
and one governance document, requires Step 29C evidence validation to pass, and then
renders one standalone, self-contained HTML review of the persisted human decisions.

The report is operational and regenerable only:

- It is not primary evidence.
- It does not change the governance document.
- It does not change snapshots.
- It does not change the project catalog schema.
- It does not change the longitudinal report path or bytes referenced by the project
  catalog.

It presents persisted decisions exactly in persisted order, Left endpoint before Right
endpoint, with run resolution still based only on `runId` + `occurrenceIndex`. It does not
run matching, lifecycle, continuity, grouping, identity inference, propagation,
transitivity, Clash Ledger, or `Reopened`.

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
- No matcher-candidacy, run-adjacency, or left/right-inversion validation
- No projection of human decisions into the existing longitudinal report
- No grouping of decisions by `persistentIdentityId`
- No graph merge or transitive closure over decisions
- No project-catalog, run-index, or snapshot mutation

Step 29C narrows exactly one of these: it does validate that a decision's `runId` and
`occurrenceIndex` resolve to a real occurrence slot inside one indexed snapshot for the
correct project. It still does not validate matcher candidacy, run adjacency, left/right
ordering intent, transitivity across decisions, graph conflicts, identity merges,
reopening, decision supersession, reviewer identity, timestamps, or responsibility, and it
still never projects decisions into a report.

## Current Limits

This stage is intentionally narrow:

- Domain model for explicit human identity decisions
- Deterministic Core validation
- Deterministic schema-v1 JSON serialization and parsing
- Safe replace-existing persistence for governance files
- Explicit CLI creation and append workflows
- Read-only evidence validation of project binding and evidence endpoints against indexed
  snapshots
- Unit and contract tests

It does not change published preview.2 behavior, snapshots, run indexes, project catalogs,
lifecycle classification, continuity projection, or existing HTML renderers.
