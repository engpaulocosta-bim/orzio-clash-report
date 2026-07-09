using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>Renders a grouped clash report into a self-contained output document.</summary>
    public interface IReportRenderer
    {
        string Render(GroupedClashReport report);
    }
}
