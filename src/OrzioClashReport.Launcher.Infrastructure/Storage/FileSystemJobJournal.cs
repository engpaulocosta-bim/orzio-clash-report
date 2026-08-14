using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Infrastructure.Storage
{
    /// <summary>
    /// Writes one file per running job under <c>jobs\{jobId}.json</c> and removes it on any terminal
    /// state. An entry that is still there when the launcher starts means the previous session was
    /// interrupted — which the launcher reports, and never acts on by itself.
    /// </summary>
    public sealed class FileSystemJobJournal : IJobJournal
    {
        private const int SchemaVersion = 1;

        private readonly string _jobsDirectory;

        public FileSystemJobJournal(string jobsDirectory)
        {
            if (string.IsNullOrWhiteSpace(jobsDirectory))
            {
                throw new ArgumentException("Jobs directory cannot be empty.", nameof(jobsDirectory));
            }

            _jobsDirectory = jobsDirectory;
        }

        public Task BeginAsync(JobJournalEntry entry, CancellationToken cancellationToken)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("schemaVersion", SchemaVersion);
                    writer.WriteString("jobId", entry.JobId);
                    writer.WriteString("operation", entry.Operation.ToString());
                    writer.WriteString("startedAtUtc", entry.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture));

                    // The file name only. A leftover journal must never disclose a client's folders.
                    if (entry.OutputFileName == null)
                    {
                        writer.WriteNull("outputFileName");
                    }
                    else
                    {
                        writer.WriteString("outputFileName", entry.OutputFileName);
                    }

                    writer.WriteEndObject();
                }

                AtomicFileWriter.Write(PathFor(entry.JobId), Encoding.UTF8.GetString(stream.ToArray()));
            }

            return Task.CompletedTask;
        }

        public Task CompleteAsync(string jobId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
            }

            try
            {
                File.Delete(PathFor(jobId));
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is System.Security.SecurityException)
            {
                // The entry stays behind and the next start reports an interrupted operation. That is
                // a false positive the user can dismiss, which is preferable to hiding a real one.
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<JobJournalEntry>> ReadInterruptedAsync(CancellationToken cancellationToken)
        {
            var entries = new List<JobJournalEntry>();

            if (!Directory.Exists(_jobsDirectory))
            {
                return Task.FromResult<IReadOnlyList<JobJournalEntry>>(entries);
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(_jobsDirectory, "*.json");
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                return Task.FromResult<IReadOnlyList<JobJournalEntry>>(entries);
            }

            Array.Sort(files, StringComparer.Ordinal);

            foreach (string file in files)
            {
                JobJournalEntry? entry = TryRead(file);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return Task.FromResult<IReadOnlyList<JobJournalEntry>>(entries);
        }

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(_jobsDirectory))
            {
                return Task.CompletedTask;
            }

            foreach (string file in Directory.GetFiles(_jobsDirectory, "*.json"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception exception) when (
                    exception is IOException || exception is UnauthorizedAccessException)
                {
                    // Leaving one behind is harmless: it is reported again on the next start.
                }
            }

            return Task.CompletedTask;
        }

        private static JobJournalEntry? TryRead(string file)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(file)))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        return null;
                    }

                    if (!root.TryGetProperty("jobId", out JsonElement jobId) || jobId.ValueKind != JsonValueKind.String
                        || !root.TryGetProperty("operation", out JsonElement operation) || operation.ValueKind != JsonValueKind.String
                        || !root.TryGetProperty("startedAtUtc", out JsonElement startedAt) || startedAt.ValueKind != JsonValueKind.String)
                    {
                        return null;
                    }

                    if (!Enum.TryParse(operation.GetString(), ignoreCase: false, out LauncherOperationKind parsedOperation)
                        || !DateTimeOffset.TryParse(
                            startedAt.GetString(),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind,
                            out DateTimeOffset parsedStartedAt))
                    {
                        return null;
                    }

                    string? outputFileName = null;
                    if (root.TryGetProperty("outputFileName", out JsonElement fileName)
                        && fileName.ValueKind == JsonValueKind.String)
                    {
                        outputFileName = fileName.GetString();
                    }

                    return new JobJournalEntry(
                        jobId.GetString()!, parsedOperation, parsedStartedAt, outputFileName);
                }
            }
            catch (Exception exception) when (
                exception is JsonException
                || exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException)
            {
                // An unreadable entry cannot describe what was interrupted, so it is skipped rather
                // than surfaced as a half-formed recovery prompt.
                return null;
            }
        }

        private string PathFor(string jobId) => Path.Combine(_jobsDirectory, jobId + ".json");
    }
}
