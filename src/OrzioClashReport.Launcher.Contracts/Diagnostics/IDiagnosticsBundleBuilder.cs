using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Contracts.Diagnostics
{
    /// <summary>
    /// Builds a support bundle, and only when a human explicitly asks for one. Nothing is collected in
    /// the background, nothing is uploaded, and the exact contents are shown before anything is
    /// written.
    /// </summary>
    public interface IDiagnosticsBundleBuilder
    {
        /// <summary>What the bundle will contain. Shown to the user before it is built.</summary>
        IReadOnlyList<DiagnosticBundleItem> Plan();

        /// <summary>The redacted log as it would be included, so the user can read it first.</summary>
        Task<string> PreviewRedactedLogAsync(int maximumLines, CancellationToken cancellationToken);

        /// <summary>Writes the bundle and returns its path.</summary>
        Task<string> BuildAsync(EngineInfo engine, CancellationToken cancellationToken);
    }
}
