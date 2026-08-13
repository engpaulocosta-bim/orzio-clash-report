using System;

namespace OrzioClashReport.Launcher.Contracts.Logging
{
    /// <summary>
    /// A path reduced to what can safely be written down: the file name, its extension, a SHA-256 of the
    /// full path so two log lines about the same file can be correlated, and the class of location it
    /// came from. Directory structure, drive letters, share names, and client names are all discarded.
    /// </summary>
    public sealed class RedactedPath
    {
        public string FileName { get; }
        public string Extension { get; }
        public string PathHash { get; }
        public PathRootKind RootKind { get; }

        public RedactedPath(string fileName, string extension, string pathHash, PathRootKind rootKind)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (string.IsNullOrWhiteSpace(pathHash))
            {
                throw new ArgumentException("Path hash cannot be empty.", nameof(pathHash));
            }

            if (!Enum.IsDefined(typeof(PathRootKind), rootKind))
            {
                throw new ArgumentOutOfRangeException(nameof(rootKind), rootKind, "Unknown path root kind.");
            }

            FileName = fileName;
            Extension = extension;
            PathHash = pathHash;
            RootKind = rootKind;
        }
    }
}
