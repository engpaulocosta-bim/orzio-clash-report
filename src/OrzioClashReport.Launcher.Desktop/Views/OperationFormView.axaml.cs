using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OrzioClashReport.Launcher.Desktop.Views
{
    /// <summary>Renders any operation form from its typed fields.</summary>
    public partial class OperationFormView : UserControl
    {
        public OperationFormView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
