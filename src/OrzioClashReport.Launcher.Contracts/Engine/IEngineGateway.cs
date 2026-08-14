using System;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Jobs;

namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// The single boundary through which the launcher reaches the engine. Implementations own how the
    /// engine is invoked; callers supply an already-built argument vector and never a command string.
    /// </summary>
    public interface IEngineGateway
    {
        /// <summary>Probes the engine and reports what was actually observed. Never throws for a missing or broken engine.</summary>
        Task<EngineInfo> DescribeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Runs one engine job to completion. Cancellation terminates the engine process tree and
        /// yields a canceled result rather than an exception.
        /// </summary>
        Task<EngineJobResult> ExecuteAsync(
            EngineJobRequest request,
            IProgress<EngineJobProgress>? progress,
            CancellationToken cancellationToken);
    }
}
