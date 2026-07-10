using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// A single, auditable piece of evidence comparing two clash occurrences: what was compared
    /// (<see cref="Kind"/>), what it says (<see cref="Verdict"/>), and a human-readable explanation. This type
    /// only records a raw signal -- it does not interpret values, compute a distance or score, or derive its
    /// own verdict from the values it carries.
    /// </summary>
    public sealed class MatchEvidence
    {
        public MatchEvidenceKind Kind { get; }
        public MatchEvidenceVerdict Verdict { get; }
        public string Description { get; }
        public string? PreviousValue { get; }
        public string? CurrentValue { get; }

        public MatchEvidence(
            MatchEvidenceKind kind, MatchEvidenceVerdict verdict, string description,
            string? previousValue, string? currentValue)
        {
            if (!Enum.IsDefined(typeof(MatchEvidenceKind), kind))
            {
                throw new ArgumentException($"'{kind}' is not a defined {nameof(MatchEvidenceKind)}.", nameof(kind));
            }

            if (!Enum.IsDefined(typeof(MatchEvidenceVerdict), verdict))
            {
                throw new ArgumentException($"'{verdict}' is not a defined {nameof(MatchEvidenceVerdict)}.", nameof(verdict));
            }

            Kind = kind;
            Verdict = verdict;
            Description = RequireNonBlank(description, nameof(description));
            PreviousValue = NormalizeOptional(previousValue);
            CurrentValue = NormalizeOptional(currentValue);
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

        /// <summary>Human-readable representation for logs and test output, e.g. "SourceClashGuid: Supports — source GUIDs are equal". Not a key.</summary>
        public override string ToString() => $"{Kind}: {Verdict} — {Description}";
    }
}
