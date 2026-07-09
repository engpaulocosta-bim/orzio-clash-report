using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Grouping
{
    /// <summary>Default best-effort <see cref="IDisciplineResolver"/>: uses the nested-model name from PathHierarchy, falling back to the source-file property, then "Unknown"; naming varies per project (see IDisciplineResolver), so swap this out when it does not fit.</summary>
    public sealed class PathHierarchyDisciplineResolver : IDisciplineResolver
    {
        private const int NestedModelPathIndex = 2;

        public string Resolve(ClashObject element)
        {
            if (element.PathHierarchy.Count > NestedModelPathIndex)
            {
                return element.PathHierarchy[NestedModelPathIndex];
            }

            if (element.Properties.TryGetValue("Item Source File Name", out var sourceFileName)
                && !string.IsNullOrEmpty(sourceFileName))
            {
                return sourceFileName;
            }

            if (element.Properties.TryGetValue("Item Source File", out var sourceFile)
                && !string.IsNullOrEmpty(sourceFile))
            {
                return sourceFile;
            }

            return "Unknown";
        }
    }
}
