using System;
using System.Collections.Generic;
using System.Globalization;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Governance;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Presentation
{
    /// <summary>Deterministically resolves validated governance evidence endpoints into an immutable operational review presentation.</summary>
    public sealed class DeterministicIdentityGovernanceReviewPresenter : IIdentityGovernanceReviewPresenter
    {
        public IdentityGovernanceReviewPresentation Present(
            string projectId,
            string projectDisplayName,
            IdentityGovernanceDocument governance,
            IReadOnlyList<CoordinationRun> indexedRuns)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (projectDisplayName == null)
            {
                throw new ArgumentNullException(nameof(projectDisplayName));
            }

            if (governance == null)
            {
                throw new ArgumentNullException(nameof(governance));
            }

            if (indexedRuns == null)
            {
                throw new ArgumentNullException(nameof(indexedRuns));
            }

            var runById = new Dictionary<string, CoordinationRun>(StringComparer.Ordinal);
            for (int i = 0; i < indexedRuns.Count; i++)
            {
                CoordinationRun run = indexedRuns[i]
                    ?? throw new ArgumentException($"Indexed run at index {i} cannot be null.", nameof(indexedRuns));

                if (runById.ContainsKey(run.RunId))
                {
                    throw new InvalidOperationException(
                        $"Indexed runs contain duplicate run id '{run.RunId}', so review endpoints cannot be resolved deterministically.");
                }

                runById.Add(run.RunId, run);
            }

            var decisions = new List<IdentityGovernanceReviewDecisionPresentation>(governance.Decisions.Count);
            for (int i = 0; i < governance.Decisions.Count; i++)
            {
                HumanIdentityDecision decision = governance.Decisions[i];
                var leftEndpoint = ResolveEndpoint(IdentityGovernanceEvidenceEndpointSide.Left, decision.LeftEvidence, runById);
                var rightEndpoint = ResolveEndpoint(IdentityGovernanceEvidenceEndpointSide.Right, decision.RightEvidence, runById);

                decisions.Add(
                    new IdentityGovernanceReviewDecisionPresentation(
                        i + 1,
                        decision.DecisionId,
                        decision.DecisionKind,
                        decision.ReviewerAlias,
                        decision.Reason,
                        decision.PersistentIdentityId,
                        leftEndpoint,
                        rightEndpoint));
            }

            return new IdentityGovernanceReviewPresentation(projectId, projectDisplayName, indexedRuns.Count, decisions);
        }

        private static IdentityGovernanceReviewEndpointPresentation ResolveEndpoint(
            IdentityGovernanceEvidenceEndpointSide side,
            ClashEvidenceEndpoint endpoint,
            Dictionary<string, CoordinationRun> runById)
        {
            if (!runById.TryGetValue(endpoint.RunId, out CoordinationRun? run))
            {
                throw new InvalidOperationException(
                    $"Validated governance endpoint references run id '{endpoint.RunId}', but that run is unavailable during presentation.");
            }

            if (endpoint.OccurrenceIndex < 0 || endpoint.OccurrenceIndex >= run.Occurrences.Count)
            {
                throw new InvalidOperationException(
                    $"Validated governance endpoint references occurrence index {endpoint.OccurrenceIndex} in run '{run.RunId}', "
                    + $"but the run exposes {run.Occurrences.Count.ToString(CultureInfo.InvariantCulture)} occurrence(s) during presentation.");
            }

            ClashOccurrence occurrence = run.Occurrences[endpoint.OccurrenceIndex];
            ClashResult clash = occurrence.Clash;

            return new IdentityGovernanceReviewEndpointPresentation(
                side,
                run.RunId,
                endpoint.OccurrenceIndex,
                run.CreatedAt,
                occurrence.ClashTestName,
                clash.Status,
                FormatModel(occurrence.ModelA),
                FormatModel(occurrence.ModelB),
                clash.ElementA.ElementId,
                clash.ElementA.ElementName,
                clash.ElementA.Level,
                clash.ElementB.ElementId,
                clash.ElementB.ElementName,
                clash.ElementB.Level);
        }

        private static string FormatModel(ModelRevision model) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} / {1} / {2} @ {3}",
                model.Identity.Company,
                model.Identity.Discipline,
                model.Identity.ModelName,
                model.Revision);
    }
}
