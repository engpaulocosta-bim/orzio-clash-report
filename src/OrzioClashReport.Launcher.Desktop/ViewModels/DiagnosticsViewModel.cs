using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// The diagnostic bundle, generated only on an explicit action and only after the human has seen
    /// the complete list of what it contains and the exact redacted log that would go into it.
    /// </summary>
    public sealed partial class DiagnosticsViewModel : ViewModelBase
    {
        internal const int LogPreviewLimit = 200;

        private readonly IDiagnosticBundleBuilder _builder;
        private readonly IFileDialogs _dialogs;
        private readonly IOutputRevealer _revealer;

        [ObservableProperty]
        private bool _isPreviewVisible;

        [ObservableProperty]
        private string? _lastBundleFileName;

        [ObservableProperty]
        private string? _failureMessage;

        public DiagnosticsViewModel(
            IDiagnosticBundleBuilder builder, IFileDialogs dialogs, IOutputRevealer revealer)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _revealer = revealer ?? throw new ArgumentNullException(nameof(revealer));

            IncludedFiles = new ObservableCollection<string>();
            LogPreviewLines = new ObservableCollection<string>();
        }

        /// <summary>The complete list of files the bundle would contain. Nothing else is ever added.</summary>
        public ObservableCollection<string> IncludedFiles { get; }

        public ObservableCollection<string> LogPreviewLines { get; }

        public bool HasLogPreview => LogPreviewLines.Count > 0;

        public bool HasBundle => LastBundleFileName != null;

        public bool HasFailure => FailureMessage != null;

        public string Explanation =>
            "Um pacote de diagnóstico contém apenas os ficheiros listados abaixo. Nunca inclui ficheiros do "
                + "cliente, caminhos absolutos, argumentos de execução ou o conteúdo dos seus modelos.";

        [RelayCommand]
        private void ShowPreview()
        {
            DiagnosticBundlePreview preview = _builder.Preview();

            IncludedFiles.Clear();
            foreach (DiagnosticBundleItem item in preview.Items)
            {
                IncludedFiles.Add(DiagnosticBundleContents.FileNameFor(item));
            }

            LogPreviewLines.Clear();
            IReadOnlyList<string> lines = preview.RedactedLogPreviewLines;
            int start = Math.Max(0, lines.Count - LogPreviewLimit);
            for (int i = start; i < lines.Count; i++)
            {
                LogPreviewLines.Add(lines[i]);
            }

            IsPreviewVisible = true;
            OnPropertyChanged(nameof(HasLogPreview));
        }

        [RelayCommand(CanExecute = nameof(CanCreate))]
        private async Task CreateBundleAsync()
        {
            FailureMessage = null;

            string? destination = await _dialogs
                .PickSaveFileAsync("Guardar pacote de diagnóstico", PickedFileKind.HtmlReport, "orzio-diagnostics.zip")
                .ConfigureAwait(true);

            if (destination == null)
            {
                return;
            }

            try
            {
                string written = await _builder
                    .BuildAsync(Path.GetFullPath(destination), CancellationToken.None)
                    .ConfigureAwait(true);

                LastBundleFileName = Path.GetFileName(written);
                _lastBundlePath = written;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                FailureMessage = "Não foi possível escrever o pacote de diagnóstico: " + ex.Message;
            }

            OnPropertyChanged(nameof(HasBundle));
            OnPropertyChanged(nameof(HasFailure));
        }

        [RelayCommand]
        private Task RevealBundleAsync() =>
            _lastBundlePath == null ? Task.CompletedTask : _revealer.RevealAsync(_lastBundlePath);

        private string? _lastBundlePath;

        /// <summary>A bundle is only offered once the human has actually seen what is in it.</summary>
        private bool CanCreate() => IsPreviewVisible;

        partial void OnIsPreviewVisibleChanged(bool value)
        {
            _ = value;
            CreateBundleCommand.NotifyCanExecuteChanged();
        }

        partial void OnLastBundleFileNameChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(HasBundle));
        }

        partial void OnFailureMessageChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(HasFailure));
        }
    }
}
