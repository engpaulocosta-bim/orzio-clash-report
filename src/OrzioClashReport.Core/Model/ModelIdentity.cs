using System;
using System.Globalization;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable, stable, revision-free identity of a BIM model: company, discipline, and model name.</summary>
    public sealed class ModelIdentity : IEquatable<ModelIdentity>
    {
        public string Company { get; }
        public string Discipline { get; }
        public string ModelName { get; }

        /// <summary>
        /// Deterministic, revision-free key built from length-prefixed, trimmed, lowercase-invariant segments
        /// (e.g. "5:sigma|9:structure|10:main model"). Length prefixes make the key unambiguous even when a
        /// field's own value contains the '|' separator; do not use this format for persistence parsing, only
        /// for equality/lookup.
        /// </summary>
        public string StableKey { get; }

        private readonly string _normalizedCompany;
        private readonly string _normalizedDiscipline;
        private readonly string _normalizedModelName;

        public ModelIdentity(string company, string discipline, string modelName)
        {
            Company = RequireNonBlank(company, nameof(company));
            Discipline = RequireNonBlank(discipline, nameof(discipline));
            ModelName = RequireNonBlank(modelName, nameof(modelName));

            _normalizedCompany = Company.ToLowerInvariant();
            _normalizedDiscipline = Discipline.ToLowerInvariant();
            _normalizedModelName = ModelName.ToLowerInvariant();

            StableKey = $"{Segment(_normalizedCompany)}|{Segment(_normalizedDiscipline)}|{Segment(_normalizedModelName)}";
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

        private static string Segment(string normalizedValue) =>
            $"{normalizedValue.Length.ToString(CultureInfo.InvariantCulture)}:{normalizedValue}";

        public bool Equals(ModelIdentity? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(_normalizedCompany, other._normalizedCompany, StringComparison.Ordinal)
                && string.Equals(_normalizedDiscipline, other._normalizedDiscipline, StringComparison.Ordinal)
                && string.Equals(_normalizedModelName, other._normalizedModelName, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as ModelIdentity);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(_normalizedCompany);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(_normalizedDiscipline);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(_normalizedModelName);
                return hash;
            }
        }

        public static bool operator ==(ModelIdentity? left, ModelIdentity? right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(ModelIdentity? left, ModelIdentity? right) => !(left == right);

        /// <summary>Human-readable representation for logs and test output. Not a stable key; use <see cref="StableKey"/> for identity comparisons or lookups.</summary>
        public override string ToString() => $"{Company} / {Discipline} / {ModelName}";
    }
}
