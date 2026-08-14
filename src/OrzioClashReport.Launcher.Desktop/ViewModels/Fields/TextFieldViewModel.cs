using System;
using CommunityToolkit.Mvvm.ComponentModel;

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

        [ObservableProperty]
        private string _value = string.Empty;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private bool _hasValidationMessage;

        public TextFieldViewModel(
            string label,
            string description,
            string watermark = "",
            Action? onChanged = null,
            bool isOptional = false,
            Func<string, string?>? validate = null)
            : base(label, description)
        {
            Watermark = watermark;
            _onChanged = onChanged;
            _isOptional = isOptional;
            _validate = validate;
        }

        public string Watermark { get; }

        public bool IsOptional => _isOptional;

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

        partial void OnValueChanged(string value)
        {
            string trimmed = value.Trim();

            string? message = trimmed.Length == 0 || _validate == null ? null : _validate(trimmed);

            ValidationMessage = message ?? string.Empty;
            HasValidationMessage = message != null;

            OnPropertyChanged(nameof(IsComplete));
            _onChanged?.Invoke();
        }
    }
}
