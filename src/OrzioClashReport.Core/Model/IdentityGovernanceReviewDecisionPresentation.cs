using System;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable operational review view of one persisted human identity decision.</summary>
    public sealed class IdentityGovernanceReviewDecisionPresentation
    {
        public int DecisionNumber { get; }

        public string DecisionId { get; }

        public HumanIdentityDecisionKind DecisionKind { get; }

        public string ReviewerAlias { get; }

        public string? Reason { get; }

        public string? PersistentIdentityId { get; }

        public IdentityGovernanceReviewEndpointPresentation LeftEndpoint { get; }

        public IdentityGovernanceReviewEndpointPresentation RightEndpoint { get; }

        public IdentityGovernanceReviewDecisionPresentation(
            int decisionNumber,
            string decisionId,
            HumanIdentityDecisionKind decisionKind,
            string reviewerAlias,
            string? reason,
            string? persistentIdentityId,
            IdentityGovernanceReviewEndpointPresentation leftEndpoint,
            IdentityGovernanceReviewEndpointPresentation rightEndpoint)
        {
            if (decisionNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decisionNumber),
                    decisionNumber,
                    "Decision number must be positive.");
            }

            LeftEndpoint = leftEndpoint ?? throw new ArgumentNullException(nameof(leftEndpoint));
            RightEndpoint = rightEndpoint ?? throw new ArgumentNullException(nameof(rightEndpoint));

            if (LeftEndpoint.Side != Governance.IdentityGovernanceEvidenceEndpointSide.Left)
            {
                throw new ArgumentException("Left endpoint must declare the Left side.", nameof(leftEndpoint));
            }

            if (RightEndpoint.Side != Governance.IdentityGovernanceEvidenceEndpointSide.Right)
            {
                throw new ArgumentException("Right endpoint must declare the Right side.", nameof(rightEndpoint));
            }

            DecisionNumber = decisionNumber;
            DecisionId = RequireNonBlank(decisionId, nameof(decisionId));
            DecisionKind = decisionKind;
            ReviewerAlias = RequireNonBlank(reviewerAlias, nameof(reviewerAlias));
            Reason = NormalizeOptional(reason);
            PersistentIdentityId = NormalizeOptional(persistentIdentityId);
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

        private static string? NormalizeOptional(string? value)
        {
            if (value == null)
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}
