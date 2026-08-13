using System;
using Avalonia;

namespace OrzioClashReport.Launcher.Desktop
{
    /// <summary>
    /// Process entry point. Everything the application needs is wired by hand in
    /// <see cref="App"/>: there is no dependency-injection container, by design.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Referenced by the Avalonia designer and by StartWithClassicDesktopLifetime above.
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
