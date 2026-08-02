using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.Dialogs
{
    /// <summary>
    /// Asks what to do about an existing report. Replacing is offered but is never the default, and
    /// closing the window is a cancellation rather than a silent overwrite.
    /// </summary>
    public sealed partial class OutputCollisionWindow : Window
    {
        public OutputCollisionWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }

        internal OutputCollisionDecision Decision { get; private set; } = OutputCollisionDecision.Cancel;

        internal void ShowExistingFile(string path)
        {
            var text = this.FindControl<TextBlock>("ExistingFileText");
            if (text != null)
            {
                text.Text = Path.GetFileName(path);
            }
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            Decision = OutputCollisionDecision.Cancel;
            Close();
        }

        private void OnReplace(object? sender, RoutedEventArgs e)
        {
            Decision = OutputCollisionDecision.Replace;
            Close();
        }

        private void OnChooseAnother(object? sender, RoutedEventArgs e)
        {
            Decision = OutputCollisionDecision.UseDifferentPath;
            Close();
        }
    }

    /// <summary>
    /// Shows the collision window and, when the human chooses a different name, the save picker.
    /// Nothing is replaced without an explicit choice.
    /// </summary>
    public sealed class DialogOutputCollisionPrompt : IOutputCollisionPrompt
    {
        private readonly Func<Window?> _owner;
        private readonly IFileDialogs _fileDialogs;

        public DialogOutputCollisionPrompt(Func<Window?> owner, IFileDialogs fileDialogs)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
        }

        public async Task<OutputCollisionResolution> ResolveAsync(
            string existingPath, CancellationToken cancellationToken)
        {
            Window? owner = _owner();
            if (owner == null)
            {
                return OutputCollisionResolution.Cancel;
            }

            var window = new OutputCollisionWindow();
            window.ShowExistingFile(existingPath);
            await window.ShowDialog(owner).ConfigureAwait(true);

            switch (window.Decision)
            {
                case OutputCollisionDecision.Replace:
                    return OutputCollisionResolution.Replace;

                case OutputCollisionDecision.UseDifferentPath:
                    string? replacement = await _fileDialogs
                        .PickSaveFileAsync(
                            "Guardar relatório como",
                            PickedFileKind.HtmlReport,
                            Path.GetFileName(existingPath))
                        .ConfigureAwait(true);

                    return replacement == null
                        ? OutputCollisionResolution.Cancel
                        : OutputCollisionResolution.UseDifferentPath(Path.GetFullPath(replacement));

                default:
                    return OutputCollisionResolution.Cancel;
            }
        }
    }
}
