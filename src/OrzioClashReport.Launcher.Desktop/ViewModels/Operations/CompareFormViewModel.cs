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
                "Comparar dois runs",
                "Compara dois exports com os respetivos manifests. Anterior e atual são papéis explícitos que você declara.",
                job,
                engineStatus)
        {
            _previousXml = new PathFieldViewModel(
                "XML anterior", "O export do run de partida.",
                dialogs, FilePickerFileKind.NavisworksClashXml, onChanged: NotifyFieldsChanged);

            _previousManifest = new PathFieldViewModel(
                "Manifest anterior", "As revisões declaradas para o run de partida.",
                dialogs, FilePickerFileKind.RunManifestJson, onChanged: NotifyFieldsChanged);

            _currentXml = new PathFieldViewModel(
                "XML atual", "O export do run de chegada.",
                dialogs, FilePickerFileKind.NavisworksClashXml, onChanged: NotifyFieldsChanged);

            _currentManifest = new PathFieldViewModel(
                "Manifest atual", "As revisões declaradas para o run de chegada.",
                dialogs, FilePickerFileKind.RunManifestJson, onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "Destino do relatório", "O HTML da comparação.",
                dialogs, FilePickerFileKind.HtmlReport,
                isDestination: true, suggestedFileName: "comparison.html", onChanged: NotifyFieldsChanged);

            RegisterField(_previousXml);
            RegisterField(_previousManifest);
            RegisterField(_currentXml);
            RegisterField(_currentManifest);
            RegisterField(_output);

            Notes.Add("A ordem cronológica nunca é inferida: os papéis anterior e atual são os que declarar aqui.");
            Notes.Add("Comparação longitudinal continua experimental: ainda não foi validada em exports reais sequenciais.");
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
