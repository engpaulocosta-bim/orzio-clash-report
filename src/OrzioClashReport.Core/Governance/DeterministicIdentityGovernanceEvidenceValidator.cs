using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Governance
{
    /// <summary>
    /// A deterministic <see cref="IIdentityGovernanceEvidenceValidator"/>: compares the governance document's
    /// declared project id against the expected project id, detects indexed runs that share the same run id
    /// (making resolution ambiguous), and validates every decision's <see cref="ClashEvidenceEndpoint"/> against
    /// the indexed runs -- an endpoint is valid only when its run id is indexed exactly once and its occurrence
    /// index falls inside that run's occurrence range. All comparisons are ordinal; nothing is normalized.
    /// </summary>
    public sealed class DeterministicIdentityGovernanceEvidenceValidator : IIdentityGovernanceEvidenceValidator
    {
        public IdentityGovernanceEvidenceValidationResult Validate(
            string expectedProjectId,
            IdentityGovernanceDocument governance,
            IReadOnlyList<CoordinationRun> indexedRuns)
        {
            if (expectedProjectId == null)
            {
                throw new ArgumentNullException(nameof(expectedProjectId));
            }

            if (governance == null)
            {
                throw new ArgumentNullException(nameof(governance));
            }

            if (indexedRuns == null)
            {
                throw new ArgumentNullException(nameof(indexedRuns));
            }

            for (int i = 0; i < indexedRuns.Count; i++)
            {
                if (indexedRuns[i] == null)
                {
                    throw new ArgumentException($"Indexed run at index {i} cannot be null.", nameof(indexedRuns));
                }
            }

            var issues = new List<IdentityGovernanceEvidenceValidationIssue>();

            // 1. Project id (ordinal, never normalized).
            if (!string.Equals(governance.ProjectId, expectedProjectId, StringComparison.Ordinal))
            {
                issues.Add(new IdentityGovernanceEvidenceValidationIssue(
                    IdentityGovernanceEvidenceValidationIssueKind.ProjectIdMismatch,
                    decisionId: null,
                    side: null,
                    runId: null,
                    occurrenceIndex: null,
                    occurrenceCount: null,
                    message: $"Project id mismatch: governance declares '{governance.ProjectId}', expected '{expectedProjectId}'."));
            }

            // 2. Duplicate indexed run ids, in run-index order. A run id seen more than once produces one issue
            // per occurrence after the first.
            var runCountByRunId = new Dictionary<string, int>(StringComparer.Ordinal);
            var firstRunByRunId = new Dictionary<string, CoordinationRun>(StringComparer.Ordinal);

            foreach (var run in indexedRuns)
            {
                runCountByRunId.TryGetValue(run.RunId, out int existingCount);
                runCountByRunId[run.RunId] = existingCount + 1;

                if (existingCount == 0)
                {
                    firstRunByRunId[run.RunId] = run;
                }
            }

            for (int i = 0; i < indexedRuns.Count; i++)
            {
                var run = indexedRuns[i];
                if (runCountByRunId[run.RunId] <= 1)
                {
                    continue;
                }

                bool isFirstOccurrence = ReferenceEquals(run, firstRunByRunId[run.RunId]);
                if (isFirstOccurrence)
                {
                    continue;
                }

                issues.Add(new IdentityGovernanceEvidenceValidationIssue(
                    IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId,
                    decisionId: null,
                    side: null,
                    runId: run.RunId,
                    occurrenceIndex: null,
                    occurrenceCount: null,
                    message: $"Run id '{run.RunId}' is duplicated among indexed runs (duplicate at indexed position {i})."));
            }

            // 3 & 4. Every decision's evidence endpoints, in persisted order, Left before Right.
            foreach (var decision in governance.Decisions)
            {
                ValidateEndpoint(
                    decision.DecisionId,
                    IdentityGovernanceEvidenceEndpointSide.Left,
                    decision.LeftEvidence,
                    runCountByRunId,
                    firstRunByRunId,
                    issues);

                ValidateEndpoint(
                    decision.DecisionId,
                    IdentityGovernanceEvidenceEndpointSide.Right,
                    decision.RightEvidence,
                    runCountByRunId,
                    firstRunByRunId,
                    issues);
            }

            return new IdentityGovernanceEvidenceValidationResult(issues);
        }

        private static void ValidateEndpoint(
            string decisionId,
            IdentityGovernanceEvidenceEndpointSide side,
            ClashEvidenceEndpoint endpoint,
            Dictionary<string, int> runCountByRunId,
            Dictionary<string, CoordinationRun> firstRunByRunId,
            List<IdentityGovernanceEvidenceValidationIssue> issues)
        {
            if (!runCountByRunId.TryGetValue(endpoint.RunId, out int count))
            {
                issues.Add(new IdentityGovernanceEvidenceValidationIssue(
                    IdentityGovernanceEvidenceValidationIssueKind.RunNotIndexed,
                    decisionId,
                    side,
                    endpoint.RunId,
                    endpoint.OccurrenceIndex,
                    occurrenceCount: null,
                    message: $"Decision '{decisionId}' {side} endpoint references run id '{endpoint.RunId}' at "
                        + $"occurrence index {endpoint.OccurrenceIndex}, which is not indexed."));
                return;
            }

            if (count > 1)
            {
                // The duplication issue already makes the result invalid; resolving to one of the ambiguous
                // runs arbitrarily would be a silent choice, so no additional issue is produced here.
                return;
            }

            var run = firstRunByRunId[endpoint.RunId];

            if (endpoint.OccurrenceIndex < 0 || endpoint.OccurrenceIndex >= run.Occurrences.Count)
            {
                issues.Add(new IdentityGovernanceEvidenceValidationIssue(
                    IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange,
                    decisionId,
                    side,
                    endpoint.RunId,
                    endpoint.OccurrenceIndex,
                    run.Occurrences.Count,
                    message: $"Decision '{decisionId}' {side} endpoint references occurrence index "
                        + $"{endpoint.OccurrenceIndex} in run '{endpoint.RunId}', which has {run.Occurrences.Count} occurrence(s)."));
            }
        }
    }
}
