using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Fields
{
    /// <summary>
    /// A single file path, chosen through the platform picker so the value is always absolute. A
    /// destination field uses the save picker; every other field opens an existing file.
    /// </summary>
    public sealed partial class PathFieldViewModel : OperationFieldViewModel
    {
        private readonly IFileDialogService _fileDialogService;
        private readonly FilePickerFileKind _kind;
        private readonly bool _isDestination;
        private readonly string _suggestedFileName;
        private readonly Action? _onChanged;

        [ObservableProperty]
        private string _value = string.Empty;

        public PathFieldViewModel(
            string label,
            string description,
            IFileDialogService fileDialogService,
            FilePickerFileKind kind,
            bool isDestination = false,
            string suggestedFileName = "",
            Action? onChanged = null)
            : base(label, description)
        {
            _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
            _kind = kind;
            _isDestination = isDestination;
            _suggestedFileName = suggestedFileName;
            _onChanged = onChanged;
        }

        public bool IsDestination => _isDestination;

        public override bool IsComplete => Value.Length > 0;

        public string Watermark => _isDestination ? "Nenhum destino escolhido" : "Nenhum ficheiro escolhido";

        [RelayCommand]
        private async Task BrowseAsync()
        {
            string? startDirectory = Value.Length == 0 ? null : Path.GetDirectoryName(Value);

            string? picked = _isDestination
                ? await _fileDialogService
                    .PickSaveFileAsync("Escolher destino", _kind, _suggestedFileName, startDirectory)
                    .ConfigureAwait(true)
                : await _fileDialogService
                    .PickOpenFileAsync("Escolher ficheiro", _kind, startDirectory)
                    .ConfigureAwait(true);

            if (picked != null)
            {
                Value = picked;
            }
        }

        partial void OnValueChanged(string value) => _onChanged?.Invoke();
    }
}
