using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Fields
{
    /// <summary>A plain text value, such as a project id or a reviewer alias.</summary>
    public sealed partial class TextFieldViewModel : OperationFieldViewModel
    {
        private readonly Action? _onChanged;

        [ObservableProperty]
        private string _value = string.Empty;

        public TextFieldViewModel(
            string label, string description, string watermark = "", Action? onChanged = null)
            : base(label, description)
        {
            Watermark = watermark;
            _onChanged = onChanged;
        }

        public string Watermark { get; }

        public override bool IsComplete => Value.Trim().Length > 0;

        partial void OnValueChanged(string value) => _onChanged?.Invoke();
    }
}
