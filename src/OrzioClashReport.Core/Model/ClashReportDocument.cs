using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable, source-agnostic root of a clash export: one or more batches read from any clash source.</summary>
    public sealed class ClashReportDocument
    {
        public string? SourceName { get; }
        public DateTime? ExportedAt { get; }
        public IReadOnlyList<ClashBatch> Batches { get; }

        public ClashReportDocument(string? sourceName, DateTime? exportedAt, IReadOnlyList<ClashBatch>? batches)
        {
            SourceName = sourceName;
            ExportedAt = exportedAt;
            Batches = batches ?? Array.Empty<ClashBatch>();
        }
    }
}
