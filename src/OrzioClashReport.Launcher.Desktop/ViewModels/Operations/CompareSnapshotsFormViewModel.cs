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
                "op.compareSnapshots.title",
                "op.compareSnapshots.description",
                job,
                engineStatus)
        {
            _previous = new PathFieldViewModel(
                "op.compareSnapshots.previous.label",
                "op.compareSnapshots.previous.description",
                dialogs,
                FilePickerFileKind.RunSnapshotJson,
                onChanged: NotifyFieldsChanged);

            _current = new PathFieldViewModel(
                "op.compareSnapshots.current.label",
                "op.compareSnapshots.current.description",
                dialogs,
                FilePickerFileKind.RunSnapshotJson,
                onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "op.compareSnapshots.output.label",
                "op.compareSnapshots.output.description",
                dialogs,
                FilePickerFileKind.HtmlReport,
                isDestination: true,
                suggestedFileName: "comparison.html",
                onChanged: NotifyFieldsChanged);

            RegisterField(_previous);
            RegisterField(_current);
            RegisterField(_output);

            AddNote("op.compareSnapshots.note.1");
            AddNote("op.compareSnapshots.note.2");
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
