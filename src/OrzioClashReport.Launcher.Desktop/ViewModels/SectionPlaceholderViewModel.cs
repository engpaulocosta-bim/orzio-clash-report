using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Application.Presentation;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// A section whose operations are not wired yet. It states plainly what the section will do and
    /// which engine commands stand behind it, rather than showing an empty screen.
    /// </summary>
    public sealed class SectionPlaceholderViewModel : ObservableObject
    {
        public SectionPlaceholderViewModel(LauncherSection section, IReadOnlyList<string> plannedOperations)
        {
            LauncherSectionPresentation presentation = LauncherSectionPresentation.For(section);

            Title = presentation.Label;
            Description = presentation.Description;
            PlannedOperations = plannedOperations ?? throw new ArgumentNullException(nameof(plannedOperations));
        }

        public string Title { get; }

        public string Description { get; }

        public IReadOnlyList<string> PlannedOperations { get; }
    }
}
