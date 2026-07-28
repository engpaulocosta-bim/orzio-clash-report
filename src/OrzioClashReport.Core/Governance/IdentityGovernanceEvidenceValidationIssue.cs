using System;

namespace OrzioClashReport.Core.Governance
{
    /// <summary>
    /// Immutable, explicitly typed evidence-validation issue. Consumers can branch on <see cref="Kind"/> and the
    /// structured fields relevant to it instead of parsing <see cref="Message"/>. <see cref="Message"/> is
    /// deterministic, contains no stack trace, and contains no file-system path.
    /// </summary>
    public sealed class IdentityGovernanceEvidenceValidationIssue
    {
        public IdentityGovernanceEvidenceValidationIssueKind Kind { get; }

        /// <summary>Set for <see cref="IdentityGovernanceEvidenceValidationIssueKind.RunNotIndexed"/> and <see cref="IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange"/>; otherwise <c>null</c>.</summary>
        public string? DecisionId { get; }

        /// <summary>Set for <see cref="IdentityGovernanceEvidenceValidationIssueKind.RunNotIndexed"/> and <see cref="IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange"/>; otherwise <c>null</c>.</summary>
        public IdentityGovernanceEvidenceEndpointSide? Side { get; }

        /// <summary>Set for every kind except <see cref="IdentityGovernanceEvidenceValidationIssueKind.ProjectIdMismatch"/>; otherwise <c>null</c>.</summary>
        public string? RunId { get; }

        /// <summary>Set for <see cref="IdentityGovernanceEvidenceValidationIssueKind.RunNotIndexed"/> and <see cref="IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange"/>; otherwise <c>null</c>.</summary>
        public int? OccurrenceIndex { get; }

        /// <summary>Set only for <see cref="IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange"/>; otherwise <c>null</c>.</summary>
        public int? OccurrenceCount { get; }

        public string Message { get; }

        internal IdentityGovernanceEvidenceValidationIssue(
            IdentityGovernanceEvidenceValidationIssueKind kind,
            string? decisionId,
            IdentityGovernanceEvidenceEndpointSide? side,
            string? runId,
            int? occurrenceIndex,
            int? occurrenceCount,
            string message)
        {
            Kind = kind;
            DecisionId = decisionId;
            Side = side;
            RunId = runId;
            OccurrenceIndex = occurrenceIndex;
            OccurrenceCount = occurrenceCount;
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>Returns <see cref="Message"/>. Not a persistence key.</summary>
        public override string ToString() => Message;
    }
}
