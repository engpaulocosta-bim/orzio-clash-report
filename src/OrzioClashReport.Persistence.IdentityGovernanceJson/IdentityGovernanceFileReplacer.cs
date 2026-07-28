using OrzioClashReport.Core.Model;
using System;
using System.IO;
using System.Text;

namespace OrzioClashReport.Persistence.IdentityGovernanceJson
{
    /// <summary>Replaces an existing identity-governance file only after a complete temporary write succeeds.</summary>
    public sealed class IdentityGovernanceFileReplacer
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public void ReplaceExisting(IdentityGovernanceDocument document, string filePath)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new IdentityGovernanceFormatException("Identity governance file path cannot be empty or whitespace.");
            }

            string resolvedFilePath = GetFullPath(filePath);
            string? parentDirectory = Path.GetDirectoryName(resolvedFilePath);
            if (string.IsNullOrEmpty(parentDirectory) || !Directory.Exists(parentDirectory))
            {
                throw new IdentityGovernanceFormatException("Identity governance file not found.");
            }

            if (Directory.Exists(resolvedFilePath))
            {
                throw new IdentityGovernanceFormatException("Identity governance destination cannot be an existing directory.");
            }

            if (!File.Exists(resolvedFilePath))
            {
                throw new IdentityGovernanceFormatException("Identity governance file not found.");
            }

            string json = new JsonIdentityGovernanceSerializer().Serialize(document);
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
                throw new IdentityGovernanceFormatException("Failed to replace identity governance document.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IdentityGovernanceFormatException("Failed to replace identity governance document.", ex);
            }
            finally
            {
                DeleteTemporaryFileIfPresent(temporaryFilePath);
            }
        }

        private static string CreateTemporaryFilePath(string parentDirectory) =>
            Path.Combine(parentDirectory, $".identity-governance-replace-{Guid.NewGuid():N}.tmp");

        private static string GetFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                throw new IdentityGovernanceFormatException("Identity governance file path is invalid.", ex);
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
