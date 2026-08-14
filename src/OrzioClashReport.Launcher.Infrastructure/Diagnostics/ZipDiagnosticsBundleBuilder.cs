using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Platform;

namespace OrzioClashReport.Launcher.Infrastructure.Diagnostics
{
    /// <summary>
    /// Writes a support bundle containing exactly the six declared entries and nothing else. It reads
    /// only the launcher's own already-redacted log; it never opens an export, a manifest, a snapshot,
    /// a governance document, or a report, and it never leaves the machine.
    /// </summary>
    public sealed class ZipDiagnosticsBundleBuilder : IDiagnosticsBundleBuilder
    {
        private readonly string _diagnosticsDirectory;
        private readonly Func<string> _currentLogFilePath;
        private readonly IClock _clock;
        private readonly string _launcherVersion;

        public ZipDiagnosticsBundleBuilder(
            string diagnosticsDirectory,
            Func<string> currentLogFilePath,
            IClock clock,
            string launcherVersion)
        {
            if (string.IsNullOrWhiteSpace(diagnosticsDirectory))
            {
                throw new ArgumentException("Diagnostics directory cannot be empty.", nameof(diagnosticsDirectory));
            }

            _diagnosticsDirectory = diagnosticsDirectory;
            _currentLogFilePath = currentLogFilePath ?? throw new ArgumentNullException(nameof(currentLogFilePath));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _launcherVersion = launcherVersion ?? throw new ArgumentNullException(nameof(launcherVersion));
        }

        public IReadOnlyList<DiagnosticBundleItem> Plan() => DiagnosticBundleItem.All;

        public Task<string> PreviewRedactedLogAsync(int maximumLines, CancellationToken cancellationToken)
        {
            if (maximumLines <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumLines), maximumLines, "Preview needs at least one line.");
            }

            return Task.FromResult(string.Join("\n", ReadLogLines(maximumLines)));
        }

        public Task<string> BuildAsync(EngineInfo engine, CancellationToken cancellationToken)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            Directory.CreateDirectory(_diagnosticsDirectory);

            string bundlePath = Path.Combine(
                _diagnosticsDirectory,
                "orzio-diagnostics-"
                + _clock.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                + ".zip");

            IReadOnlyList<string> logLines = ReadLogLines(int.MaxValue);

            using (var stream = new FileStream(bundlePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                Write(archive, DiagnosticBundleItem.LauncherVersion, LauncherVersionJson());
                Write(archive, DiagnosticBundleItem.EngineInfo, EngineInfoJson(engine));
                Write(archive, DiagnosticBundleItem.OperatingSystem, OperatingSystemJson());
                Write(archive, DiagnosticBundleItem.JobSummary, JobSummaryJson(logLines));
                Write(archive, DiagnosticBundleItem.RedactedLog, string.Join("\n", logLines));
                Write(archive, DiagnosticBundleItem.IntegrityCheck, IntegrityCheckJson(engine));
            }

            return Task.FromResult(bundlePath);
        }

        private static void Write(ZipArchive archive, DiagnosticBundleItem item, string content)
        {
            if (!DiagnosticBundleItem.IsAllowed(item.FileName))
            {
                throw new InvalidOperationException(
                    $"'{item.FileName}' is not on the diagnostic bundle allow-list.");
            }

            ZipArchiveEntry entry = archive.CreateEntry(item.FileName, CompressionLevel.Optimal);

            using (Stream entryStream = entry.Open())
            using (var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(content);
            }
        }

        private IReadOnlyList<string> ReadLogLines(int maximumLines)
        {
            var lines = new List<string>();

            try
            {
                string path = _currentLogFilePath();
                if (!File.Exists(path))
                {
                    return lines;
                }

                foreach (string line in File.ReadLines(path))
                {
                    lines.Add(line);

                    if (lines.Count >= maximumLines)
                    {
                        break;
                    }
                }
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is System.Security.SecurityException)
            {
                // An unreadable log yields an empty section rather than a failed bundle: the rest of
                // the bundle is still what support needs most.
            }

            return lines;
        }

        private string LauncherVersionJson() =>
            Json(writer =>
            {
                writer.WriteString("launcherVersion", _launcherVersion);
                writer.WriteString("generatedAtUtc", _clock.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            });

        private static string EngineInfoJson(EngineInfo engine) =>
            Json(writer =>
            {
                writer.WriteString("status", engine.Status.ToString());
                writer.WriteString("reportedVersion", engine.ReportedVersion ?? string.Empty);
                writer.WriteString("expectedVersion", engine.ExpectedVersion);

                // Deliberately no executable path: the installation location is not needed to diagnose
                // anything, and it identifies the machine's user.
                writer.WriteBoolean("engineFound", engine.Location != null);
            });

        private static string OperatingSystemJson() =>
            Json(writer =>
            {
                writer.WriteString("osDescription", RuntimeInformation.OSDescription);
                writer.WriteString("osArchitecture", RuntimeInformation.OSArchitecture.ToString());
                writer.WriteString("processArchitecture", RuntimeInformation.ProcessArchitecture.ToString());
                writer.WriteString("frameworkDescription", RuntimeInformation.FrameworkDescription);
            });

        private static string IntegrityCheckJson(EngineInfo engine) =>
            Json(writer =>
            {
                writer.WriteString("verdict", engine.Integrity.Verdict.ToString());
                writer.WriteString("expectedSha256", engine.Integrity.ExpectedSha256 ?? string.Empty);
                writer.WriteString("actualSha256", engine.Integrity.ActualSha256 ?? string.Empty);
            });

        /// <summary>
        /// Job entries lifted straight from the already-redacted log. Nothing is re-read from disk and
        /// nothing new is collected: whatever was safe to log is exactly what is safe to send.
        /// </summary>
        private static string JobSummaryJson(IReadOnlyList<string> logLines)
        {
            var jobs = new List<string>();

            foreach (string line in logLines)
            {
                if (line.Contains("\"event\":\"job.", StringComparison.Ordinal))
                {
                    jobs.Add(line);
                }
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("jobEntryCount", jobs.Count);
                    writer.WriteStartArray("entries");

                    foreach (string job in jobs)
                    {
                        try
                        {
                            using (JsonDocument document = JsonDocument.Parse(job))
                            {
                                document.RootElement.WriteTo(writer);
                            }
                        }
                        catch (JsonException)
                        {
                            // A truncated final line is skipped rather than written through unparsed.
                        }
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static string Json(Action<Utf8JsonWriter> write)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();
                    write(writer);
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
