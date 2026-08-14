using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// Locates the built fake engine executable. The integration tests launch it as a real child
    /// process, which is the only way to exercise redirection, encoding, timeouts and process-tree
    /// termination for real.
    /// </summary>
    internal static class FakeEngineLocation
    {
        public static string ExecutablePath { get; } = Find();

        private static string Find()
        {
            string fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "fake-engine.exe"
                : "fake-engine";

            string binDirectory = Path.Combine(
                RepositoryLayout.RootDirectory, "tests", "OrzioClashReport.Launcher.FakeEngine", "bin");

            if (Directory.Exists(binDirectory))
            {
                // Prefer the configuration this test assembly was itself built in.
                string configuration = AppContext.BaseDirectory.Contains(
                    Path.DirectorySeparatorChar + "Debug" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    ? "Debug"
                    : "Release";

                string preferred = Path.Combine(binDirectory, configuration, "net8.0", fileName);
                if (File.Exists(preferred))
                {
                    return preferred;
                }

                foreach (string candidate in Directory.GetFiles(binDirectory, fileName, SearchOption.AllDirectories))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                $"The fake engine was not found under '{binDirectory}'. Build the solution before running the tests.");
        }
    }
}
