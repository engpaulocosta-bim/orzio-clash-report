using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Platform;

namespace OrzioClashReport.Launcher.Infrastructure.Logging
{
    /// <summary>
    /// Appends redacted JSON Lines to a per-day file under the launcher's local logs folder. Every
    /// value written comes from an already-redacted <see cref="LauncherLogEntry"/>: this class adds
    /// nothing of its own, so no absolute path, argument vector, or file content can enter through it.
    /// </summary>
    public sealed class JsonLinesLauncherLog : ILauncherLog
    {
        private readonly object _gate = new object();
        private readonly string _logsDirectory;
        private readonly IClock _clock;

        public JsonLinesLauncherLog(string logsDirectory, IClock clock)
        {
            if (string.IsNullOrWhiteSpace(logsDirectory))
            {
                throw new ArgumentException("Logs directory cannot be empty.", nameof(logsDirectory));
            }

            _logsDirectory = logsDirectory;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public string CurrentFilePath =>
            Path.Combine(_logsDirectory, "launcher-" + _clock.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".jsonl");

        public void Write(LauncherLogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            string line = Serialize(entry);

            try
            {
                lock (_gate)
                {
                    Directory.CreateDirectory(_logsDirectory);
                    File.AppendAllText(
                        CurrentFilePath,
                        line + "\n",
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is System.Security.SecurityException)
            {
                // Logging is best effort by design: a full or read-only disk must never turn a report
                // the user asked for into a failure. The operation itself still reports its own outcome.
            }
        }

        internal static string Serialize(LauncherLogEntry entry)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
                {
                    writer.WriteStartObject();
                    writer.WriteString("timestamp", entry.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
                    writer.WriteString("level", entry.Level.ToString());
                    writer.WriteString("event", entry.EventCode);
                    writer.WriteString("message", entry.Message);

                    if (entry.Fields.Count > 0)
                    {
                        writer.WriteStartObject("fields");

                        // Ordinal key order keeps log lines stable and comparable across runs.
                        var keys = new List<string>(entry.Fields.Keys);
                        keys.Sort(StringComparer.Ordinal);

                        foreach (string key in keys)
                        {
                            writer.WriteString(key, entry.Fields[key]);
                        }

                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
