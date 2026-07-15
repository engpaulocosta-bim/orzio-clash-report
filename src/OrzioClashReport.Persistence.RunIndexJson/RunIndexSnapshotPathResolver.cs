using System;
using System.IO;

namespace OrzioClashReport.Persistence.RunIndexJson
{
    /// <summary>Creates and resolves canonical run-index snapshot path references relative to the run-index file location.</summary>
    public sealed class RunIndexSnapshotPathResolver
    {
        public string CreateReference(string runIndexFilePath, string snapshotFilePath)
        {
            string fullRunIndexPath = GetFullPath(runIndexFilePath, "Run index file path", "runIndexFilePath");
            string fullSnapshotPath = GetFullPath(snapshotFilePath, "Snapshot file path", "snapshotFilePath");

            string runIndexDirectory = Path.GetDirectoryName(fullRunIndexPath)
                ?? throw new RunIndexFormatException($"Run index file path '{runIndexFilePath}' does not have a parent directory.");

            string relativePath = Path.GetRelativePath(runIndexDirectory, fullSnapshotPath);
            string reference = relativePath.Replace(Path.DirectorySeparatorChar, '/');
            reference = reference.Replace(Path.AltDirectorySeparatorChar, '/');

            RunIndexPathReferenceValidator.EnsureCanonicalReference(reference, "snapshotPathReference");
            return reference;
        }

        public string ResolveReference(string runIndexFilePath, string snapshotPathReference)
        {
            string fullRunIndexPath = GetFullPath(runIndexFilePath, "Run index file path", "runIndexFilePath");
            RunIndexPathReferenceValidator.EnsureCanonicalReference(snapshotPathReference, "snapshotPathReference");

            string runIndexDirectory = Path.GetDirectoryName(fullRunIndexPath)
                ?? throw new RunIndexFormatException($"Run index file path '{runIndexFilePath}' does not have a parent directory.");

            string localReference = snapshotPathReference.Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(runIndexDirectory, localReference));
        }

        private static string GetFullPath(string path, string label, string parameterName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (path.Trim().Length == 0)
            {
                throw new RunIndexFormatException($"{label} cannot be empty or whitespace.");
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                throw new RunIndexFormatException($"{label} '{path}' is invalid.", ex);
            }
        }
    }
}
