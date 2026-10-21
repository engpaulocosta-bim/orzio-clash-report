using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>
    /// Declares the order of a sequence of runs. The list is the declaration: it is never sorted, and
    /// a repeated snapshot stays exactly where it was put.
    /// </summary>
    public sealed class IndexSnapshotsFormViewModel : OperationFormViewModel
    {
        private readonly OrderedFilesFieldViewModel _snapshots;
        private readonly PathFieldViewModel _output;

        public IndexSnapshotsFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.IndexSnapshots,
                "op.indexSnapshots.title",
                "op.indexSnapshots.description",
                job,
                engineStatus)
        {
            _snapshots = new OrderedFilesFieldViewModel(
                "op.indexSnapshots.snapshots.label",
                "op.indexSnapshots.snapshots.description",
                dialogs,
                FilePickerFileKind.RunSnapshotJson,
                onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "op.indexSnapshots.output.label",
                "op.indexSnapshots.output.description",
                dialogs,
                FilePickerFileKind.RunIndexJson,
                isDestination: true,
                suggestedFileName: "run-index.json",
                onChanged: NotifyFieldsChanged);

            RegisterField(_snapshots);
            RegisterField(_output);

            AddNote("op.indexSnapshots.note.1");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.IndexSnapshots,
                EngineArgumentBuilder.IndexSnapshots(_snapshots.Paths, _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }
}
