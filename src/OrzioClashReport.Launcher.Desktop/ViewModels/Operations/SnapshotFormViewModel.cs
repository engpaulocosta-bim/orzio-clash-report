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
                "Criar snapshot",
                "Congela um run como evidência imutável. O snapshot nunca é substituído: se o destino já existir, escolha outro nome.",
                job,
                engineStatus)
        {
            _xml = new PathFieldViewModel(
                "Export XML",
                "O ficheiro exportado pelo Clash Detective.",
                dialogs,
                FilePickerFileKind.NavisworksClashXml,
                onChanged: NotifyFieldsChanged);

            _manifest = new PathFieldViewModel(
                "Run manifest",
                "As revisões e os testes executados declarados para este run.",
                dialogs,
                FilePickerFileKind.RunManifestJson,
                onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "Destino do snapshot",
                "Onde o snapshot JSON será criado.",
                dialogs,
                FilePickerFileKind.RunSnapshotJson,
                isDestination: true,
                suggestedFileName: "run-snapshot.json",
                onChanged: NotifyFieldsChanged);

            RegisterField(_xml);
            RegisterField(_manifest);
            RegisterField(_output);

            Notes.Add("O snapshot é evidência: é sempre criado de novo e nunca sobrescrito.");
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
