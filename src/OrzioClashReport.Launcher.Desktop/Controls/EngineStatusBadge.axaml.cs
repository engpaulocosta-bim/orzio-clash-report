using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OrzioClashReport.Launcher.Desktop.Controls
{
    /// <summary>
    /// Shows the engine state as glyph plus label plus version. Bind its data context to an
    /// <see cref="ViewModels.EngineStatusViewModel"/>.
    /// </summary>
    public partial class EngineStatusBadge : UserControl
    {
        public EngineStatusBadge()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
