using System;
using System.IO;
using System.Text;

namespace OrzioClashReport.Output.Html
{
    /// <summary>Writes a derived HTML report only after a complete temporary UTF-8 write succeeds in the destination directory.</summary>
    public sealed class DerivedHtmlReportWriter
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public void Write(string html, string filePath)
        {
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new InvalidOperationException("Derived HTML report path cannot be empty or whitespace.");
            }

            string resolvedFilePath = GetFullPath(filePath);
            string? parentDirectory = Path.GetDirectoryName(resolvedFilePath);
            if (string.IsNullOrEmpty(parentDirectory) || !Directory.Exists(parentDirectory))
            {
                throw new InvalidOperationException($"Derived HTML report parent directory not found: {parentDirectory ?? resolvedFilePath}");
            }

            if (Directory.Exists(resolvedFilePath))
            {
                throw new InvalidOperationException($"Derived HTML report destination cannot be an existing directory: {resolvedFilePath}");
            }

            byte[] bytes = Utf8WithoutBom.GetBytes(html);
            string temporaryFilePath = Path.Combine(parentDirectory, $".derived-html-report-{Guid.NewGuid():N}.tmp");

            try
            {
                using (var stream = new FileStream(temporaryFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                if (File.Exists(resolvedFilePath))
                {
                    File.Replace(temporaryFilePath, resolvedFilePath, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(temporaryFilePath, resolvedFilePath);
                }
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("Failed to write derived HTML report.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("Failed to write derived HTML report.", ex);
            }
            finally
            {
                DeleteTemporaryFileIfPresent(temporaryFilePath);
            }
        }

        private static string GetFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                throw new InvalidOperationException("Derived HTML report path is invalid.", ex);
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
