namespace OrzioClashReport.Launcher.Tests;

/// <summary>Locates the stand-in engine that the gateway tests drive as a real child process.</summary>
internal static class FakeEngine
{
    internal static string ExecutablePath
    {
        get
        {
            string directory = AppContext.BaseDirectory;
            string windows = Path.Combine(directory, "orzio-fake-engine.exe");
            string other = Path.Combine(directory, "orzio-fake-engine");

            if (File.Exists(windows))
            {
                return windows;
            }

            if (File.Exists(other))
            {
                return other;
            }

            throw new InvalidOperationException(
                $"The fake engine was not copied next to the tests in '{directory}'.");
        }
    }
}
