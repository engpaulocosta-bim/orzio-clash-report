using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OrzioClashReport.Launcher.Desktop
{
    /// <summary>The application shell window.</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
