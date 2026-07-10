using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable result of grouping a <see cref="ClashReportDocument"/>: raw vs grouped counts plus the buckets themselves.</summary>
    public sealed class GroupedClashReport
    {
        public ClashReportDocument Document { get; }
        public IReadOnlyList<ClashGroup> Groups { get; }
        public int RawCount { get; }
        public int GroupCount => Groups.Count;

        public GroupedClashReport(ClashReportDocument document, IReadOnlyList<ClashGroup>? groups, int rawCount)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Groups = groups == null ? Array.Empty<ClashGroup>() : new List<ClashGroup>(groups).AsReadOnly();
            RawCount = rawCount;
        }
    }
}
