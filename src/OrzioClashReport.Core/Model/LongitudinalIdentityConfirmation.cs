using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable projection of one explicit human ConfirmSameIdentity decision for longitudinal presentation.
    /// It represents only the two evidence endpoints named by the decision and never implies transitivity.
    /// </summary>
    public sealed class LongitudinalIdentityConfirmation
    {
        public LongitudinalIdentityConfirmation(
            string decisionId,
            string persistentIdentityId,
            ClashEvidenceEndpoint leftEvidence,
            ClashEvidenceEndpoint rightEvidence,
            string reviewerAlias,
            string? reason)
        {
            DecisionId = RequireValue(decisionId, nameof(decisionId));
            PersistentIdentityId = RequireValue(persistentIdentityId, nameof(persistentIdentityId));
            LeftEvidence = leftEvidence ?? throw new ArgumentNullException(nameof(leftEvidence));
            RightEvidence = rightEvidence ?? throw new ArgumentNullException(nameof(rightEvidence));
            ReviewerAlias = RequireValue(reviewerAlias, nameof(reviewerAlias));
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        }

        public string DecisionId { get; }

        public string PersistentIdentityId { get; }

        public ClashEvidenceEndpoint LeftEvidence { get; }

        public ClashEvidenceEndpoint RightEvidence { get; }

        public string ReviewerAlias { get; }

        public string? Reason { get; }

        private static string RequireValue(string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
            }

            return trimmed;
        }
    }
}
