using System;

namespace OrzioClashReport.Launcher.Contracts.Diagnostics
{
    /// <summary>
    /// Which well-known root a path lives under. This is the only location information a default log
    /// is allowed to carry, because it describes structure without identifying a client or a person.
    /// </summary>
    public enum PathRootKind
    {
        /// <summary>The root could not be classified.</summary>
        Unknown = 0,

        /// <summary>Under the launcher's own local application data directory.</summary>
        LocalApplicationData = 1,

        /// <summary>Under the current user's profile, but not under local application data.</summary>
        UserProfile = 2,

        /// <summary>Under a program-installation directory.</summary>
        ProgramInstallation = 3,

        /// <summary>Under a temporary directory.</summary>
        Temporary = 4,

        /// <summary>A UNC or otherwise network location.</summary>
        Network = 5,

        /// <summary>A rooted local path outside every category above.</summary>
        OtherLocal = 6,

        /// <summary>The path was relative, so no root could be determined.</summary>
        Relative = 7,
    }

    /// <summary>
    /// A path reduced to what is safe to log by default: the file name, the extension, a SHA-256 of
    /// the full path so two log lines can be correlated, and the kind of root it lives under.
    /// The absolute path itself is deliberately absent.
    /// </summary>
    public sealed class RedactedPath
    {
        public RedactedPath(string fileName, string extension, string pathHash, PathRootKind pathRootKind)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));

            if (string.IsNullOrWhiteSpace(pathHash))
            {
                throw new ArgumentException("Path hash must not be empty or whitespace.", nameof(pathHash));
            }

            PathHash = pathHash.Trim().ToLowerInvariant();
            PathRootKind = pathRootKind;
        }

        /// <summary>The file name only. Never the directory chain.</summary>
        public string FileName { get; }

        /// <summary>The extension including the leading dot, or an empty string.</summary>
        public string Extension { get; }

        /// <summary>Lowercase hex SHA-256 of the full path.</summary>
        public string PathHash { get; }

        public PathRootKind PathRootKind { get; }
    }
}
