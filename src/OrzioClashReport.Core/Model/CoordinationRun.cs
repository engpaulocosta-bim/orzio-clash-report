using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable snapshot of a coordination run: the declared <see cref="RunManifest"/> plus the ordered
    /// <see cref="ClashOccurrence"/>s observed in it. Every occurrence's <see cref="ClashOccurrence.ModelA"/>
    /// and <see cref="ClashOccurrence.ModelB"/> must equal, by full value, one of the exact
    /// <see cref="ModelRevision"/>s declared in the manifest -- the same <see cref="ModelIdentity"/> at a
    /// different revision is rejected. Every occurrence must also correspond to a clash test declared in
    /// <see cref="RunManifest.ExecutedClashTests"/>: same name (case-insensitive) and the same revision-free
    /// model pair, direct or A/B-swapped. A declared executed test with no matching occurrence is valid --
    /// that is how this snapshot proves a test ran and returned zero clashes. This snapshot does not compare
    /// itself to any other run.
    /// </summary>
    public sealed class CoordinationRun
    {
        public RunManifest Manifest { get; }
        public IReadOnlyList<ClashOccurrence> Occurrences { get; }

        public string RunId => Manifest.RunId;
        public DateTimeOffset CreatedAt => Manifest.CreatedAt;
        public IReadOnlyList<ModelRevision> Models => Manifest.Models;
        public IReadOnlyList<ExecutedClashTest> ExecutedClashTests => Manifest.ExecutedClashTests;

        public CoordinationRun(RunManifest manifest, IReadOnlyList<ClashOccurrence> occurrences)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));

            if (occurrences == null)
            {
                throw new ArgumentNullException(nameof(occurrences));
            }

            var copy = new List<ClashOccurrence>(occurrences);
            var declaredRevisions = new HashSet<ModelRevision>(Manifest.Models);

            for (int i = 0; i < copy.Count; i++)
            {
                var occurrence = copy[i];
                if (occurrence == null)
                {
                    throw new ArgumentException($"Occurrence at index {i} cannot be null.", nameof(occurrences));
                }

                if (!declaredRevisions.Contains(occurrence.ModelA))
                {
                    throw new ArgumentException(
                        $"Occurrence at index {i} references ModelA '{occurrence.ModelA}', "
                        + $"which is not declared in run manifest '{Manifest.RunId}'.",
                        nameof(occurrences));
                }

                if (!declaredRevisions.Contains(occurrence.ModelB))
                {
                    throw new ArgumentException(
                        $"Occurrence at index {i} references ModelB '{occurrence.ModelB}', "
                        + $"which is not declared in run manifest '{Manifest.RunId}'.",
                        nameof(occurrences));
                }

                if (!IsCoveredByExecutedClashTest(occurrence, Manifest.ExecutedClashTests))
                {
                    throw new ArgumentException(
                        $"Occurrence at index {i} (clash test '{occurrence.ClashTestName}', models "
                        + $"{occurrence.ModelA.Identity} x {occurrence.ModelB.Identity}) is not covered by any "
                        + $"executed clash test declared in run manifest '{Manifest.RunId}'.",
                        nameof(occurrences));
                }
            }

            Occurrences = copy.AsReadOnly();
        }

        private static bool IsCoveredByExecutedClashTest(
            ClashOccurrence occurrence, IReadOnlyList<ExecutedClashTest> executedClashTests)
        {
            for (int i = 0; i < executedClashTests.Count; i++)
            {
                var test = executedClashTests[i];
                if (!string.Equals(test.Name, occurrence.ClashTestName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool direct = test.ModelA.Equals(occurrence.ModelA.Identity) && test.ModelB.Equals(occurrence.ModelB.Identity);
                bool swapped = test.ModelA.Equals(occurrence.ModelB.Identity) && test.ModelB.Equals(occurrence.ModelA.Identity);

                if (direct || swapped)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "run-1 (3 models, 1458 occurrences)". Not a persistence key.</summary>
        public override string ToString() => $"{RunId} ({Models.Count} models, {Occurrences.Count} occurrences)";
    }
}
