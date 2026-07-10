using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// A single, auditable signal recorded while classifying one <see cref="ClashLifecycleEntry"/>: what was
    /// checked (<see cref="Kind"/>), whether it supports or blocks an automatic classification
    /// (<see cref="Verdict"/>), and a human-readable explanation. This type only records a raw signal -- it
    /// does not interpret <see cref="Description"/>, and its <see cref="Verdict"/> is never derived
    /// automatically from anything stored here.
    /// </summary>
    public sealed class ClashLifecycleEvidence
    {
        public ClashLifecycleEvidenceKind Kind { get; }
        public ClashLifecycleEvidenceVerdict Verdict { get; }
        public string Description { get; }

        public ClashLifecycleEvidence(ClashLifecycleEvidenceKind kind, ClashLifecycleEvidenceVerdict verdict, string description)
        {
            if (!Enum.IsDefined(typeof(ClashLifecycleEvidenceKind), kind))
            {
                throw new ArgumentException($"'{kind}' is not a defined {nameof(ClashLifecycleEvidenceKind)}.", nameof(kind));
            }

            if (!Enum.IsDefined(typeof(ClashLifecycleEvidenceVerdict), verdict))
            {
                throw new ArgumentException($"'{verdict}' is not a defined {nameof(ClashLifecycleEvidenceVerdict)}.", nameof(verdict));
            }

            Kind = kind;
            Verdict = verdict;
            Description = RequireNonBlank(description, nameof(description));
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

        /// <summary>Human-readable representation for logs and test output, e.g. "MatchConfidence: SupportsClassification — Match confidence is High.". Not a key.</summary>
        public override string ToString() => $"{Kind}: {Verdict} — {Description}";
    }
}
