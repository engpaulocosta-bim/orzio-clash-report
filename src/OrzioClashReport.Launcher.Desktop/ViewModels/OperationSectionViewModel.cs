using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// A section that groups the typed forms belonging to one part of the workflow. The section
    /// itself has no behaviour beyond choosing which form is on screen.
    /// </summary>
    public sealed partial class OperationSectionViewModel : ViewModelBase
    {
        [ObservableProperty]
        private OperationFormViewModel _selectedForm;

        public OperationSectionViewModel(
            string title, string description, IReadOnlyList<OperationFormViewModel> forms)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));

            if (forms == null)
            {
                throw new ArgumentNullException(nameof(forms));
            }

            if (forms.Count == 0)
            {
                throw new ArgumentException("A section must have at least one form.", nameof(forms));
            }

            Forms = new ReadOnlyCollection<OperationFormViewModel>(new List<OperationFormViewModel>(forms));
            _selectedForm = Forms[0];
        }

        public string Title { get; }

        public string Description { get; }

        public IReadOnlyList<OperationFormViewModel> Forms { get; }
    }
}
