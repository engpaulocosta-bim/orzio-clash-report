# Identity Governance

## Purpose

Step 29A adds the minimum source-only foundation required to represent explicit human
identity decisions over immutable clash evidence. It exists in the source tree as of July
28, 2026 and is not part of the published `v0.1.0-preview.2` binary contract.

## Evidence, Suggestion, Decision

- A `CoordinationRun` snapshot remains immutable evidence only.
- Algorithmic matching remains a suggestion only. It can support review, but it is never
  persisted as truth.
- A persistent clash identity can be assigned only through an explicit human
  `ConfirmSameIdentity` decision.

## Allowed Decision Types

- `ConfirmSameIdentity`
- `RejectSameIdentity`

No other decision kind exists in Step 29A.

## Persistent Identity Rule

- `ConfirmSameIdentity` requires an explicit `persistentIdentityId`.
- `RejectSameIdentity` must not carry a persistent identity id.
- Absence of a decision means only absence of a decision. Step 29A does not introduce
  `Pending`, `NeedsReview`, or any equivalent persisted state.

## Evidence Endpoint Rule

Each decision references two explicit evidence endpoints. The foundation reuses the existing
immutable snapshot vocabulary: `runId` plus preserved `occurrenceIndex` inside that run's
ordered occurrence list. No new automatic fingerprint, inferred clash key, or synthetic
cross-run identity is introduced.

## Provenance Rule

`reviewerAlias` is an operational alias such as `coordinator-a`. The domain does not require
email, personal name, external login, or any other personal data. Step 29A does not attach
automatic human-decision timestamps.

## Non-Goals in Step 29A

- No automatic identity assignment
- No automatic identity propagation to other clashes
- No automatic transitivity
- No automatic chronology
- No automatic responsibility or assignee data
- No Clash Ledger
- No `Reopened`
- No CLI review workflow
- No report projection of human decisions

## JSON Persistence Scope

`OrzioClashReport.Persistence.IdentityGovernanceJson` owns a strict deterministic
schema-v1 JSON contract for a project-scoped set of explicit human identity decisions.
It uses exact camelCase property names, UTF-8 without BOM, LF line endings, strict enum
reading, duplicate-property rejection, create-new save semantics, and no silent conflict
resolution.

## Step 29A Limits

This stage is intentionally narrow:

- Domain model for explicit human identity decisions
- Deterministic Core validation
- Deterministic JSON serialization and parsing
- Unit and contract tests

It does not change CLI behavior, published preview.2 release behavior, snapshots, run
indexes, project catalogs, lifecycle classification, continuity projection, or existing
HTML renderers.

## Possible Next Steps for 29B

- Add an explicit CLI workflow for reviewing and appending human decisions
- Load identity-governance JSON alongside immutable evidence for analysis
- Project confirmed human identity information into new operational outputs
- Define safe governance around conflict resolution, replacement, and audit history

Those are possible next steps only. Step 29A does not implement them.
