using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OrzioClashReport.Launcher.Desktop.Views
{
    public sealed partial class HomeView : UserControl
    {
        public HomeView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
