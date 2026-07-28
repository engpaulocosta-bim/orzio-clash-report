using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable explicit human decision about whether two snapshot evidence endpoints share one persistent identity.</summary>
    public sealed class HumanIdentityDecision
    {
        public const int MaxIdentifierLength = 128;
        public const int MaxReasonLength = 500;

        public string DecisionId { get; }

        public string ProjectId { get; }

        public HumanIdentityDecisionKind DecisionKind { get; }

        public ClashEvidenceEndpoint LeftEvidence { get; }

        public ClashEvidenceEndpoint RightEvidence { get; }

        public string? PersistentIdentityId { get; }

        public string ReviewerAlias { get; }

        public string? Reason { get; }

        public HumanIdentityDecision(
            string decisionId,
            string projectId,
            HumanIdentityDecisionKind decisionKind,
            ClashEvidenceEndpoint leftEvidence,
            ClashEvidenceEndpoint rightEvidence,
            string? persistentIdentityId,
            string reviewerAlias,
            string? reason)
        {
            DecisionId = RequireIdentifier(decisionId, nameof(decisionId));
            ProjectId = RequireIdentifier(projectId, nameof(projectId));
            LeftEvidence = leftEvidence ?? throw new ArgumentNullException(nameof(leftEvidence));
            RightEvidence = rightEvidence ?? throw new ArgumentNullException(nameof(rightEvidence));

            if (LeftEvidence.Equals(RightEvidence))
            {
                throw new ArgumentException(
                    "Left and right evidence endpoints must be different.",
                    nameof(rightEvidence));
            }

            DecisionKind = decisionKind;
            ReviewerAlias = RequireIdentifier(reviewerAlias, nameof(reviewerAlias));
            Reason = NormalizeOptionalReason(reason, nameof(reason));

            string? normalizedPersistentIdentityId = NormalizeOptionalIdentifier(
                persistentIdentityId,
                nameof(persistentIdentityId));

            switch (decisionKind)
            {
                case HumanIdentityDecisionKind.ConfirmSameIdentity:
                    if (normalizedPersistentIdentityId == null)
                    {
                        throw new ArgumentException(
                            "ConfirmSameIdentity requires a persistent identity id.",
                            nameof(persistentIdentityId));
                    }

                    break;

                case HumanIdentityDecisionKind.RejectSameIdentity:
                    if (normalizedPersistentIdentityId != null)
                    {
                        throw new ArgumentException(
                            "RejectSameIdentity cannot carry a persistent identity id.",
                            nameof(persistentIdentityId));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(decisionKind),
                        decisionKind,
                        "Decision kind is not supported.");
            }

            PersistentIdentityId = normalizedPersistentIdentityId;
        }

        /// <summary>Human-readable representation for logs and test output. Not a persistent identity mapping by itself.</summary>
        public override string ToString() => $"{DecisionId}: {DecisionKind} ({LeftEvidence} <> {RightEvidence})";

        private static string RequireIdentifier(string value, string paramName)
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

            if (trimmed.Length > MaxIdentifierLength)
            {
                throw new ArgumentException(
                    $"Value cannot exceed {MaxIdentifierLength} characters after trimming.",
                    paramName);
            }

            return trimmed;
        }

        private static string? NormalizeOptionalIdentifier(string? value, string paramName)
        {
            if (value == null)
            {
                return null;
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            if (trimmed.Length > MaxIdentifierLength)
            {
                throw new ArgumentException(
                    $"Value cannot exceed {MaxIdentifierLength} characters after trimming.",
                    paramName);
            }

            return trimmed;
        }

        private static string? NormalizeOptionalReason(string? value, string paramName)
        {
            if (value == null)
            {
                return null;
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            if (trimmed.Length > MaxReasonLength)
            {
                throw new ArgumentException(
                    $"Value cannot exceed {MaxReasonLength} characters after trimming.",
                    paramName);
            }

            return trimmed;
        }
    }
}
