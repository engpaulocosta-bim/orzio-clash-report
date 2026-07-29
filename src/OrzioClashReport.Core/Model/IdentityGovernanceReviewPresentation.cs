using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable operational review presentation of one validated identity-governance document.</summary>
    public sealed class IdentityGovernanceReviewPresentation
    {
        public string ProjectId { get; }

        public string ProjectDisplayName { get; }

        public IReadOnlyList<IdentityGovernanceReviewDecisionPresentation> Decisions { get; }

        public int IndexedRunCount { get; }

        public int DecisionCount { get; }

        public int ConfirmationCount { get; }

        public int RejectionCount { get; }

        public int EvidenceEndpointCount { get; }

        public IdentityGovernanceReviewPresentation(
            string projectId,
            string projectDisplayName,
            int indexedRunCount,
            IReadOnlyList<IdentityGovernanceReviewDecisionPresentation> decisions)
        {
            if (indexedRunCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(indexedRunCount),
                    indexedRunCount,
                    "Indexed run count cannot be negative.");
            }

            ProjectId = RequireNonBlank(projectId, nameof(projectId));
            ProjectDisplayName = RequireNonBlank(projectDisplayName, nameof(projectDisplayName));
            IndexedRunCount = indexedRunCount;

            if (decisions == null)
            {
                throw new ArgumentNullException(nameof(decisions));
            }

            var copy = new List<IdentityGovernanceReviewDecisionPresentation>(decisions.Count);
            int confirmations = 0;
            int rejections = 0;

            for (int i = 0; i < decisions.Count; i++)
            {
                IdentityGovernanceReviewDecisionPresentation decision = decisions[i]
                    ?? throw new ArgumentException($"Decision presentation at index {i} cannot be null.", nameof(decisions));

                copy.Add(decision);

                switch (decision.DecisionKind)
                {
                    case HumanIdentityDecisionKind.ConfirmSameIdentity:
                        confirmations++;
                        break;
                    case HumanIdentityDecisionKind.RejectSameIdentity:
                        rejections++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(decisions),
                            decision.DecisionKind,
                            "Decision kind is not supported.");
                }
            }

            Decisions = copy.AsReadOnly();
            DecisionCount = Decisions.Count;
            ConfirmationCount = confirmations;
            RejectionCount = rejections;
            EvidenceEndpointCount = DecisionCount * 2;
        }

        private static string RequireNonBlank(string value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
            }

            return trimmed;
        }
    }
}
