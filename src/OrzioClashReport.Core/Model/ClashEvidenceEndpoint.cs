using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable reference to one exact occurrence slot inside one immutable coordination-run snapshot.</summary>
    public sealed class ClashEvidenceEndpoint : IEquatable<ClashEvidenceEndpoint>
    {
        public const int MaxIdentifierLength = 128;

        public string RunId { get; }

        public int OccurrenceIndex { get; }

        public ClashEvidenceEndpoint(string runId, int occurrenceIndex)
        {
            RunId = RequireIdentifier(runId, nameof(runId));

            if (occurrenceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(occurrenceIndex),
                    occurrenceIndex,
                    "Occurrence index cannot be negative.");
            }

            OccurrenceIndex = occurrenceIndex;
        }

        public bool Equals(ClashEvidenceEndpoint? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(RunId, other.RunId, StringComparison.Ordinal)
                && OccurrenceIndex == other.OccurrenceIndex;
        }

        public override bool Equals(object? obj) => Equals(obj as ClashEvidenceEndpoint);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(RunId);
                hash = (hash * 31) + OccurrenceIndex;
                return hash;
            }
        }

        public static bool operator ==(ClashEvidenceEndpoint? left, ClashEvidenceEndpoint? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(ClashEvidenceEndpoint? left, ClashEvidenceEndpoint? right) => !(left == right);

        /// <summary>Human-readable representation for logs and test output. Not a persistent identity.</summary>
        public override string ToString() => $"{RunId}#occurrence[{OccurrenceIndex}]";

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
    }
}
