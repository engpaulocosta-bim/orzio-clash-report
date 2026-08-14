using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>Traverses the adjacent pairs of an already-declared run index.</summary>
    public sealed class CompareIndexFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _index;
        private readonly PathFieldViewModel _output;

        public CompareIndexFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.CompareIndex,
                "Comparar índice",
                "Percorre apenas pares adjacentes, na ordem declarada no índice.",
                job,
                engineStatus)
        {
            _index = new PathFieldViewModel(
                "Índice de runs", "O run-index JSON que declara a sequência.",
                dialogs, FilePickerFileKind.RunIndexJson, onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "Destino do relatório", "O HTML longitudinal. É derivado e pode ser gerado de novo.",
                dialogs, FilePickerFileKind.HtmlReport,
                isDestination: true, suggestedFileName: "longitudinal.html", onChanged: NotifyFieldsChanged);

            RegisterField(_index);
            RegisterField(_output);

            Notes.Add("Os caminhos de continuidade são derivados e recalculáveis: não provam que se trata do mesmo clash.");
            Notes.Add("Comparação longitudinal continua experimental: ainda não foi validada em três exports reais sequenciais.");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.CompareIndex,
                EngineArgumentBuilder.CompareIndex(_index.Value, _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }
}
