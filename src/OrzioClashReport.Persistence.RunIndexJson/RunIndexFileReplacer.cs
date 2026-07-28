using System;
using System.IO;
using System.Text;

namespace OrzioClashReport.Persistence.RunIndexJson
{
    /// <summary>Replaces an existing run-index file with newly serialized deterministic content after a complete temporary write succeeds.</summary>
    public sealed class RunIndexFileReplacer
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public void ReplaceExisting(RunIndexDocument index, string filePath)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new RunIndexFormatException("Run index file path cannot be empty or whitespace.");
            }

            string resolvedFilePath = GetFullPath(filePath);
            string? parentDirectory = Path.GetDirectoryName(resolvedFilePath);
            if (string.IsNullOrEmpty(parentDirectory) || !Directory.Exists(parentDirectory))
            {
                throw new RunIndexFormatException($"Run index parent directory not found: '{parentDirectory ?? resolvedFilePath}'.");
            }

            if (Directory.Exists(resolvedFilePath))
            {
                throw new RunIndexFormatException($"Run index destination cannot be an existing directory: '{resolvedFilePath}'.");
            }

            if (!File.Exists(resolvedFilePath))
            {
                throw new RunIndexFormatException($"Run index file not found: '{resolvedFilePath}'.");
            }

            string json = new JsonRunIndexSerializer().Serialize(index);
            byte[] bytes = Utf8WithoutBom.GetBytes(json);
            string temporaryFilePath = CreateTemporaryFilePath(parentDirectory);

            try
            {
                using (var stream = new FileStream(temporaryFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                File.Move(temporaryFilePath, resolvedFilePath, overwrite: true);
            }
            catch (IOException ex)
            {
                throw new RunIndexFormatException($"Failed to replace run index at '{resolvedFilePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RunIndexFormatException($"Failed to replace run index at '{resolvedFilePath}'.", ex);
            }
            finally
            {
                DeleteTemporaryFileIfPresent(temporaryFilePath);
            }
        }

        private static string CreateTemporaryFilePath(string parentDirectory) =>
            Path.Combine(parentDirectory, $".run-index-replace-{Guid.NewGuid():N}.tmp");

        private static string GetFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                throw new RunIndexFormatException($"Run index file path '{path}' is invalid.", ex);
            }
        }

        private static void DeleteTemporaryFileIfPresent(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
