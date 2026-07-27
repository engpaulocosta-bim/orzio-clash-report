using System;
using System.IO;

namespace OrzioClashReport.Persistence.ProjectCatalogJson
{
    /// <summary>Creates and resolves canonical project-catalog path references relative to the project-catalog file location.</summary>
    public sealed class ProjectCatalogPathResolver
    {
        public string CreateReference(string projectCatalogFilePath, string targetFilePath)
        {
            string fullProjectCatalogPath = GetFullPath(projectCatalogFilePath, "Project catalog file path", "projectCatalogFilePath");
            string fullTargetPath = GetFullPath(targetFilePath, "Target file path", "targetFilePath");

            string projectCatalogDirectory = Path.GetDirectoryName(fullProjectCatalogPath)
                ?? throw new ProjectCatalogFormatException(
                    $"Project catalog file path '{projectCatalogFilePath}' does not have a parent directory.");

            EnsureTargetIsWithinProjectDirectory(fullTargetPath, projectCatalogDirectory, targetFilePath);

            string relativePath = Path.GetRelativePath(projectCatalogDirectory, fullTargetPath);
            string reference = relativePath.Replace(Path.DirectorySeparatorChar, '/');
            reference = reference.Replace(Path.AltDirectorySeparatorChar, '/');

            ProjectCatalogPathReferenceValidator.EnsureCanonicalReference(reference, "pathReference");
            return reference;
        }

        public string ResolveReference(string projectCatalogFilePath, string pathReference)
        {
            string fullProjectCatalogPath = GetFullPath(projectCatalogFilePath, "Project catalog file path", "projectCatalogFilePath");
            ProjectCatalogPathReferenceValidator.EnsureCanonicalReference(pathReference, "pathReference");

            string projectCatalogDirectory = Path.GetDirectoryName(fullProjectCatalogPath)
                ?? throw new ProjectCatalogFormatException(
                    $"Project catalog file path '{projectCatalogFilePath}' does not have a parent directory.");

            string localReference = pathReference.Replace('/', Path.DirectorySeparatorChar);
            string resolvedPath = Path.GetFullPath(Path.Combine(projectCatalogDirectory, localReference));

            if (!IsWithinDirectory(projectCatalogDirectory, resolvedPath))
            {
                throw new ProjectCatalogFormatException(
                    $"Project catalog path reference '{pathReference}' resolves outside the project catalog directory.");
            }

            return resolvedPath;
        }

        private static void EnsureTargetIsWithinProjectDirectory(
            string fullTargetPath,
            string projectCatalogDirectory,
            string originalTargetPath)
        {
            if (!IsWithinDirectory(projectCatalogDirectory, fullTargetPath))
            {
                throw new ProjectCatalogFormatException(
                    $"Target file path '{originalTargetPath}' must be inside project catalog directory '{projectCatalogDirectory}'.");
            }
        }

        private static bool IsWithinDirectory(string directoryPath, string candidatePath)
        {
            string fullDirectoryPath = Path.GetFullPath(directoryPath);
            string normalizedDirectoryPath = fullDirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return candidatePath.StartsWith(normalizedDirectoryPath, comparison);
        }

        private static string GetFullPath(string path, string label, string parameterName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (path.Trim().Length == 0)
            {
                throw new ProjectCatalogFormatException($"{label} cannot be empty or whitespace.");
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                throw new ProjectCatalogFormatException($"{label} '{path}' is invalid.", ex);
            }
        }
    }
}
