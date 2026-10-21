using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>Compares two runs directly from their exports and manifests, without creating snapshots.</summary>
    public sealed class CompareFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _previousXml;
        private readonly PathFieldViewModel _previousManifest;
        private readonly PathFieldViewModel _currentXml;
        private readonly PathFieldViewModel _currentManifest;
        private readonly PathFieldViewModel _output;

        public CompareFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.Compare,
                "op.compare.title",
                "op.compare.description",
                job,
                engineStatus)
        {
            _previousXml = new PathFieldViewModel(
                "op.compare.previousXml.label", "op.compare.previousXml.description",
                dialogs, FilePickerFileKind.NavisworksClashXml, onChanged: NotifyFieldsChanged);

            _previousManifest = new PathFieldViewModel(
                "op.compare.previousManifest.label", "op.compare.previousManifest.description",
                dialogs, FilePickerFileKind.RunManifestJson, onChanged: NotifyFieldsChanged);

            _currentXml = new PathFieldViewModel(
                "op.compare.currentXml.label", "op.compare.currentXml.description",
                dialogs, FilePickerFileKind.NavisworksClashXml, onChanged: NotifyFieldsChanged);

            _currentManifest = new PathFieldViewModel(
                "op.compare.currentManifest.label", "op.compare.currentManifest.description",
                dialogs, FilePickerFileKind.RunManifestJson, onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "op.compare.output.label", "op.compare.output.description",
                dialogs, FilePickerFileKind.HtmlReport,
                isDestination: true, suggestedFileName: "comparison.html", onChanged: NotifyFieldsChanged);

            RegisterField(_previousXml);
            RegisterField(_previousManifest);
            RegisterField(_currentXml);
            RegisterField(_currentManifest);
            RegisterField(_output);

            AddNote("op.compare.note.1");
            AddNote("op.compare.note.2");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.Compare,
                EngineArgumentBuilder.Compare(
                    _previousXml.Value,
                    _previousManifest.Value,
                    _currentXml.Value,
                    _currentManifest.Value,
                    _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }
}
