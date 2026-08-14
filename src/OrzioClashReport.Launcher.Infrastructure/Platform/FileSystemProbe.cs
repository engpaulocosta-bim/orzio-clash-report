using System;
using System.IO;
using OrzioClashReport.Launcher.Contracts.Platform;

namespace OrzioClashReport.Launcher.Infrastructure.Platform
{
    /// <summary>
    /// Answers existence and size questions about the filesystem. It never opens, writes, moves, or
    /// deletes anything: the engine owns every artifact, and the launcher only looks.
    /// </summary>
    public sealed class FileSystemProbe : IFileProbe
    {
        public bool FileExists(string path) => !string.IsNullOrWhiteSpace(path) && File.Exists(path);

        public bool DirectoryExists(string path) => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);

        public long GetFileSizeInBytes(string path)
        {
            if (!FileExists(path))
            {
                return -1;
            }

            try
            {
                return new FileInfo(path).Length;
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is System.Security.SecurityException)
            {
                // The file exists but cannot be measured. Returning -1 makes the caller treat the
                // output as unverified rather than silently accepting it as produced.
                return -1;
            }
        }
    }
}
