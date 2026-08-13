using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// Runs one engine process and captures its bounded streams. Implementations must redirect stdout
    /// and stderr, disable shell execution, create no window, pass arguments element by element, and
    /// terminate the whole process tree on cancellation. Tests substitute this port with a fake engine.
    /// </summary>
    public interface IEngineProcessRunner
    {
        Task<EngineProcessResult> RunAsync(
            EngineProcessRequest request,
            IProgress<EngineJobProgress>? progress,
            CancellationToken cancellationToken);
    }
}
