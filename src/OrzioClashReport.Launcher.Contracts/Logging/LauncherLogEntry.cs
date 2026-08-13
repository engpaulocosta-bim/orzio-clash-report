using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Launcher.Contracts.Logging
{
    /// <summary>
    /// One structured log record, written as a single JSON line. <see cref="EventCode"/> is a stable
    /// machine-readable identifier so support does not have to parse prose.
    /// </summary>
    /// <remarks>
    /// What must never appear in <see cref="Fields"/> or <see cref="Message"/>: an absolute path, a full
    /// argument vector, a client name, XML/manifest/snapshot/governance content, a reviewer alias, or a
    /// stack trace carrying private data. Paths belong here only as a <see cref="RedactedPath"/> added
    /// through <see cref="WithPath"/>.
    /// </remarks>
    public sealed class LauncherLogEntry
    {
        public DateTimeOffset TimestampUtc { get; }
        public LauncherLogLevel Level { get; }
        public string EventCode { get; }
        public string Message { get; }
        public IReadOnlyDictionary<string, string> Fields { get; }

        public LauncherLogEntry(
            DateTimeOffset timestampUtc,
            LauncherLogLevel level,
            string eventCode,
            string message,
            IReadOnlyDictionary<string, string>? fields = null)
        {
            if (!Enum.IsDefined(typeof(LauncherLogLevel), level))
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown log level.");
            }

            if (string.IsNullOrWhiteSpace(eventCode))
            {
                throw new ArgumentException("Event code cannot be empty.", nameof(eventCode));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            if (fields != null)
            {
                foreach (KeyValuePair<string, string> field in fields)
                {
                    if (string.IsNullOrEmpty(field.Key))
                    {
                        throw new ArgumentException("Log field keys cannot be empty.", nameof(fields));
                    }

                    copy[field.Key] = field.Value ?? string.Empty;
                }
            }

            TimestampUtc = timestampUtc;
            Level = level;
            EventCode = eventCode;
            Message = message;
            Fields = new ReadOnlyDictionary<string, string>(copy);
        }

        /// <summary>Returns a copy carrying the redacted representation of one path under the given prefix.</summary>
        public LauncherLogEntry WithPath(string keyPrefix, RedactedPath path)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                throw new ArgumentException("Key prefix cannot be empty.", nameof(keyPrefix));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> field in Fields)
            {
                fields[field.Key] = field.Value;
            }

            fields[keyPrefix + ".fileName"] = path.FileName;
            fields[keyPrefix + ".extension"] = path.Extension;
            fields[keyPrefix + ".pathHash"] = path.PathHash;
            fields[keyPrefix + ".pathRootKind"] = path.RootKind.ToString();

            return new LauncherLogEntry(TimestampUtc, Level, EventCode, Message, fields);
        }
    }
}
