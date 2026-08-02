using System;
using Avalonia;

namespace OrzioClashReport.Launcher.Desktop
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        /// <summary>Used by the Avalonia designer as well as by <see cref="Main"/>.</summary>
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
        }
    }
}
