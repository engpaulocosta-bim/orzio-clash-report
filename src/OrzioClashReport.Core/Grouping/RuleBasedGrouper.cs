using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Grouping
{
    /// <summary>Collapses duplicate clash detections, then buckets the remainder by discipline pair and level.</summary>
    public sealed class RuleBasedGrouper : IClashGrouper
    {
        private const double DefaultTolerance = 1e-6;

        private readonly IDisciplineResolver _disciplineResolver;

        public RuleBasedGrouper(IDisciplineResolver disciplineResolver)
        {
            _disciplineResolver = disciplineResolver ?? throw new ArgumentNullException(nameof(disciplineResolver));
        }

        public GroupedClashReport Group(ClashReportDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            int rawCount = 0;
            var deduped = new List<ClashResult>();

            foreach (var batch in document.Batches)
            {
                rawCount += batch.Clashes.Count;
                deduped.AddRange(CollapseDuplicates(batch.Clashes, batch.Tolerance ?? DefaultTolerance));
            }

            var groups = BucketByDisciplinePairAndLevel(deduped);

            return new GroupedClashReport(document, groups, rawCount);
        }

        /// <summary>Collapses clashes that share an element-id pair and land within tolerance of a previously kept point.</summary>
        private static IEnumerable<ClashResult> CollapseDuplicates(IReadOnlyList<ClashResult> clashes, double tolerance)
        {
            var pairGroups = clashes.GroupBy(c => MakeUnorderedPairKey(c.ElementA.ElementId, c.ElementB.ElementId));

            foreach (var pairGroup in pairGroups)
            {
                var kept = new List<ClashResult>();

                foreach (var clash in pairGroup)
                {
                    bool isDuplicate = clash.Point.HasValue
                        && kept.Any(k => k.Point.HasValue && WithinTolerance(k.Point.Value, clash.Point.Value, tolerance));

                    if (!isDuplicate)
                    {
                        kept.Add(clash);
                    }
                }

                foreach (var clash in kept)
                {
                    yield return clash;
                }
            }
        }

        /// <summary>Groups deduped clashes by an order-independent discipline pair plus level, sorted by a stable key.</summary>
        private List<ClashGroup> BucketByDisciplinePairAndLevel(IReadOnlyList<ClashResult> deduped)
        {
            var buckets = new Dictionary<string, (string DisciplineA, string DisciplineB, string? Level, List<ClashResult> Members)>();
            var bucketOrder = new List<string>();

            foreach (var clash in deduped)
            {
                string disciplineA = _disciplineResolver.Resolve(clash.ElementA);
                string disciplineB = _disciplineResolver.Resolve(clash.ElementB);
                string? level = clash.ElementA.Level ?? clash.ElementB.Level;

                bool aFirst = string.CompareOrdinal(disciplineA, disciplineB) <= 0;
                string first = aFirst ? disciplineA : disciplineB;
                string second = aFirst ? disciplineB : disciplineA;
                string key = $"{first}|{second}|{level ?? "(none)"}";

                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = (first, second, level, new List<ClashResult>());
                    buckets[key] = bucket;
                    bucketOrder.Add(key);
                }

                bucket.Members.Add(clash);
            }

            return bucketOrder
                .Select(key => buckets[key])
                .OrderBy(b => b.DisciplineA, StringComparer.Ordinal)
                .ThenBy(b => b.DisciplineB, StringComparer.Ordinal)
                .ThenBy(b => b.Level, StringComparer.Ordinal)
                .Select(b => new ClashGroup(b.DisciplineA, b.DisciplineB, b.Level, b.Members))
                .ToList();
        }

        private static string MakeUnorderedPairKey(string idA, string idB) =>
            string.CompareOrdinal(idA, idB) <= 0 ? $"{idA}|{idB}" : $"{idB}|{idA}";

        private static bool WithinTolerance(ClashPoint a, ClashPoint b, double tolerance)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz) <= tolerance;
        }
    }
}
