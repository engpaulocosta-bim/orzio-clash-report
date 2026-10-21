using System;
using System.Collections.Generic;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// A section whose operations are not wired yet. It states plainly what the section will do and
    /// which engine commands stand behind it, rather than showing an empty screen.
    /// </summary>
    public sealed class SectionPlaceholderViewModel : LocalizedObservableObject
    {
        public SectionPlaceholderViewModel(LauncherSection section, IReadOnlyList<string> plannedOperations)
        {
            _presentation = LauncherSectionPresentation.For(section);
            PlannedOperations = plannedOperations ?? throw new ArgumentNullException(nameof(plannedOperations));
        }

        private readonly LauncherSectionPresentation _presentation;

        public string Title => _presentation.Label;

        public string Description => _presentation.Description;

        public IReadOnlyList<string> PlannedOperations { get; }
    }
}
