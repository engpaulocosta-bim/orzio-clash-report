# Controlled Pilot Evaluation Guide

Use this guide for the internal controlled pilot of `v0.1.0-preview.3`.

## Audience

- BIM coordinator
- BIM manager
- Coordination specialist
- Authorized technical evaluator

## Prerequisites

- Windows `x64`
- Authorized local execution
- Authorized Navisworks exports
- Controlled workspace
- Human-reviewed manifests
- Backup of all inputs
- Authorization to process the data

## Evaluation Scenario

Run the pilot in this order:

1. Verify the ZIP checksum.
2. Run `orzioclash.exe --version`.
3. Run `orzioclash.exe --help`.
4. Generate one single-run report.
5. Create three snapshots.
6. Create one run index.
7. Generate one longitudinal report.
8. Create one project catalog.
9. Create one governance document.
10. Append one confirmation.
11. Append one rejection.
12. Validate the governance document.
13. Render the standalone review report.
14. Capture feedback without sharing private data.

## Success Criteria

- Commands complete without crashes.
- Schemas are correct.
- Outputs are readable.
- Decisions are represented correctly.
- Invalid references are detected.
- Inputs are preserved.
- No private paths appear in the review report.
- Limitations are understood.
- Feedback is reproducible.

## Stop Criteria

Stop the pilot if any of these occur:

- unexpected input mutation
- private path leakage in a report
- incorrect decision representation
- inconsistent schema
- crash
- non-deterministic output
- uncertainty about data authorization

## Feedback Template

Request only:

- version
- command
- exit code
- redacted stdout/stderr
- observed behavior
- minimum reproduction steps
- confirmation that the shared material is anonymized

Do not automatically request XML, snapshots, real HTML, client names, personal names, full
paths, email addresses, or credentials.

## Allowed Claims

- internal controlled pilot
- explicit human decisions
- deterministic local reports
- immutable evidence snapshots
- read-only evidence validation

## Prohibited Claims

- final product
- automatic identity
- Clash Ledger
- complete historical workflow
- replacement of human coordination
- production accuracy
- legal certification

## Privacy and Handling

- The package contains anonymized fixtures only.
- Outputs remain local unless separately authorized.
- The review report does not project raw `ClashObject.SourceModel`.
- Real data requires explicit authorization.

## Distribution

Distribution remains private and authorized by the owner.
Legal distribution terms remain an owner decision.
