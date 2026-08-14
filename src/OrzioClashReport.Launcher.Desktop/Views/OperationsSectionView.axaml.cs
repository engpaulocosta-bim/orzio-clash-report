using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OrzioClashReport.Launcher.Desktop.Views
{
    /// <summary>A section that hosts several operation forms, one shown at a time.</summary>
    public partial class OperationsSectionView : UserControl
    {
        public OperationsSectionView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
