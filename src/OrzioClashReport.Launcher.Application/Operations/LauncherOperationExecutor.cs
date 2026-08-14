using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Application.Operations
{
    /// <summary>
    /// Runs one operation end to end: checks what can be checked before starting, hands the argument
    /// vector to the engine, and records the outcome. It contains no coordination rule of its own —
    /// grouping, matching, lifecycle and governance all stay inside the engine.
    /// </summary>
    public sealed class LauncherOperationExecutor
    {
        private readonly IEngineGateway _gateway;
        private readonly IFileProbe _fileProbe;
        private readonly IRecentItemsStore _recentItemsStore;
        private readonly IJobJournal _journal;
        private readonly ILauncherLog _log;
        private readonly IPathRedactor _redactor;
        private readonly IClock _clock;
        private readonly string _installationDirectory;

        public LauncherOperationExecutor(
            IEngineGateway gateway,
            IFileProbe fileProbe,
            IRecentItemsStore recentItemsStore,
            IJobJournal journal,
            ILauncherLog log,
            IPathRedactor redactor,
            IClock clock,
            string installationDirectory)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _fileProbe = fileProbe ?? throw new ArgumentNullException(nameof(fileProbe));
            _recentItemsStore = recentItemsStore ?? throw new ArgumentNullException(nameof(recentItemsStore));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            if (string.IsNullOrWhiteSpace(installationDirectory))
            {
                throw new ArgumentException("Installation directory cannot be empty.", nameof(installationDirectory));
            }

            _installationDirectory = installationDirectory;
        }

        public async Task<EngineJobResult> ExecuteAsync(
            LauncherOperationRequest request,
            IProgress<EngineJobProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string jobId = Guid.NewGuid().ToString("N");

            LauncherError? refusal = Validate(request);
            if (refusal != null)
            {
                return Refused(jobId, request, refusal);
            }

            var journalEntry = new JobJournalEntry(
                jobId,
                request.Operation,
                _clock.UtcNow,
                request.OutputPath == null ? null : Path.GetFileName(request.OutputPath));

            await _journal.BeginAsync(journalEntry, cancellationToken).ConfigureAwait(false);

            try
            {
                var jobRequest = new EngineJobRequest(
                    jobId,
                    request.Operation,
                    request.ArgumentList,
                    request.WorkingDirectory,
                    request.OutputPath);

                Log(LauncherLogLevel.Information, "job.started", "Operation started.", request, jobId);

                EngineJobResult result =
                    await _gateway.ExecuteAsync(jobRequest, progress, cancellationToken).ConfigureAwait(false);

                if (result.State == EngineJobState.Succeeded)
                {
                    await RecordArtifactsAsync(request, result, cancellationToken).ConfigureAwait(false);
                }

                Log(
                    result.State == EngineJobState.Succeeded ? LauncherLogLevel.Information : LauncherLogLevel.Warning,
                    "job.finished",
                    "Operation finished.",
                    request,
                    jobId,
                    result);

                return result;
            }
            finally
            {
                // Always removed, on every terminal state: a journal entry that survives means the
                // launcher was interrupted, and that is the only thing it is allowed to mean.
                await _journal.CompleteAsync(jobId, CancellationToken.None).ConfigureAwait(false);
            }
        }

        private LauncherError? Validate(LauncherOperationRequest request)
        {
            if (request.ArgumentList.Count == 0)
            {
                return new LauncherError(
                    LauncherErrorKind.InvalidInput,
                    "A operação não tem argumentos.",
                    "Preencha o formulário e tente novamente.");
            }

            if (!_fileProbe.DirectoryExists(request.WorkingDirectory))
            {
                return new LauncherError(
                    LauncherErrorKind.InvalidInput,
                    "A pasta de trabalho da operação não existe.",
                    "Escolha um destino cuja pasta já exista.");
            }

            if (IsInsideInstallation(request.WorkingDirectory))
            {
                return new LauncherError(
                    LauncherErrorKind.InvalidInput,
                    "A operação não pode escrever dentro da pasta de instalação.",
                    "Escolha um destino na sua pasta de trabalho ou de projeto.");
            }

            if (request.OutputPath == null)
            {
                return null;
            }

            if (!Path.IsPathFullyQualified(request.OutputPath))
            {
                return new LauncherError(
                    LauncherErrorKind.InvalidInput,
                    "O destino tem de ser um caminho absoluto.",
                    "Escolha o destino através do seletor de ficheiros.");
            }

            if (!_fileProbe.FileExists(request.OutputPath))
            {
                return null;
            }

            if (!LauncherOperationMetadata.ProducesReplaceableHtmlOutput(request.Operation))
            {
                // Snapshots, run indexes, project catalogs and governance documents are created with
                // create-new semantics inside the engine. Offering to replace one here would quietly
                // destroy evidence, so the launcher only ever asks for a different name.
                return new LauncherError(
                    LauncherErrorKind.OutputCollision,
                    "Já existe um ficheiro neste destino e este tipo de artefacto nunca é substituído.",
                    "Escolha outro nome. Snapshots, índices, catálogos e governança são sempre criados de novo.");
            }

            if (request.CollisionDecision != OutputCollisionDecision.ReplaceExisting)
            {
                return new LauncherError(
                    LauncherErrorKind.OutputCollision,
                    "Já existe um relatório neste destino.",
                    "Escolha outro nome ou confirme explicitamente que quer substituir o relatório existente.");
            }

            return null;
        }

        private bool IsInsideInstallation(string directory)
        {
            string candidate = Normalize(directory);
            string installation = Normalize(_installationDirectory);

            if (installation.Length == 0)
            {
                return false;
            }

            if (!candidate.StartsWith(installation, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return candidate.Length == installation.Length
                || candidate[installation.Length] == Path.DirectorySeparatorChar
                || candidate[installation.Length] == Path.AltDirectorySeparatorChar;
        }

        private static string Normalize(string path)
        {
            try
            {
                return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException)
            {
                return string.Empty;
            }
        }

        private async Task RecordArtifactsAsync(
            LauncherOperationRequest request, EngineJobResult result, CancellationToken cancellationToken)
        {
            foreach (LauncherArtifact artifact in result.Artifacts)
            {
                await _recentItemsStore.AddAsync(
                    new RecentOutputItem(
                        artifact.Path,
                        request.DisplayName,
                        request.Operation,
                        artifact.Kind,
                        _clock.UtcNow),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private void Log(
            LauncherLogLevel level,
            string eventCode,
            string message,
            LauncherOperationRequest request,
            string jobId,
            EngineJobResult? result = null)
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["jobId"] = jobId,
                ["operation"] = request.Operation.ToString(),

                // The count only. The vector itself carries the user's own paths and never goes in a log.
                ["argumentCount"] = request.ArgumentList.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };

            if (result != null)
            {
                fields["state"] = result.State.ToString();
                fields["exitCode"] = result.ExitCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

                if (result.Error != null)
                {
                    fields["errorKind"] = result.Error.Kind.ToString();
                }
            }

            var entry = new LauncherLogEntry(_clock.UtcNow, level, eventCode, message, fields);

            if (request.OutputPath != null)
            {
                entry = entry.WithPath("output", _redactor.Redact(request.OutputPath));
            }

            _log.Write(entry);
        }

        private EngineJobResult Refused(string jobId, LauncherOperationRequest request, LauncherError error)
        {
            Log(LauncherLogLevel.Warning, "job.refused", "Operation refused before start.", request, jobId);

            return new EngineJobResult(
                jobId,
                request.Operation,
                EngineJobState.Failed,
                null,
                string.Empty,
                string.Empty,
                false,
                false,
                TimeSpan.Zero,
                error,
                Array.Empty<LauncherArtifact>(),
                Array.Empty<LauncherWarning>());
        }
    }
}
