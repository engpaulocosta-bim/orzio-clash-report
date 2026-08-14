using System;
using System.Collections.Generic;
using System.IO;
using OrzioClashReport.Launcher.Contracts.Platform;

namespace OrzioClashReport.Launcher.Infrastructure.Logging
{
    /// <summary>
    /// Keeps the local log directory small: at most fourteen days and at most twenty files, whichever
    /// bites first. Applied at startup, because a log nobody prunes eventually becomes the largest
    /// thing the application owns.
    /// </summary>
    public sealed class LogRetentionPolicy
    {
        public const int MaximumAgeInDays = 14;
        public const int MaximumFileCount = 20;

        private readonly string _logsDirectory;
        private readonly IClock _clock;

        public LogRetentionPolicy(string logsDirectory, IClock clock)
        {
            if (string.IsNullOrWhiteSpace(logsDirectory))
            {
                throw new ArgumentException("Logs directory cannot be empty.", nameof(logsDirectory));
            }

            _logsDirectory = logsDirectory;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Returns the files it removed, newest-first ordering preserved for the survivors.</summary>
        public IReadOnlyList<string> Apply()
        {
            var removed = new List<string>();

            if (!Directory.Exists(_logsDirectory))
            {
                return removed;
            }

            List<FileInfo> files;
            try
            {
                files = new List<FileInfo>();
                foreach (string path in Directory.GetFiles(_logsDirectory, "*.jsonl"))
                {
                    files.Add(new FileInfo(path));
                }
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                return removed;
            }

            files.Sort((left, right) => right.LastWriteTimeUtc.CompareTo(left.LastWriteTimeUtc));

            DateTime cutoff = _clock.UtcNow.UtcDateTime.AddDays(-MaximumAgeInDays);

            for (int i = 0; i < files.Count; i++)
            {
                bool tooOld = files[i].LastWriteTimeUtc < cutoff;
                bool beyondCount = i >= MaximumFileCount;

                if (!tooOld && !beyondCount)
                {
                    continue;
                }

                try
                {
                    files[i].Delete();
                    removed.Add(files[i].FullName);
                }
                catch (Exception exception) when (
                    exception is IOException || exception is UnauthorizedAccessException)
                {
                    // A file that cannot be removed now is retried on the next start. Failing startup
                    // over log housekeeping would be far worse than keeping one extra file.
                }
            }

            return removed;
        }
    }
}
