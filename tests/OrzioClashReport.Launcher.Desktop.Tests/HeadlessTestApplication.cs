using Avalonia;
using Avalonia.Headless;
using OrzioClashReport.Launcher.Desktop;

[assembly: AvaloniaTestApplication(typeof(OrzioClashReport.Launcher.Desktop.Tests.HeadlessTestApplication))]

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    /// <summary>
    /// Boots the real <see cref="App"/> headlessly, so the tests exercise the same styles, tokens and
    /// templates the shipped application uses rather than a stand-in.
    /// </summary>
    public static class HeadlessTestApplication
    {
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}
