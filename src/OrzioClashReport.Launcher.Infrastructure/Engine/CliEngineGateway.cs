using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Results;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// Reaches the engine as a subprocess. This is deliberate for this phase: the published CLI is
    /// already a stable, tested contract, and calling it keeps the launcher from re-implementing any
    /// part of the engine. An in-process gateway can replace this class later without changing a
    /// single caller.
    /// </summary>
    public sealed class CliEngineGateway : IEngineGateway
    {
        /// <summary>
        /// Generous by design. A large export legitimately takes minutes, and the user's own cancel
        /// button — not a short timeout — is the intended way to stop a run.
        /// </summary>
        public static readonly TimeSpan OperationTimeout = TimeSpan.FromMinutes(30);

        private readonly EngineProbe _probe;
        private readonly IEngineProcessRunner _processRunner;
        private readonly IFileProbe _fileProbe;
        private readonly SemaphoreSlim _describeGate = new SemaphoreSlim(1, 1);

        private EngineInfo? _verifiedEngine;

        public CliEngineGateway(EngineProbe probe, IEngineProcessRunner processRunner, IFileProbe fileProbe)
        {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _fileProbe = fileProbe ?? throw new ArgumentNullException(nameof(fileProbe));
        }

        public async Task<EngineInfo> DescribeAsync(CancellationToken cancellationToken)
        {
            EngineInfo info = await _probe.DescribeAsync(cancellationToken).ConfigureAwait(false);

            if (info.IsReady)
            {
                // Hashing the engine on every run would add seconds to every operation. Verification
                // is cached for the process lifetime; the executable is not expected to change while
                // the application is open, and any replacement is caught on the next start.
                await _describeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    _verifiedEngine = info;
                }
                finally
                {
                    _describeGate.Release();
                }
            }

            return info;
        }

        public async Task<EngineJobResult> ExecuteAsync(
            EngineJobRequest request,
            IProgress<EngineJobProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            EngineInfo engine = _verifiedEngine ?? await DescribeAsync(cancellationToken).ConfigureAwait(false);

            if (!engine.IsReady || engine.Location == null)
            {
                return Failed(request, MapUnavailableEngine(engine), TimeSpan.Zero, null);
            }

            progress?.Report(EngineJobProgress.ForState(EngineJobState.Running));

            EngineProcessResult process = await _processRunner.RunAsync(
                new EngineProcessRequest(
                    engine.Location.ExecutablePath,
                    request.ArgumentList,
                    request.WorkingDirectory,
                    OperationTimeout),
                progress,
                cancellationToken).ConfigureAwait(false);

            return Interpret(request, process);
        }

        private EngineJobResult Interpret(EngineJobRequest request, EngineProcessResult process)
        {
            var warnings = new List<LauncherWarning>();

            if (process.StandardOutputTruncated || process.StandardErrorTruncated)
            {
                warnings.Add(new LauncherWarning(
                    LauncherWarningKind.EngineOutputTruncated,
                    "O motor produziu mais texto do que é mostrado. Foram guardados o início e o fim."));
            }

            if (process.StartFailure != null)
            {
                return Failed(
                    request,
                    new LauncherError(
                        LauncherErrorKind.EngineStartFailure,
                        "O motor não pôde ser iniciado.",
                        "Verifique o estado do motor em Definições e reinstale se necessário."),
                    process.Duration,
                    process,
                    warnings);
            }

            if (process.Canceled)
            {
                return new EngineJobResult(
                    request.JobId,
                    request.Operation,
                    EngineJobState.Canceled,
                    null,
                    process.StandardOutput,
                    process.StandardError,
                    process.StandardOutputTruncated,
                    process.StandardErrorTruncated,
                    process.Duration,
                    null,
                    Array.Empty<LauncherArtifact>(),
                    warnings);
            }

            if (process.TimedOut)
            {
                return Failed(
                    request,
                    new LauncherError(
                        LauncherErrorKind.EngineTimeout,
                        "O motor não terminou dentro do tempo permitido e foi interrompido.",
                        "Tente novamente com um export mais pequeno, ou verifique se o ficheiro de entrada está acessível."),
                    process.Duration,
                    process,
                    warnings);
            }

            if (process.ExitCode != 0)
            {
                // The engine's published contract is 0 for success and 1 for failure. Anything else
                // is reported exactly as observed rather than translated into an invented taxonomy.
                return Failed(
                    request,
                    new LauncherError(
                        LauncherErrorKind.EngineExecutionFailure,
                        "O motor terminou com um erro.",
                        "Leia a saída do motor abaixo: descreve exatamente o que falhou.",
                        process.ExitCode),
                    process.Duration,
                    process,
                    warnings);
            }

            if (request.ExpectedOutputPath != null)
            {
                long size = _fileProbe.GetFileSizeInBytes(request.ExpectedOutputPath);

                if (size <= 0)
                {
                    return Failed(
                        request,
                        new LauncherError(
                            LauncherErrorKind.OutputMissing,
                            "O motor terminou com sucesso mas o ficheiro de destino não existe ou está vazio.",
                            "Confirme que tem permissão de escrita no destino e tente novamente.",
                            process.ExitCode),
                        process.Duration,
                        process,
                        warnings);
                }

                LauncherArtifactKind? kind = LauncherOperationMetadata.OutputArtifactKind(request.Operation);
                var artifacts = kind == null
                    ? Array.Empty<LauncherArtifact>()
                    : new[] { new LauncherArtifact(kind.Value, request.ExpectedOutputPath, size) };

                return Succeeded(request, process, artifacts, warnings);
            }

            return Succeeded(request, process, Array.Empty<LauncherArtifact>(), warnings);
        }

        private static LauncherError MapUnavailableEngine(EngineInfo engine)
        {
            switch (engine.Status)
            {
                case EngineStatusKind.Missing:
                    return new LauncherError(
                        LauncherErrorKind.EngineMissing,
                        "Não existe motor instalado.",
                        "Reinstale a aplicação para repor o motor.");

                case EngineStatusKind.IntegrityFailure:
                    return new LauncherError(
                        LauncherErrorKind.IntegrityFailure,
                        "O motor instalado não passou a verificação de integridade.",
                        "Reinstale a aplicação. Nenhuma operação é executada com um motor por verificar.");

                case EngineStatusKind.VersionMismatch:
                    return new LauncherError(
                        LauncherErrorKind.VersionMismatch,
                        $"O motor instalado reporta {engine.ReportedVersion}, mas esta aplicação foi empacotada com {engine.ExpectedVersion}.",
                        "Reinstale a aplicação para voltar a um par verificado.");

                default:
                    return new LauncherError(
                        LauncherErrorKind.EngineMissing,
                        "O motor não está disponível.",
                        "Verifique o estado do motor em Definições.");
            }
        }

        private static EngineJobResult Succeeded(
            EngineJobRequest request,
            EngineProcessResult process,
            IReadOnlyList<LauncherArtifact> artifacts,
            IReadOnlyList<LauncherWarning> warnings) =>
            new EngineJobResult(
                request.JobId,
                request.Operation,
                EngineJobState.Succeeded,
                process.ExitCode,
                process.StandardOutput,
                process.StandardError,
                process.StandardOutputTruncated,
                process.StandardErrorTruncated,
                process.Duration,
                null,
                artifacts,
                warnings);

        private static EngineJobResult Failed(
            EngineJobRequest request,
            LauncherError error,
            TimeSpan duration,
            EngineProcessResult? process,
            IReadOnlyList<LauncherWarning>? warnings = null) =>
            new EngineJobResult(
                request.JobId,
                request.Operation,
                EngineJobState.Failed,
                process?.ExitCode,
                process?.StandardOutput ?? string.Empty,
                process?.StandardError ?? string.Empty,
                process?.StandardOutputTruncated ?? false,
                process?.StandardErrorTruncated ?? false,
                duration,
                error,
                Array.Empty<LauncherArtifact>(),
                warnings ?? Array.Empty<LauncherWarning>());
    }
}
