using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable grouping result with fully reconciled raw, retained, and collapsed counts plus
    /// auditable evidence for every collapsed duplicate.
    /// </summary>
    public sealed class GroupedClashReport
    {
        public ClashReportDocument Document { get; }
        public IReadOnlyList<ClashGroup> Groups { get; }
        public IReadOnlyList<ClashCollapse> Collapses { get; }
        public int RawCount { get; }
        public int RetainedCount { get; }
        public int CollapsedCount => Collapses.Count;
        public int GroupCount => Groups.Count;

        public GroupedClashReport(ClashReportDocument document, IReadOnlyList<ClashGroup>? groups, int rawCount)
            : this(document, groups, Array.Empty<ClashCollapse>(), rawCount)
        {
        }

        public GroupedClashReport(
            ClashReportDocument document,
            IReadOnlyList<ClashGroup>? groups,
            IReadOnlyList<ClashCollapse>? collapses,
            int rawCount)
        {
            if (rawCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rawCount), rawCount, "Raw count cannot be negative.");
            }

            Document = document ?? throw new ArgumentNullException(nameof(document));
            Groups = groups == null ? Array.Empty<ClashGroup>() : new List<ClashGroup>(groups).AsReadOnly();
            Collapses = collapses == null
                ? Array.Empty<ClashCollapse>()
                : new List<ClashCollapse>(collapses).AsReadOnly();
            RawCount = rawCount;

            for (int index = 0; index < Collapses.Count; index++)
            {
                if (Collapses[index] == null)
                {
                    throw new ArgumentException(
                        $"Collapse entry at index {index} is null.", nameof(collapses));
                }
            }

            int retainedCount = 0;
            foreach (ClashGroup group in Groups)
            {
                retainedCount += group.Members.Count;
            }

            RetainedCount = retainedCount;

            if (RawCount != RetainedCount + CollapsedCount)
            {
                throw new ArgumentException(
                    "Raw count must equal retained count plus collapsed count.", nameof(rawCount));
            }
        }
    }
}
