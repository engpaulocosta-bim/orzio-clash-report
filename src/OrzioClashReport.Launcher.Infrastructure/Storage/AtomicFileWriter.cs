using System;
using System.IO;
using System.Text;

namespace OrzioClashReport.Launcher.Infrastructure.Storage
{
    /// <summary>
    /// Replaces a launcher-owned file only after a complete temporary write succeeds, mirroring the
    /// safe-replace discipline the engine already applies to its own artifacts. A failure mid-write
    /// leaves the previous file byte-identical.
    /// </summary>
    /// <remarks>
    /// This is used only for the launcher's own local state (settings, recent items, job journal).
    /// It is never used for engine artifacts: the engine owns the persistence of everything it writes.
    /// </remarks>
    internal static class AtomicFileWriter
    {
        public static void Write(string filePath, string content)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            string directory = Path.GetDirectoryName(filePath)
                ?? throw new ArgumentException("File path has no directory.", nameof(filePath));

            Directory.CreateDirectory(directory);

            string temporaryPath = Path.Combine(
                directory, "." + Path.GetFileName(filePath) + "-replace-" + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                File.WriteAllText(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                if (File.Exists(filePath))
                {
                    File.Replace(temporaryPath, filePath, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(temporaryPath, filePath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                    {
                        // The temporary file could not be removed. The destination is already correct,
                        // and leaving a stray temporary behind is preferable to failing the operation.
                    }
                }
            }
        }
    }
}
