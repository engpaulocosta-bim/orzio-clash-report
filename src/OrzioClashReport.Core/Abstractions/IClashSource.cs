using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>Reads clash data from an external source and produces a source-agnostic <see cref="ClashReportDocument"/>.</summary>
    public interface IClashSource
    {
        ClashReportDocument Read();
    }
}
