using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// A single clash observed in one clash test during a specific coordination run, linked to the exact
    /// <see cref="ModelRevision"/> that produced each side. This is run-specific evidence, not a stable
    /// identity: the same underlying clash can appear as a different <see cref="ClashOccurrence"/> in a later
    /// run, and this type makes no attempt to recognize that. <see cref="ModelA"/> corresponds to
    /// <see cref="ClashResult.ElementA"/> of <see cref="Clash"/>, and <see cref="ModelB"/> corresponds to
    /// <see cref="ClashResult.ElementB"/>; the declared A/B order is preserved exactly as received, never
    /// normalized or swapped (that belongs to a future matcher, not to this raw snapshot).
    /// </summary>
    public sealed class ClashOccurrence
    {
        public string ClashTestName { get; }
        public ClashResult Clash { get; }
        public ModelRevision ModelA { get; }
        public ModelRevision ModelB { get; }

        public ClashOccurrence(string clashTestName, ClashResult clash, ModelRevision modelA, ModelRevision modelB)
        {
            ClashTestName = RequireNonBlank(clashTestName, nameof(clashTestName));
            Clash = clash ?? throw new ArgumentNullException(nameof(clash));
            ModelA = modelA ?? throw new ArgumentNullException(nameof(modelA));
            ModelB = modelB ?? throw new ArgumentNullException(nameof(modelB));
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

        /// <summary>Human-readable representation for logs and test output, e.g. "Test 1 / Clash12 / Sigma / Structure / Main @ R04 x Alfa / HVAC / Main @ R07". Not a fingerprint or key.</summary>
        public override string ToString()
        {
            string clashLabel = Clash.Name ?? Clash.Guid ?? "(unnamed clash)";
            return $"{ClashTestName} / {clashLabel} / {ModelA} x {ModelB}";
        }
    }
}
