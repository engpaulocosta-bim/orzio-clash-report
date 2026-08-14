using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Launcher.Contracts.Diagnostics
{
    /// <summary>
    /// One file a diagnostic bundle may contain. The set is closed: a bundle contains these entries
    /// and nothing else, so a user can be told exactly what they are about to hand over.
    /// </summary>
    public sealed class DiagnosticBundleItem
    {
        public string FileName { get; }
        public string Description { get; }

        private DiagnosticBundleItem(string fileName, string description)
        {
            FileName = fileName;
            Description = description;
        }

        public static DiagnosticBundleItem LauncherVersion { get; } = new DiagnosticBundleItem(
            "launcher-version.json",
            "A versão desta aplicação.");

        public static DiagnosticBundleItem EngineInfo { get; } = new DiagnosticBundleItem(
            "engine-info.json",
            "O estado do motor: versão reportada, versão esperada e resultado da verificação.");

        public static DiagnosticBundleItem OperatingSystem { get; } = new DiagnosticBundleItem(
            "operating-system.json",
            "O sistema operativo e a arquitetura. Sem nome de máquina e sem nome de utilizador.");

        public static DiagnosticBundleItem JobSummary { get; } = new DiagnosticBundleItem(
            "job-summary.json",
            "Um resumo das operações recentes: tipo, estado e código de saída.");

        public static DiagnosticBundleItem RedactedLog { get; } = new DiagnosticBundleItem(
            "redacted-log.jsonl",
            "O registo local já redigido. Os caminhos aparecem apenas como nome de ficheiro, extensão e hash.");

        public static DiagnosticBundleItem IntegrityCheck { get; } = new DiagnosticBundleItem(
            "integrity-check.json",
            "O resultado da verificação SHA-256 do motor.");

        /// <summary>
        /// The complete, closed allow-list, in bundle order. Nothing outside this list is ever written
        /// into a bundle: no export, no manifest, no snapshot, no governance document, no report.
        /// </summary>
        public static IReadOnlyList<DiagnosticBundleItem> All { get; } =
            new ReadOnlyCollection<DiagnosticBundleItem>(new[]
            {
                LauncherVersion,
                EngineInfo,
                OperatingSystem,
                JobSummary,
                RedactedLog,
                IntegrityCheck,
            });

        public static bool IsAllowed(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            foreach (DiagnosticBundleItem item in All)
            {
                if (string.Equals(item.FileName, fileName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
