using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Fields
{
    /// <summary>
    /// One labelled input in an operation form. Fields describe what the user is being asked for; the
    /// form itself is still typed, and reads its fields by name to build a request.
    /// </summary>
    public abstract class OperationFieldViewModel : ObservableObject
    {
        protected OperationFieldViewModel(string label, string description)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Field label cannot be empty.", nameof(label));
            }

            Label = label;
            Description = description ?? string.Empty;
        }

        public string Label { get; }

        public string Description { get; }

        /// <summary>True when the field holds enough for the operation to be submitted.</summary>
        public abstract bool IsComplete { get; }
    }
}
