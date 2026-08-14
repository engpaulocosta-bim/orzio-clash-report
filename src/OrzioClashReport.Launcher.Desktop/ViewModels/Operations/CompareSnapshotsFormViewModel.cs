using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>Compares two persisted snapshots in the explicit previous/current roles the user chooses.</summary>
    public sealed class CompareSnapshotsFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _previous;
        private readonly PathFieldViewModel _current;
        private readonly PathFieldViewModel _output;

        public CompareSnapshotsFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.CompareSnapshots,
                "Comparar dois snapshots",
                "Compara dois snapshots já criados. Os papéis anterior e atual são seus: nada é ordenado por data, revisão ou nome de ficheiro.",
                job,
                engineStatus)
        {
            _previous = new PathFieldViewModel(
                "Snapshot anterior",
                "O run que serve de ponto de partida.",
                dialogs,
                FilePickerFileKind.RunSnapshotJson,
                onChanged: NotifyFieldsChanged);

            _current = new PathFieldViewModel(
                "Snapshot atual",
                "O run que serve de ponto de chegada.",
                dialogs,
                FilePickerFileKind.RunSnapshotJson,
                onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "Destino do relatório",
                "O HTML da comparação. É derivado e pode ser gerado de novo.",
                dialogs,
                FilePickerFileKind.HtmlReport,
                isDestination: true,
                suggestedFileName: "comparison.html",
                onChanged: NotifyFieldsChanged);

            RegisterField(_previous);
            RegisterField(_current);
            RegisterField(_output);

            Notes.Add("Matching e lifecycle são recalculados a partir da evidência, e não são guardados.");
            Notes.Add("Comparação longitudinal continua experimental: ainda não foi validada em exports reais sequenciais.");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.CompareSnapshots,
                EngineArgumentBuilder.CompareSnapshots(_previous.Value, _current.Value, _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }
}
