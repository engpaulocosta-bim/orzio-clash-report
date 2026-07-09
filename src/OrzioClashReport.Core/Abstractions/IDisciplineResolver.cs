using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>Resolves the discipline label of a clash element; naming conventions vary per project, so this is pluggable.</summary>
    public interface IDisciplineResolver
    {
        string Resolve(ClashObject element);
    }
}
