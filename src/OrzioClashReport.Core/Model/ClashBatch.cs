using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable set of clash results produced by a single clash test (maps to one &lt;clashtest&gt;).</summary>
    public sealed class ClashBatch
    {
        public string? Name { get; }
        public double? Tolerance { get; }
        public IReadOnlyList<ClashResult> Clashes { get; }

        public ClashBatch(string? name, double? tolerance, IReadOnlyList<ClashResult>? clashes)
        {
            Name = name;
            Tolerance = tolerance;
            Clashes = clashes ?? Array.Empty<ClashResult>();
        }
    }
}
