using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, manual declaration that a clash test was executed for a specific ordered pair of
    /// revision-free <see cref="ModelIdentity"/> values. This is a declared fact, not an inference: a
    /// <see cref="RunManifest"/> can declare a test as executed even when it produced zero
    /// <see cref="ClashOccurrence"/>s, which is what proves that the test ran with no results rather than not
    /// running at all. The declared A/B order is preserved exactly as received; consumers that treat the
    /// model pair as unordered do so themselves, this type does not normalize or swap the sides.
    /// </summary>
    public sealed class ExecutedClashTest
    {
        public string Name { get; }
        public ModelIdentity ModelA { get; }
        public ModelIdentity ModelB { get; }

        public ExecutedClashTest(string name, ModelIdentity modelA, ModelIdentity modelB)
        {
            Name = RequireNonBlank(name, nameof(name));
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

        /// <summary>Human-readable representation for logs and test output, e.g. "HVAC vs Structure / Sigma / Structure / Main x Alfa / HVAC / Main". Not a persistence key.</summary>
        public override string ToString() => $"{Name} / {ModelA} x {ModelB}";
    }
}
