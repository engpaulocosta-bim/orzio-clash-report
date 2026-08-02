using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Application
{
    /// <summary>
    /// Runs one operation at a time on behalf of a window: resolves an output collision with a human
    /// first, records the job in the journal while it runs, hands the request to the engine gateway,
    /// and records what was produced. It contains no coordination rule of its own.
    /// </summary>
    public sealed class OperationJobService
    {
        private readonly IEngineGateway _gateway;
        private readonly IJobJournal _journal;
        private readonly IRecentItemsStore _recentItems;
        private readonly IFileSystemProbe _fileSystem;
        private readonly TimeProvider _time;
        private int _busy;

        public OperationJobService(
            IEngineGateway gateway,
            IJobJournal journal,
            IRecentItemsStore recentItems,
            IFileSystemProbe fileSystem,
            TimeProvider? time = null)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _recentItems = recentItems ?? throw new ArgumentNullException(nameof(recentItems));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _time = time ?? TimeProvider.System;
        }

        /// <summary>One job is active per window; a second request is refused rather than queued.</summary>
        public bool IsBusy => Volatile.Read(ref _busy) != 0;

        public async Task<JobSnapshot> RunAsync(
            LauncherOperationRequest request,
            IOutputCollisionPrompt collisionPrompt,
            IProgress<EngineOutputChunk>? progress,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (collisionPrompt == null)
            {
                throw new ArgumentNullException(nameof(collisionPrompt));
            }

            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            {
                throw new InvalidOperationException("Another operation is already running in this window.");
            }

            JobId jobId = JobId.New();
            DateTimeOffset startedAt = _time.GetUtcNow();

            try
            {
                LauncherOperationRequest? resolved =
                    await ResolveCollisionAsync(request, collisionPrompt, cancellationToken).ConfigureAwait(false);

                if (resolved == null)
                {
                    return new JobSnapshot(
                        jobId,
                        request.Kind,
                        JobState.Canceled,
                        startedAt,
                        _time.GetUtcNow(),
                        OperationResult.Failure(
                            new LauncherError(
                                LauncherErrorCode.Canceled,
                                "A operação não foi iniciada porque o ficheiro de destino já existe."),
                            exitCode: null,
                            warnings: null,
                            standardOutput: string.Empty,
                            standardError: string.Empty,
                            duration: TimeSpan.Zero));
                }

                _journal.MarkRunning(new JobJournalEntry(jobId, resolved.Kind, startedAt));

                OperationResult result;
                try
                {
                    result = await _gateway
                        .ExecuteAsync(resolved, progress, cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    // A terminal state, whatever it is, always clears the journal entry. Only an
                    // interrupted process can leave one behind, which is exactly what it must mean.
                    _journal.Clear(jobId);
                }

                RecordArtifacts(result);

                JobState state = result.Succeeded
                    ? JobState.Succeeded
                    : result.Error?.Code == LauncherErrorCode.Canceled ? JobState.Canceled : JobState.Failed;

                return new JobSnapshot(jobId, resolved.Kind, state, startedAt, _time.GetUtcNow(), result);
            }
            finally
            {
                Volatile.Write(ref _busy, 0);
            }
        }

        /// <summary>
        /// Returns the request to run, or <c>null</c> when the human chose not to run at all.
        /// Only derived HTML has a collision question: the engine refuses to overwrite the artifacts
        /// that are evidence, so the launcher never offers to replace one.
        /// </summary>
        private async Task<LauncherOperationRequest?> ResolveCollisionAsync(
            LauncherOperationRequest request,
            IOutputCollisionPrompt collisionPrompt,
            CancellationToken cancellationToken)
        {
            string? outputPath = request.OutputPath;
            if (outputPath == null ||
                LauncherOperationPolicy.OutputPersistence(request.Kind) != OutputPersistenceKind.DerivedHtml ||
                !_fileSystem.FileExists(outputPath))
            {
                return request;
            }

            OutputCollisionResolution resolution = await collisionPrompt
                .ResolveAsync(outputPath, cancellationToken)
                .ConfigureAwait(false);

            switch (resolution.Decision)
            {
                case OutputCollisionDecision.Replace:
                    return request;
                case OutputCollisionDecision.UseDifferentPath:
                    return request.WithOutputPath(
                        resolution.ReplacementPath
                        ?? throw new InvalidOperationException("No replacement path was supplied."));
                default:
                    return null;
            }
        }

        private void RecordArtifacts(OperationResult result)
        {
            if (!result.Succeeded)
            {
                return;
            }

            foreach (LauncherArtifact artifact in result.Artifacts)
            {
                _recentItems.Add(new RecentOutputItem(
                    Path.GetFileName(artifact.Path), artifact.Path, _time.GetUtcNow()));
            }
        }
    }
}
