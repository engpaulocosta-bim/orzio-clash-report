using System;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;

namespace OrzioClashReport.Launcher.Contracts.Settings
{
    /// <summary>
    /// One previously produced output, kept so the home screen can offer to open it again. It stores the
    /// path the user themselves chose; it is local convenience state, never evidence and never uploaded.
    /// </summary>
    public sealed class RecentOutputItem
    {
        public string Path { get; }
        public string DisplayName { get; }
        public LauncherOperationKind Operation { get; }
        public LauncherArtifactKind ArtifactKind { get; }
        public DateTimeOffset CompletedAtUtc { get; }

        public RecentOutputItem(
            string path,
            string displayName,
            LauncherOperationKind operation,
            LauncherArtifactKind artifactKind,
            DateTimeOffset completedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Recent item path cannot be empty.", nameof(path));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Recent item display name cannot be empty.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(LauncherOperationKind), operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown launcher operation.");
            }

            if (!Enum.IsDefined(typeof(LauncherArtifactKind), artifactKind))
            {
                throw new ArgumentOutOfRangeException(nameof(artifactKind), artifactKind, "Unknown artifact kind.");
            }

            Path = path;
            DisplayName = displayName;
            Operation = operation;
            ArtifactKind = artifactKind;
            CompletedAtUtc = completedAtUtc;
        }
    }
}
