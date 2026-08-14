using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OrzioClashReport.Launcher.Desktop.Views
{
    /// <summary>The shared running/result panel. Bind its data context to a <see cref="ViewModels.JobViewModel"/>.</summary>
    public partial class JobPanelView : UserControl
    {
        public JobPanelView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
