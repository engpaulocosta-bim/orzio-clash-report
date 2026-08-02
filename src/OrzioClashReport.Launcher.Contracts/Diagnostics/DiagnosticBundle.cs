using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Launcher.Contracts.Diagnostics
{
    /// <summary>
    /// The complete, closed set of files a diagnostic bundle may contain. Nothing else is ever
    /// included, and in particular no file belonging to a client ever is.
    /// </summary>
    public enum DiagnosticBundleItem
    {
        LauncherVersion = 0,
        EngineInfo = 1,
        OperatingSystem = 2,
        JobSummary = 3,
        RedactedLog = 4,
        IntegrityCheck = 5,
    }

    /// <summary>Maps every allowed bundle item to its fixed file name inside the bundle.</summary>
    public static class DiagnosticBundleContents
    {
        public static string FileNameFor(DiagnosticBundleItem item)
        {
            switch (item)
            {
                case DiagnosticBundleItem.LauncherVersion:
                    return "launcher-version.json";
                case DiagnosticBundleItem.EngineInfo:
                    return "engine-info.json";
                case DiagnosticBundleItem.OperatingSystem:
                    return "operating-system.json";
                case DiagnosticBundleItem.JobSummary:
                    return "job-summary.json";
                case DiagnosticBundleItem.RedactedLog:
                    return "redacted-log.jsonl";
                case DiagnosticBundleItem.IntegrityCheck:
                    return "integrity-check.json";
                default:
                    throw new ArgumentOutOfRangeException(nameof(item), item, "Unknown diagnostic bundle item.");
            }
        }

        /// <summary>Every allowed item, in bundle order.</summary>
        public static IReadOnlyList<DiagnosticBundleItem> All { get; } =
            new ReadOnlyCollection<DiagnosticBundleItem>(new[]
            {
                DiagnosticBundleItem.LauncherVersion,
                DiagnosticBundleItem.EngineInfo,
                DiagnosticBundleItem.OperatingSystem,
                DiagnosticBundleItem.JobSummary,
                DiagnosticBundleItem.RedactedLog,
                DiagnosticBundleItem.IntegrityCheck,
            });
    }

    /// <summary>
    /// What a bundle will contain, shown to the human before anything is written. The redacted log
    /// preview lets them read exactly what would leave their machine.
    /// </summary>
    public sealed class DiagnosticBundlePreview
    {
        public DiagnosticBundlePreview(
            IReadOnlyList<DiagnosticBundleItem> items, IReadOnlyList<string> redactedLogPreviewLines)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (redactedLogPreviewLines == null)
            {
                throw new ArgumentNullException(nameof(redactedLogPreviewLines));
            }

            Items = new ReadOnlyCollection<DiagnosticBundleItem>(new List<DiagnosticBundleItem>(items));
            RedactedLogPreviewLines =
                new ReadOnlyCollection<string>(new List<string>(redactedLogPreviewLines));
        }

        public IReadOnlyList<DiagnosticBundleItem> Items { get; }

        /// <summary>The exact redacted log lines that would be written into the bundle.</summary>
        public IReadOnlyList<string> RedactedLogPreviewLines { get; }
    }
}
