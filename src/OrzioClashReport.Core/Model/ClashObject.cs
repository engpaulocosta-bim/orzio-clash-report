using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable reference to one of the two elements involved in a clash, carrying source-specific properties verbatim for downstream discipline resolution.</summary>
    public sealed class ClashObject
    {
        public string ElementId { get; }
        public string? ElementName { get; }
        public string? Level { get; }
        public string? SourceModel { get; }
        public IReadOnlyList<string> PathHierarchy { get; }
        public IReadOnlyDictionary<string, string> Properties { get; }

        public ClashObject(
            string elementId,
            string? elementName,
            string? level,
            string? sourceModel,
            IReadOnlyList<string>? pathHierarchy,
            IReadOnlyDictionary<string, string>? properties)
        {
            ElementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
            ElementName = elementName;
            Level = level;
            SourceModel = sourceModel;
            PathHierarchy = pathHierarchy ?? Array.Empty<string>();
            Properties = properties ?? new Dictionary<string, string>();
        }
    }
}
