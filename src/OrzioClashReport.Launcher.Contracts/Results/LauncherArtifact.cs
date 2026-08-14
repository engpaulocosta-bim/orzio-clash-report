using System;

namespace OrzioClashReport.Launcher.Contracts.Results
{
    /// <summary>
    /// A file the engine produced, recorded so the user can open or reveal it. The launcher does not
    /// stage, copy, move, or rewrite artifacts: the engine writes them directly to their destination.
    /// </summary>
    public sealed class LauncherArtifact
    {
        public LauncherArtifactKind Kind { get; }
        public string Path { get; }
        public long SizeInBytes { get; }

        public LauncherArtifact(LauncherArtifactKind kind, string path, long sizeInBytes)
        {
            if (!Enum.IsDefined(typeof(LauncherArtifactKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown artifact kind.");
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Artifact path cannot be empty.", nameof(path));
            }

            if (sizeInBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeInBytes), sizeInBytes, "Artifact size cannot be negative.");
            }

            Kind = kind;
            Path = path;
            SizeInBytes = sizeInBytes;
        }
    }
}
