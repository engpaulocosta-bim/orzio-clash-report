using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Infrastructure.Processes;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// Runs engine operations as a child process. This is the only place that knows the engine is a
    /// separate executable; everything above it speaks in typed requests and results.
    /// </summary>
    public sealed class CliEngineGateway : IEngineGateway
    {
        private readonly string _executablePath;
        private readonly IEngineArgumentBuilder _argumentBuilder;
        private readonly ProcessJobRunner _runner;

        public CliEngineGateway(
            string executablePath, IEngineArgumentBuilder argumentBuilder, ProcessJobRunner runner)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path must not be empty or whitespace.", nameof(executablePath));
            }

            _executablePath = executablePath.Trim();
            _argumentBuilder = argumentBuilder ?? throw new ArgumentNullException(nameof(argumentBuilder));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public async Task<OperationResult> ExecuteAsync(
            LauncherOperationRequest request,
            IProgress<EngineOutputChunk>? progress,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IReadOnlyList<string> arguments = _argumentBuilder.Build(request);

            string workingDirectory;
            try
            {
                workingDirectory = WorkingDirectoryFor(request);
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult.Failure(
                    new LauncherError(LauncherErrorCode.InvalidRequest, ex.Message),
                    exitCode: null,
                    warnings: null,
                    standardOutput: string.Empty,
                    standardError: string.Empty,
                    duration: TimeSpan.Zero);
            }

            var specification = new ProcessRunSpecification(_executablePath, arguments, workingDirectory);
            ProcessRunResult run = await _runner
                .RunAsync(specification, progress, cancellationToken)
                .ConfigureAwait(false);

            return Interpret(request, run);
        }

        private static OperationResult Interpret(LauncherOperationRequest request, ProcessRunResult run)
        {
            var warnings = new List<LauncherWarning>();
            if (run.OutputTruncated)
            {
                warnings.Add(new LauncherWarning(
                    LauncherWarningCode.OutputTruncated,
                    "O motor produziu mais texto do que é possível manter; o meio do registo foi omitido."));
            }

            switch (run.Outcome)
            {
                case ProcessRunOutcome.StartFailed:
                    return OperationResult.Failure(
                        new LauncherError(
                            LauncherErrorCode.EngineStartFailure,
                            "Não foi possível iniciar o motor.",
                            run.StartFailureMessage),
                        exitCode: null,
                        warnings,
                        run.StandardOutput,
                        run.StandardError,
                        run.Duration);

                case ProcessRunOutcome.Canceled:
                    return OperationResult.Failure(
                        new LauncherError(LauncherErrorCode.Canceled, "A operação foi cancelada."),
                        exitCode: null,
                        warnings,
                        run.StandardOutput,
                        run.StandardError,
                        run.Duration);

                case ProcessRunOutcome.TimedOut:
                    return OperationResult.Failure(
                        new LauncherError(LauncherErrorCode.TimedOut, "O motor não terminou dentro do tempo previsto."),
                        exitCode: null,
                        warnings,
                        run.StandardOutput,
                        run.StandardError,
                        run.Duration);
            }

            int exitCode = run.ExitCode ?? 1;
            if (exitCode != 0)
            {
                // The engine's own taxonomy is success or failure. Anything non-zero that the
                // launcher cannot explain by itself is reported as an engine execution failure.
                return OperationResult.Failure(
                    new LauncherError(
                        LauncherErrorCode.EngineExecutionFailure,
                        "O motor terminou com um erro.",
                        FirstMeaningfulLine(run.StandardError) ?? FirstMeaningfulLine(run.StandardOutput)),
                    exitCode,
                    warnings,
                    run.StandardOutput,
                    run.StandardError,
                    run.Duration);
            }

            string? outputPath = request.OutputPath;
            var artifacts = new List<LauncherArtifact>();

            if (outputPath != null)
            {
                var file = new FileInfo(outputPath);
                if (!file.Exists || file.Length == 0)
                {
                    return OperationResult.Failure(
                        new LauncherError(
                            LauncherErrorCode.OutputMissing,
                            "O motor terminou com sucesso mas o ficheiro de saída não foi produzido."),
                        exitCode,
                        warnings,
                        run.StandardOutput,
                        run.StandardError,
                        run.Duration);
                }

                artifacts.Add(new LauncherArtifact(ArtifactKindFor(request.Kind), file.FullName, file.Length));
            }

            if (run.StandardError.Length > 0)
            {
                warnings.Add(new LauncherWarning(
                    LauncherWarningCode.EngineWroteToStandardError,
                    "O motor terminou com sucesso mas escreveu mensagens de diagnóstico."));
            }

            return OperationResult.Success(
                exitCode, artifacts, warnings, run.StandardOutput, run.StandardError, run.Duration);
        }

        /// <summary>
        /// The engine runs from the directory it writes into, never from the launcher's installation
        /// directory. Every operation resolves to a directory the human already chose.
        /// </summary>
        internal static string WorkingDirectoryFor(LauncherOperationRequest request)
        {
            string? anchor = request.OutputPath ?? EngineOwnedAnchor(request);
            if (anchor == null)
            {
                throw new InvalidOperationException("This operation has no directory to run from.");
            }

            string? directory = Path.GetDirectoryName(Path.GetFullPath(anchor));
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("The chosen destination has no containing directory.");
            }

            if (!Directory.Exists(directory))
            {
                throw new InvalidOperationException("A pasta de destino escolhida não existe.");
            }

            return directory;
        }

        /// <summary>The file an operation without <c>-o</c> works against.</summary>
        private static string? EngineOwnedAnchor(LauncherOperationRequest request)
        {
            switch (request)
            {
                case AppendProjectSnapshotRequest append:
                    return append.ProjectPath;
                case RenderProjectRequest render:
                    return render.ProjectPath;
                case ValidateIdentityGovernanceRequest validate:
                    return validate.ProjectPath;
                default:
                    return null;
            }
        }

        internal static ArtifactKind ArtifactKindFor(LauncherOperationKind kind)
        {
            switch (kind)
            {
                case LauncherOperationKind.QuickReport:
                case LauncherOperationKind.CompareRuns:
                case LauncherOperationKind.CompareSnapshots:
                case LauncherOperationKind.CompareIndex:
                case LauncherOperationKind.RenderIdentityGovernanceReport:
                    return ArtifactKind.HtmlReport;
                case LauncherOperationKind.CreateSnapshot:
                    return ArtifactKind.SnapshotJson;
                case LauncherOperationKind.IndexSnapshots:
                    return ArtifactKind.RunIndexJson;
                case LauncherOperationKind.CreateProject:
                    return ArtifactKind.ProjectCatalogJson;
                case LauncherOperationKind.CreateIdentityGovernance:
                    return ArtifactKind.IdentityGovernanceJson;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "This operation produces no artifact.");
            }
        }

        private static string? FirstMeaningfulLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // The engine's own message is shown as-is. It is never parsed to decide anything.
            foreach (string line in text.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0)
                {
                    return trimmed;
                }
            }

            return null;
        }
    }
}
