using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>Collapses duplicate clashes and buckets the remainder into a <see cref="GroupedClashReport"/>.</summary>
    public interface IClashGrouper
    {
        GroupedClashReport Group(ClashReportDocument document);
    }
}
