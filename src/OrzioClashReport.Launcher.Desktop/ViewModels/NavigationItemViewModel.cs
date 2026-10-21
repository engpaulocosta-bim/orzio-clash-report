using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>One entry in the navigation list, carrying both its glyph and its label.</summary>
    public sealed class NavigationItemViewModel : LocalizedObservableObject
    {
        private readonly LauncherSectionPresentation _presentation;

        public NavigationItemViewModel(LauncherSectionPresentation presentation, object content)
        {
            _presentation = presentation;
            Section = presentation.Section;
            Glyph = presentation.Glyph;
            Content = content;
        }

        public LauncherSection Section { get; }

        /// <summary>Read each time it is needed, so it follows the interface language.</summary>
        public string Label => _presentation.Label;

        public string Glyph { get; }

        public string Description => _presentation.Description;

        public object Content { get; }
    }
}
