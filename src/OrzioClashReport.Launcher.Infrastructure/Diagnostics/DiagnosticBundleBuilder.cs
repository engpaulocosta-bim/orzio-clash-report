using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Infrastructure.Diagnostics
{
    /// <summary>What the bundle builder is allowed to ask about, supplied by the composition root.</summary>
    public sealed class DiagnosticContext
    {
        public DiagnosticContext(
            string launcherVersion,
            Func<EngineProbeResult?> engineState,
            Func<EngineIntegrityResult> integrity,
            Func<IReadOnlyList<JobSnapshot>> jobHistory)
        {
            LauncherVersion = launcherVersion ?? throw new ArgumentNullException(nameof(launcherVersion));
            EngineState = engineState ?? throw new ArgumentNullException(nameof(engineState));
            Integrity = integrity ?? throw new ArgumentNullException(nameof(integrity));
            JobHistory = jobHistory ?? throw new ArgumentNullException(nameof(jobHistory));
        }

        public string LauncherVersion { get; }

        public Func<EngineProbeResult?> EngineState { get; }

        public Func<EngineIntegrityResult> Integrity { get; }

        public Func<IReadOnlyList<JobSnapshot>> JobHistory { get; }
    }

    /// <summary>
    /// Builds the diagnostic bundle, and only ever on an explicit human action. The bundle contains
    /// exactly six named files and nothing else: no client file, no absolute path, no argument
    /// vector, and no engine output. The human sees the full list and the exact redacted log before
    /// anything is written.
    /// </summary>
    public sealed class DiagnosticBundleBuilder : IDiagnosticBundleBuilder
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        private readonly DiagnosticContext _context;
        private readonly JsonLinesLauncherLogReader _log;

        public DiagnosticBundleBuilder(DiagnosticContext context, JsonLinesLauncherLogReader log)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public DiagnosticBundlePreview Preview() =>
            new DiagnosticBundlePreview(DiagnosticBundleContents.All, _log.ReadAll());

        public Task<string> BuildAsync(string destinationPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException(
                    "Destination path must not be empty or whitespace.", nameof(destinationPath));
            }

            string full = Path.GetFullPath(destinationPath);
            string? directory = Path.GetDirectoryName(full);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (FileStream stream = File.Create(full))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (DiagnosticBundleItem item in DiagnosticBundleContents.All)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Write(archive, DiagnosticBundleContents.FileNameFor(item), ContentFor(item));
                }
            }

            return Task.FromResult(full);
        }

        private string ContentFor(DiagnosticBundleItem item)
        {
            switch (item)
            {
                case DiagnosticBundleItem.LauncherVersion:
                    return Json(new { launcherVersion = _context.LauncherVersion });

                case DiagnosticBundleItem.EngineInfo:
                    return EngineInfo();

                case DiagnosticBundleItem.OperatingSystem:
                    return Json(new
                    {
                        description = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                        architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                        processArchitecture =
                            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                        framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    });

                case DiagnosticBundleItem.JobSummary:
                    return JobSummary();

                case DiagnosticBundleItem.RedactedLog:
                    return string.Join("\n", _log.ReadAll());

                case DiagnosticBundleItem.IntegrityCheck:
                    return IntegrityCheck();

                default:
                    throw new ArgumentOutOfRangeException(nameof(item), item, "Unknown diagnostic bundle item.");
            }
        }

        private string EngineInfo()
        {
            EngineProbeResult? state = _context.EngineState();

            // Deliberately no path: which engine is installed is a version and a status, not a
            // location on this machine.
            return Json(new
            {
                status = state?.Status.ToString() ?? EngineStatus.Checking.ToString(),
                version = state?.Version,
                expectedVersion = state?.ExpectedVersion,
            });
        }

        private string IntegrityCheck()
        {
            EngineIntegrityResult integrity = _context.Integrity();

            return Json(new
            {
                status = integrity.Status.ToString(),
                expectedSha256 = integrity.ExpectedSha256,
                actualSha256 = integrity.ActualSha256,
                matches = integrity.IsVerified,
            });
        }

        private string JobSummary()
        {
            IReadOnlyList<JobSnapshot> history = _context.JobHistory();
            var jobs = new List<object>(history.Count);

            foreach (JobSnapshot job in history)
            {
                // Operation, outcome, and timing only. No path, no file name, no engine output.
                jobs.Add(new
                {
                    operation = job.OperationKind.ToString(),
                    state = job.State.ToString(),
                    exitCode = job.Result?.ExitCode,
                    errorCode = job.Result?.Error?.Code.ToString(),
                    durationSeconds = job.Result?.Duration.TotalSeconds.ToString(
                        "F1", CultureInfo.InvariantCulture),
                    artifactCount = job.Result?.Artifacts.Count ?? 0,
                });
            }

            return Json(new { jobs });
        }

        private static string Json(object value) => JsonSerializer.Serialize(value, WriteOptions);

        private static void Write(ZipArchive archive, string entryName, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using Stream stream = entry.Open();
            byte[] bytes = Utf8NoBom.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    /// <summary>Reads the retained redacted log lines. Kept separate so the builder needs no writer.</summary>
    public sealed class JsonLinesLauncherLogReader
    {
        private readonly Func<IReadOnlyList<string>> _read;

        public JsonLinesLauncherLogReader(Func<IReadOnlyList<string>> read)
        {
            _read = read ?? throw new ArgumentNullException(nameof(read));
        }

        public IReadOnlyList<string> ReadAll() => _read();
    }
}
