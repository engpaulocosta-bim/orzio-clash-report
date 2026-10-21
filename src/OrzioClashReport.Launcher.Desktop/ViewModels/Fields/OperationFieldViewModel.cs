using System;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Fields
{
    /// <summary>
    /// One labelled input in an operation form. Fields describe what the user is being asked for; the
    /// form itself is still typed, and reads its fields by name to build a request.
    /// </summary>
    /// <remarks>
    /// A field holds the <em>keys</em> of its label and description and looks them up when they are read,
    /// so an open form follows the interface language.
    /// </remarks>
    public abstract class OperationFieldViewModel : LocalizedObservableObject
    {
        private readonly string _labelKey;
        private readonly string _descriptionKey;

        protected OperationFieldViewModel(string labelKey, string descriptionKey)
        {
            if (string.IsNullOrWhiteSpace(labelKey))
            {
                throw new ArgumentException("Field label cannot be empty.", nameof(labelKey));
            }

            _labelKey = labelKey;
            _descriptionKey = descriptionKey ?? string.Empty;
        }

        public string Label => Loc.T(_labelKey);

        public string Description => _descriptionKey.Length == 0 ? string.Empty : Loc.T(_descriptionKey);

        /// <summary>True when the field holds enough for the operation to be submitted.</summary>
        public abstract bool IsComplete { get; }
    }
}
