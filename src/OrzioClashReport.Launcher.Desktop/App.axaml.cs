using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace OrzioClashReport.Launcher.Desktop
{
    // Fully qualified: the launcher's own Application layer shares the simple name with
    // Avalonia's Application type inside the OrzioClashReport.Launcher namespace.
    public sealed partial class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
