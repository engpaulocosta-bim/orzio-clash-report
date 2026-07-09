using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Test double for <see cref="IDisciplineResolver"/> that maps element ids to a discipline label.</summary>
    public sealed class FakeDisciplineResolver : IDisciplineResolver
    {
        private readonly IReadOnlyDictionary<string, string> _disciplineByElementId;

        public FakeDisciplineResolver(IReadOnlyDictionary<string, string> disciplineByElementId)
        {
            _disciplineByElementId = disciplineByElementId;
        }

        public string Resolve(ClashObject element) =>
            _disciplineByElementId.TryGetValue(element.ElementId, out var discipline) ? discipline : "Unknown";
    }
}
