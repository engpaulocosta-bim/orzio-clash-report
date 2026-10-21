using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>Freezes one run as immutable evidence, from its export and its declared manifest.</summary>
    public sealed class SnapshotFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _xml;
        private readonly PathFieldViewModel _manifest;
        private readonly PathFieldViewModel _output;

        public SnapshotFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.Snapshot,
                "op.snapshot.title",
                "op.snapshot.description",
                job,
                engineStatus)
        {
            _xml = new PathFieldViewModel(
                "op.snapshot.xml.label",
                "op.snapshot.xml.description",
                dialogs,
                FilePickerFileKind.NavisworksClashXml,
                onChanged: NotifyFieldsChanged);

            _manifest = new PathFieldViewModel(
                "op.snapshot.manifest.label",
                "op.snapshot.manifest.description",
                dialogs,
                FilePickerFileKind.RunManifestJson,
                onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "op.snapshot.output.label",
                "op.snapshot.output.description",
                dialogs,
                FilePickerFileKind.RunSnapshotJson,
                isDestination: true,
                suggestedFileName: "run-snapshot.json",
                onChanged: NotifyFieldsChanged);

            RegisterField(_xml);
            RegisterField(_manifest);
            RegisterField(_output);

            AddNote("op.snapshot.note.1");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.Snapshot,
                EngineArgumentBuilder.Snapshot(_xml.Value, _manifest.Value, _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }
}
