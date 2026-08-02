using CommunityToolkit.Mvvm.ComponentModel;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
    }

    /// <summary>
    /// How prominently a piece of state should read. It is always paired with text and a glyph, so
    /// nothing in the application is distinguishable by colour alone.
    /// </summary>
    public enum StatusSeverity
    {
        Neutral = 0,
        Positive = 1,
        Caution = 2,
        Critical = 3,
    }
}
