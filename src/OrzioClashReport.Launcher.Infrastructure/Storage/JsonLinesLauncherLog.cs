using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Infrastructure.Storage
{
    /// <summary>
    /// The launcher's local log, one JSON object per line, one file per day.
    /// What is written is decided field by field by the caller: nothing is interpolated into a
    /// sentence, so an absolute path, an argument vector, or a client's name cannot arrive here by
    /// accident. Retention is fourteen days and at most twenty files, applied on start.
    /// </summary>
    public sealed class JsonLinesLauncherLog : ILauncherLog
    {
        internal const int RetentionDays = 14;
        internal const int MaximumFiles = 20;

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private readonly string _directory;
        private readonly TimeProvider _time;
        private readonly object _gate = new object();

        public JsonLinesLauncherLog(string logsDirectory, TimeProvider? time = null)
        {
            if (string.IsNullOrWhiteSpace(logsDirectory))
            {
                throw new ArgumentException("Logs directory must not be empty or whitespace.", nameof(logsDirectory));
            }

            _directory = logsDirectory.Trim();
            _time = time ?? TimeProvider.System;
        }

        public void Write(LauncherLogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            string line = Serialize(entry);

            lock (_gate)
            {
                try
                {
                    Directory.CreateDirectory(_directory);
                    File.AppendAllText(FilePathFor(entry.TimestampUtc), line + "\n", Utf8NoBom);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // A log that cannot be written must never take the application down with it.
                }
            }
        }

        public void ApplyRetention()
        {
            lock (_gate)
            {
                if (!Directory.Exists(_directory))
                {
                    return;
                }

                List<FileInfo> files;
                try
                {
                    files = new List<FileInfo>(new DirectoryInfo(_directory).GetFiles("*.jsonl"));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return;
                }

                files.Sort((left, right) => string.CompareOrdinal(right.Name, left.Name));

                DateTimeOffset cutoff = _time.GetUtcNow().AddDays(-RetentionDays);

                for (int i = 0; i < files.Count; i++)
                {
                    bool tooOld = files[i].LastWriteTimeUtc < cutoff.UtcDateTime;
                    bool tooMany = i >= MaximumFiles;

                    if (tooOld || tooMany)
                    {
                        TryDelete(files[i]);
                    }
                }
            }
        }

        /// <summary>Reads the retained log lines, newest file last, for the diagnostic preview.</summary>
        public IReadOnlyList<string> ReadAll()
        {
            lock (_gate)
            {
                if (!Directory.Exists(_directory))
                {
                    return Array.Empty<string>();
                }

                var lines = new List<string>();
                try
                {
                    string[] files = Directory.GetFiles(_directory, "*.jsonl");
                    Array.Sort(files, StringComparer.Ordinal);

                    foreach (string file in files)
                    {
                        lines.AddRange(File.ReadAllLines(file));
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return lines;
                }

                return lines;
            }
        }

        private string FilePathFor(DateTimeOffset timestamp) => Path.Combine(
            _directory,
            "launcher-" + timestamp.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".jsonl");

        private static string Serialize(LauncherLogEntry entry)
        {
            var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();
                writer.WriteString(
                    "timestamp", entry.TimestampUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
                writer.WriteString("level", entry.Level.ToString());
                writer.WriteString("event", entry.EventName);

                foreach (KeyValuePair<string, string> field in entry.Fields)
                {
                    writer.WriteString(field.Key, field.Value);
                }

                writer.WriteEndObject();
            }

            return Utf8NoBom.GetString(buffer.ToArray());
        }

        private static void TryDelete(FileInfo file)
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A file that cannot be pruned stays; the next start tries again.
            }
        }
    }
}
