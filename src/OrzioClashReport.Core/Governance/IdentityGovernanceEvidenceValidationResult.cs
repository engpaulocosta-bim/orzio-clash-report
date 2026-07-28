using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Governance
{
    /// <summary>
    /// Immutable, deterministic result of validating an <see cref="IdentityGovernanceDocument"/>'s project
    /// binding and evidence endpoints against a set of indexed <see cref="CoordinationRun"/> snapshots.
    /// This is read-only evidence validation: it never mutates the governance document, the indexed runs, or any
    /// file, and it never infers, propagates, or persists identity.
    /// </summary>
    public sealed class IdentityGovernanceEvidenceValidationResult
    {
        public bool IsValid => Issues.Count == 0;

        public IReadOnlyList<IdentityGovernanceEvidenceValidationIssue> Issues { get; }

        internal IdentityGovernanceEvidenceValidationResult(IReadOnlyList<IdentityGovernanceEvidenceValidationIssue> issues)
        {
            if (issues == null)
            {
                throw new ArgumentNullException(nameof(issues));
            }

            var copy = new List<IdentityGovernanceEvidenceValidationIssue>(issues);
            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] == null)
                {
                    throw new ArgumentException($"Issue at index {i} cannot be null.", nameof(issues));
                }
            }

            Issues = copy.AsReadOnly();
        }

        /// <summary>Human-readable representation for logs and test output. Not a persistence key.</summary>
        public override string ToString() =>
            IsValid
                ? "Identity governance evidence validation passed."
                : $"Identity governance evidence validation failed: {Issues.Count} issue(s).";
    }
}
