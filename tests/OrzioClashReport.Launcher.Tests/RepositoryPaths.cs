using System.IO;

namespace OrzioClashReport.Launcher.Tests;

internal static class RepositoryPaths
{
    internal static string RepositoryRoot
    {
        get
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "OrzioClashReport.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate the repository root.");
        }
    }

    internal static string SourceRoot => Path.Combine(RepositoryRoot, "src");
}
