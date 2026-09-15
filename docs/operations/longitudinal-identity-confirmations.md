# Human-confirmed identities in longitudinal reports

This source workflow projects explicit human identity decisions into the existing longitudinal HTML without changing the matching or lifecycle engines.

## Command

Build the solution, then run:

```powershell
dotnet run --project src/OrzioClashReport.GovernedProjectCli -- `
  --project .\project.json `
  --governance .\identity-governance.json
```

The command reads the project catalog, its run index, all indexed immutable snapshots, and the supplied identity-governance document. It validates project binding and every evidence endpoint before writing the project's configured longitudinal report.

## What is projected

Only `ConfirmSameIdentity` decisions are projected. Each rendered row contains:

- persistent identity id;
- decision id;
- exact left evidence endpoint;
- exact right evidence endpoint;
- reviewer alias;
- optional reason.

`RejectSameIdentity` decisions are intentionally absent from this longitudinal section. They remain available in the standalone identity-governance review report.

## Safety boundary

The projector does not:

- create a persistent identity;
- infer that two clashes are the same;
- propagate identity to another occurrence;
- infer transitivity;
- merge confirmed pairs into a graph;
- change matcher candidates or selected matches;
- change lifecycle classification;
- create `Reopened`;
- mutate snapshots, the run index, the project catalog, or the governance document.

Two explicit decisions `A = B` and `B = C` remain two explicit rows. This workflow does not create an `A = C` row.

## Validation and write order

The output is fail-closed:

1. project catalog is loaded;
2. run index and all snapshots are loaded;
3. governance is loaded;
4. project id and all evidence endpoints are validated;
5. the normal longitudinal analysis is recalculated from immutable snapshots;
6. explicit confirmation rows are projected;
7. the HTML is rendered completely;
8. `DerivedHtmlReportWriter` atomically replaces the configured report.

If validation fails, the existing report is not replaced.

## Packaging status

The command is a separate source executable, `orzioclash-governed`, so the existing `orzioclash.exe` preview contract remains unchanged while this workflow is evaluated. Packaging it into a future preview requires a separate release decision and smoke update.
