using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Grouping
{
    /// <summary>Collapses duplicate clash detections, then buckets the remainder by clash test, discipline pair, and level.</summary>
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
            var buckets = new Dictionary<string, Bucket>();
            var bucketOrder = new List<string>();

            foreach (var batch in document.Batches)
            {
                rawCount += batch.Clashes.Count;
                var deduped = CollapseDuplicates(batch.Clashes, batch.Tolerance ?? DefaultTolerance);
                BucketClashes(batch.Name, deduped, buckets, bucketOrder);
            }

            var groups = FinalizeGroups(buckets, bucketOrder);

            return new GroupedClashReport(document, groups, rawCount);
        }

        private readonly struct Bucket
        {
            public Bucket(string? clashTestName, string disciplineA, string disciplineB, string? level, List<ClashResult> members)
            {
                ClashTestName = clashTestName;
                DisciplineA = disciplineA;
                DisciplineB = disciplineB;
                Level = level;
                Members = members;
            }

            public string? ClashTestName { get; }
            public string DisciplineA { get; }
            public string DisciplineB { get; }
            public string? Level { get; }
            public List<ClashResult> Members { get; }
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

        /// <summary>Buckets deduped clashes from one clash test by an order-independent discipline pair plus level, never mixing with other clash tests.</summary>
        private void BucketClashes(
            string? clashTestName, IEnumerable<ClashResult> deduped, Dictionary<string, Bucket> buckets, List<string> bucketOrder)
        {
            foreach (var clash in deduped)
            {
                string disciplineA = _disciplineResolver.Resolve(clash.ElementA);
                string disciplineB = _disciplineResolver.Resolve(clash.ElementB);
                string? level = LevelKey.Combine(clash.ElementA.Level, clash.ElementB.Level);

                bool aFirst = string.CompareOrdinal(disciplineA, disciplineB) <= 0;
                string first = aFirst ? disciplineA : disciplineB;
                string second = aFirst ? disciplineB : disciplineA;
                string key = $"{clashTestName ?? "(unnamed test)"}|{first}|{second}|{level ?? "(none)"}";

                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new Bucket(clashTestName, first, second, level, new List<ClashResult>());
                    buckets[key] = bucket;
                    bucketOrder.Add(key);
                }

                bucket.Members.Add(clash);
            }
        }

        /// <summary>Materializes buckets into <see cref="ClashGroup"/>s, sorted by a stable clash-test/discipline/level key.</summary>
        private static List<ClashGroup> FinalizeGroups(Dictionary<string, Bucket> buckets, List<string> bucketOrder)
        {
            return bucketOrder
                .Select(key => buckets[key])
                .OrderBy(b => b.ClashTestName, StringComparer.Ordinal)
                .ThenBy(b => b.DisciplineA, StringComparer.Ordinal)
                .ThenBy(b => b.DisciplineB, StringComparer.Ordinal)
                .ThenBy(b => b.Level, StringComparer.Ordinal)
                .Select(b => new ClashGroup(b.ClashTestName, b.DisciplineA, b.DisciplineB, b.Level, b.Members))
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
