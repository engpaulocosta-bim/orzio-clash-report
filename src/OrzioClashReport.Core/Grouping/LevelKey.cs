using System;

namespace OrzioClashReport.Core.Grouping
{
    /// <summary>Computes a deterministic, order-independent level label from the two levels involved in a clash.</summary>
    public static class LevelKey
    {
        private const string Separator = " × ";

        /// <summary>
        /// Combines two element levels into a single stable label: the shared level when both agree, the one
        /// level present when only one side has it, an order-independent "A x B" combination when they differ,
        /// or null when neither side has a level.
        /// </summary>
        public static string? Combine(string? levelA, string? levelB)
        {
            if (levelA == null)
            {
                return levelB;
            }

            if (levelB == null)
            {
                return levelA;
            }

            if (string.Equals(levelA, levelB, StringComparison.Ordinal))
            {
                return levelA;
            }

            return string.CompareOrdinal(levelA, levelB) <= 0
                ? $"{levelA}{Separator}{levelB}"
                : $"{levelB}{Separator}{levelA}";
        }
    }
}
