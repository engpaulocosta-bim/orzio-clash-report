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

        private readonly string _titleKey;
        private readonly string _descriptionKey;

        public OperationSectionViewModel(
            string titleKey, string descriptionKey, IReadOnlyList<OperationFormViewModel> forms)
        {
            _titleKey = titleKey ?? throw new ArgumentNullException(nameof(titleKey));
            _descriptionKey = descriptionKey ?? throw new ArgumentNullException(nameof(descriptionKey));

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

        public string Title => Text(_titleKey);

        public string Description => Text(_descriptionKey);

        public IReadOnlyList<OperationFormViewModel> Forms { get; }
    }
}
