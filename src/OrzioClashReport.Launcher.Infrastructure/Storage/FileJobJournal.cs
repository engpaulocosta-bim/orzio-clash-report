using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Infrastructure.Storage
{
    /// <summary>Answers whether a destination already holds a file, and nothing more.</summary>
    public sealed class PhysicalFileSystemProbe : IFileSystemProbe
    {
        public bool FileExists(string path) =>
            !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    /// <summary>
    /// One small file per running job under the launcher's jobs directory. The file appears when a
    /// job starts and is removed when it reaches any terminal state, so a file still present at the
    /// next start means that job was interrupted. It is reported, never resumed automatically.
    /// </summary>
    public sealed class FileJobJournal : IJobJournal
    {
        private sealed class Entry
        {
            public string JobId { get; set; } = string.Empty;

            public LauncherOperationKind OperationKind { get; set; }

            public DateTimeOffset StartedAtUtc { get; set; }
        }

        private readonly string _directory;

        public FileJobJournal(string jobsDirectory)
        {
            if (string.IsNullOrWhiteSpace(jobsDirectory))
            {
                throw new ArgumentException(
                    "Jobs directory must not be empty or whitespace.", nameof(jobsDirectory));
            }

            _directory = jobsDirectory.Trim();
        }

        public void MarkRunning(JobJournalEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var document = new Entry
            {
                JobId = entry.JobId.Value,
                OperationKind = entry.OperationKind,
                StartedAtUtc = entry.StartedAtUtc,
            };

            LocalJson.WriteAtomically(
                PathFor(entry.JobId), JsonSerializer.Serialize(document, LocalJson.Options));
        }

        public void Clear(JobId jobId)
        {
            if (jobId.IsEmpty)
            {
                return;
            }

            try
            {
                File.Delete(PathFor(jobId));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A journal file that cannot be removed becomes a spurious "interrupted" report at
                // the next start, which is safe: nothing is resumed automatically either way.
            }
        }

        public IReadOnlyList<JobJournalEntry> LoadInterrupted()
        {
            if (!Directory.Exists(_directory))
            {
                return Array.Empty<JobJournalEntry>();
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(_directory, "*.json");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return Array.Empty<JobJournalEntry>();
            }

            Array.Sort(files, StringComparer.Ordinal);

            var entries = new List<JobJournalEntry>(files.Length);
            foreach (string file in files)
            {
                JobJournalEntry? entry = TryRead(file);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        /// <summary>Removes every journal record. Used after the interrupted jobs were reported.</summary>
        public void ClearAll()
        {
            foreach (JobJournalEntry entry in LoadInterrupted())
            {
                Clear(entry.JobId);
            }
        }

        private static JobJournalEntry? TryRead(string file)
        {
            try
            {
                Entry? document = JsonSerializer.Deserialize<Entry>(File.ReadAllText(file), LocalJson.Options);
                if (document == null || string.IsNullOrWhiteSpace(document.JobId))
                {
                    return null;
                }

                return new JobJournalEntry(
                    JobId.Parse(document.JobId), document.OperationKind, document.StartedAtUtc);
            }
            catch (Exception ex) when (ex is JsonException
                                        or IOException
                                        or UnauthorizedAccessException
                                        or ArgumentException)
            {
                // A damaged record cannot describe an interrupted job, so it is ignored rather than
                // turned into a claim the launcher cannot support.
                return null;
            }
        }

        private string PathFor(JobId jobId) => Path.Combine(_directory, jobId.Value + ".json");
    }
}
