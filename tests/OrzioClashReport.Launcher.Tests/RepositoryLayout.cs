using System;
using System.IO;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// Locates the repository from the test output directory so architecture tests can read the real
    /// project files rather than trusting compiled metadata alone.
    /// </summary>
    internal static class RepositoryLayout
    {
        public static string RootDirectory { get; } = FindRoot();

        public static string ProjectFile(string projectName) =>
            Path.Combine(RootDirectory, "src", projectName, projectName + ".csproj");

        public static string TestProjectFile(string projectName) =>
            Path.Combine(RootDirectory, "tests", projectName, projectName + ".csproj");

        public static string[] AllProjectFiles() =>
            Directory.GetFiles(RootDirectory, "*.csproj", SearchOption.AllDirectories);

        private static string FindRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "OrzioClashReport.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "OrzioClashReport.sln was not found above the test output directory.");
        }
    }
}
