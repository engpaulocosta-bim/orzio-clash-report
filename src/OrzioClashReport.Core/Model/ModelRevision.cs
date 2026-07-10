using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, specific revision of a <see cref="ModelIdentity"/>, carrying the minimal file metadata that
    /// materialized this revision. <see cref="Revision"/> is an opaque label: it is never parsed, ordered, or
    /// used to infer anything, and it never participates in <see cref="ModelIdentity"/> equality or StableKey.
    /// </summary>
    public sealed class ModelRevision : IEquatable<ModelRevision>
    {
        public ModelIdentity Identity { get; }
        public string Revision { get; }
        public string SourceFileName { get; }
        public string? SourceFilePath { get; }
        public string? ContentHash { get; }
        public DateTimeOffset? PublishedAt { get; }

        public ModelRevision(
            ModelIdentity identity,
            string revision,
            string sourceFileName,
            string? sourceFilePath,
            string? contentHash,
            DateTimeOffset? publishedAt)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Revision = RequireNonBlank(revision, nameof(revision));
            SourceFileName = RequireNonBlank(sourceFileName, nameof(sourceFileName));
            SourceFilePath = NormalizeOptional(sourceFilePath);
            ContentHash = NormalizeOptional(contentHash);
            PublishedAt = publishedAt;
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

        /// <summary>
        /// Equality compares <see cref="Identity"/> (its own value semantics), <see cref="Revision"/>,
        /// <see cref="SourceFileName"/>, <see cref="SourceFilePath"/>, <see cref="ContentHash"/>, and
        /// <see cref="PublishedAt"/>. Revision, source file name, and source file path are compared ordinally
        /// ignoring case, because they are names and external identifiers rather than case-sensitive data.
        /// ContentHash is compared ordinally and case-sensitively because its encoding is opaque: hexadecimal
        /// hashes are case-insensitive, but Base64 and other encodings are not, and this type does not know
        /// which one it was given.
        /// </summary>
        public bool Equals(ModelRevision? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Identity.Equals(other.Identity)
                && string.Equals(Revision, other.Revision, StringComparison.OrdinalIgnoreCase)
                && string.Equals(SourceFileName, other.SourceFileName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(SourceFilePath, other.SourceFilePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(ContentHash, other.ContentHash, StringComparison.Ordinal)
                && PublishedAt.Equals(other.PublishedAt);
        }

        public override bool Equals(object? obj) => Equals(obj as ModelRevision);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Identity.GetHashCode();
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Revision);
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(SourceFileName);
                hash = hash * 31 + (SourceFilePath != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(SourceFilePath) : 0);
                hash = hash * 31 + (ContentHash != null ? StringComparer.Ordinal.GetHashCode(ContentHash) : 0);
                hash = hash * 31 + PublishedAt.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(ModelRevision? left, ModelRevision? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(ModelRevision? left, ModelRevision? right) => !(left == right);

        /// <summary>Human-readable representation for logs and test output, e.g. "Sigma / Structure / Sigma_Structure @ R04". Not a fingerprint or run key.</summary>
        public override string ToString() => $"{Identity} @ {Revision}";
    }
}
