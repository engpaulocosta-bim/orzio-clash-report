using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Application.Presentation;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>One entry in the navigation list, carrying both its glyph and its label.</summary>
    public sealed class NavigationItemViewModel : ObservableObject
    {
        public NavigationItemViewModel(LauncherSectionPresentation presentation, object content)
        {
            Section = presentation.Section;
            Label = presentation.Label;
            Glyph = presentation.Glyph;
            Description = presentation.Description;
            Content = content;
        }

        public LauncherSection Section { get; }

        public string Label { get; }

        public string Glyph { get; }

        public string Description { get; }

        public object Content { get; }
    }
}
