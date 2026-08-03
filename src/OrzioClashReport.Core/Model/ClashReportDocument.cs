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

        /// <summary>
        /// The length unit the source declared for every distance and coordinate in this document,
        /// verbatim and unconverted. A distance means nothing without it, so a report that shows one
        /// says which unit it is in. Null when the source did not declare it.
        /// </summary>
        public string? Units { get; }

        public ClashReportDocument(
            string? sourceName,
            DateTime? exportedAt,
            IReadOnlyList<ClashBatch>? batches,
            string? units = null)
        {
            SourceName = sourceName;
            ExportedAt = exportedAt;
            Batches = batches == null ? Array.Empty<ClashBatch>() : new List<ClashBatch>(batches).AsReadOnly();
            Units = units;
        }
    }
}
