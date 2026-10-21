using System;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Application.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Fields
{
    /// <summary>
    /// A plain text value, such as a project id, a reviewer alias, or an occurrence index. The
    /// validator checks operational format only; whether the value refers to something that exists is
    /// always the engine's question.
    /// </summary>
    public sealed partial class TextFieldViewModel : OperationFieldViewModel
    {
        private readonly Action? _onChanged;
        private readonly Func<string, string?>? _validate;
        private readonly bool _isOptional;
        private readonly string _watermark;
        private readonly string? _watermarkKey;

        [ObservableProperty]
        private string _value = string.Empty;

        /// <param name="labelKey">The key of the field's label.</param>
        /// <param name="descriptionKey">The key of the field's description.</param>
        /// <param name="watermark">
        /// An example value shown while the box is empty, such as <c>tower-a</c>. It is shown as written
        /// and never translated.
        /// </param>
        /// <param name="watermarkKey">
        /// Use instead of <paramref name="watermark"/> when the example is a word in the interface
        /// language rather than an identifier.
        /// </param>
        public TextFieldViewModel(
            string labelKey,
            string descriptionKey,
            string watermark = "",
            Action? onChanged = null,
            bool isOptional = false,
            Func<string, string?>? validate = null,
            string? watermarkKey = null)
            : base(labelKey, descriptionKey)
        {
            _watermark = watermark;
            _watermarkKey = watermarkKey;
            _onChanged = onChanged;
            _isOptional = isOptional;
            _validate = validate;
        }

        public string Watermark => _watermarkKey != null ? Loc.T(_watermarkKey) : _watermark;

        public bool IsOptional => _isOptional;

        /// <summary>
        /// The validator's message for the current value, computed when read so it follows the language.
        /// Empty when the value is empty (an empty box is not an error) or valid.
        /// </summary>
        public string ValidationMessage => CurrentMessage() ?? string.Empty;

        public bool HasValidationMessage => CurrentMessage() != null;

        public override bool IsComplete
        {
            get
            {
                string trimmed = Value.Trim();

                if (trimmed.Length == 0)
                {
                    return _isOptional;
                }

                return _validate == null || _validate(trimmed) == null;
            }
        }

        private string? CurrentMessage()
        {
            string trimmed = Value.Trim();
            return trimmed.Length == 0 || _validate == null ? null : _validate(trimmed);
        }

        partial void OnValueChanged(string value)
        {
            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(HasValidationMessage));
            OnPropertyChanged(nameof(IsComplete));
            _onChanged?.Invoke();
        }
    }
}
