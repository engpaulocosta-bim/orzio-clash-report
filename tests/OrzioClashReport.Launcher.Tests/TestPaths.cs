using System.IO;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// Builds an OS-correct absolute path for tests that only need one to satisfy
    /// <c>Path.IsPathFullyQualified</c> — never a real location on disk. A Unix-style literal like
    /// <c>/reports/output.html</c> is fully qualified on Linux but not on Windows, which is exactly
    /// the mismatch that made these argument-vector tests pass on the Linux dev box and fail on the
    /// Windows CI runner.
    /// </summary>
    internal static class TestPaths
    {
        public static string Absolute(string relativePath) =>
            Path.Combine(Path.GetTempPath(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
