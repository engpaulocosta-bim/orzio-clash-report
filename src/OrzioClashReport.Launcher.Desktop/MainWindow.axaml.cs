using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OrzioClashReport.Launcher.Desktop
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
