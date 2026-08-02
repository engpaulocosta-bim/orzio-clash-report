using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Contracts.Ports
{
    /// <summary>
    /// The single boundary between the launcher and the engine. Everything above this port speaks in
    /// typed requests and results; how the engine is reached is entirely behind it.
    /// </summary>
    public interface IEngineGateway
    {
        /// <summary>
        /// Runs one operation to completion. Cancellation must terminate the engine and everything it
        /// started, then wait for it to actually exit before the returned task completes.
        /// </summary>
        Task<OperationResult> ExecuteAsync(
            LauncherOperationRequest request,
            IProgress<EngineOutputChunk>? progress,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Establishes which engine is installed and whether it can be trusted. This is separate from
    /// running operations so the shell can report engine state before any work is requested.
    /// </summary>
    public interface IEngineProbe
    {
        Task<EngineProbeResult> ProbeAsync(CancellationToken cancellationToken);
    }

    /// <summary>Verifies the installed engine against the packaged engine manifest.</summary>
    public interface IEngineIntegrityVerifier
    {
        EngineIntegrityResult Verify();
    }

    /// <summary>Turns a typed request into the exact argument vector the engine expects.</summary>
    public interface IEngineArgumentBuilder
    {
        /// <summary>
        /// Builds the argument vector for one request. The result is passed element by element to the
        /// process; it is never joined into a command line and never handed to a shell.
        /// </summary>
        IReadOnlyList<string> Build(LauncherOperationRequest request);
    }
}
