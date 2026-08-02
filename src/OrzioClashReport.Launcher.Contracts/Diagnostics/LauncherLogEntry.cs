using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Launcher.Contracts.Diagnostics
{
    public enum LauncherLogLevel
    {
        Debug = 0,
        Information = 1,
        Warning = 2,
        Error = 3,
    }

    /// <summary>
    /// One structured log record. Fields are explicit key/value pairs so a reader never has to parse
    /// a sentence, and so nothing gets logged by accident through string interpolation.
    /// Absolute paths, argument vectors, reviewer aliases, and file contents never belong here.
    /// </summary>
    public sealed class LauncherLogEntry
    {
        public LauncherLogEntry(
            DateTimeOffset timestampUtc,
            LauncherLogLevel level,
            string eventName,
            IReadOnlyDictionary<string, string>? fields = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("Event name must not be empty or whitespace.", nameof(eventName));
            }

            TimestampUtc = timestampUtc;
            Level = level;
            EventName = eventName.Trim();
            Fields = Freeze(fields);
        }

        public DateTimeOffset TimestampUtc { get; }

        public LauncherLogLevel Level { get; }

        /// <summary>A stable, machine-readable event name such as <c>job.started</c>.</summary>
        public string EventName { get; }

        public IReadOnlyDictionary<string, string> Fields { get; }

        private static IReadOnlyDictionary<string, string> Freeze(IReadOnlyDictionary<string, string>? fields)
        {
            if (fields == null || fields.Count == 0)
            {
                return EmptyFields;
            }

            var copy = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.Key))
                {
                    throw new ArgumentException("Field keys must not be empty or whitespace.", nameof(fields));
                }

                copy[field.Key] = field.Value ?? string.Empty;
            }

            return new ReadOnlyDictionary<string, string>(copy);
        }

        private static readonly IReadOnlyDictionary<string, string> EmptyFields =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
    }
}
