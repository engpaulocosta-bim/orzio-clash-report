using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// A navigation section that hosts several operation forms, one selected at a time. Keeping one
    /// form visible is what stops the screen from becoming a wall of every flag the engine accepts.
    /// </summary>
    public sealed partial class OperationsSectionViewModel : LocalizedObservableObject
    {
        [ObservableProperty]
        private OperationFormViewModel? _selectedForm;

        public OperationsSectionViewModel(LauncherSection section, IReadOnlyList<OperationFormViewModel> forms)
        {
            if (forms == null)
            {
                throw new ArgumentNullException(nameof(forms));
            }

            if (forms.Count == 0)
            {
                throw new ArgumentException("A section needs at least one form.", nameof(forms));
            }

            _presentation = LauncherSectionPresentation.For(section);
            Section = section;

            foreach (OperationFormViewModel form in forms)
            {
                Forms.Add(form);
            }

            SelectedForm = Forms[0];
        }

        public LauncherSection Section { get; }

        private readonly LauncherSectionPresentation _presentation;

        public string Title => _presentation.Label;

        public string Description => _presentation.Description;

        public ObservableCollection<OperationFormViewModel> Forms { get; } =
            new ObservableCollection<OperationFormViewModel>();

        public void Select(OperationFormViewModel form)
        {
            if (Forms.Contains(form))
            {
                SelectedForm = form;
            }
        }
    }
}
