using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OrzioClashReport.Launcher.Desktop.Composition;

namespace OrzioClashReport.Launcher.Desktop
{
    // Fully qualified: the launcher's own Application layer shares the simple name with
    // Avalonia's Application type inside the OrzioClashReport.Launcher namespace.
    public sealed partial class App : Avalonia.Application
    {
        private MainWindow? _mainWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _mainWindow = new MainWindow();

                CompositionRoot root = CompositionRoot.Create(() => _mainWindow);
                _mainWindow.DataContext = root.CreateShell();

                desktop.MainWindow = _mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
