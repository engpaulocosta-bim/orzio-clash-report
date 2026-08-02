using System.IO;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>XML export plus its declared manifest, persisted as one immutable snapshot.</summary>
    public sealed class CreateSnapshotFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _xml;
        private readonly FileFieldViewModel _manifest;
        private readonly FileFieldViewModel _output;

        public CreateSnapshotFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Criar snapshot",
                "Guarda um run de coordenação como evidência imutável. O motor nunca substitui um snapshot existente.",
                LauncherOperationKind.CreateSnapshot,
                runner,
                engine)
        {
            _xml = Add(FileFieldViewModel.Input(
                "Export XML", "O export do Clash Detective deste run.", dialogs, PickedFileKind.ClashXml));
            _manifest = Add(FileFieldViewModel.Input(
                "Run manifest",
                "As revisões e os testes executados, declarados explicitamente.",
                dialogs,
                PickedFileKind.RunManifestJson));
            _output = Add(FileFieldViewModel.Destination(
                "Snapshot",
                "Onde guardar o snapshot.",
                dialogs,
                PickedFileKind.SnapshotJson,
                () => SuggestedName(_xml.Path, ".snapshot.json")));
        }

        public override bool CanBuild => _xml.HasValue && _manifest.HasValue && _output.HasValue;

        protected override LauncherOperationRequest Build() =>
            new CreateSnapshotRequest(_xml.Path!, _manifest.Path!, _output.Path!);

        internal static string SuggestedName(string? source, string suffix) =>
            (source == null ? "run" : Path.GetFileNameWithoutExtension(source)) + suffix;
    }

    /// <summary>Two persisted snapshots in explicit previous and current roles.</summary>
    public sealed class CompareSnapshotsFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _previous;
        private readonly FileFieldViewModel _current;
        private readonly FileFieldViewModel _output;

        public CompareSnapshotsFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Comparar snapshots",
                "Anterior e atual são papéis declarados por si. A aplicação nunca deduz a ordem por data ou nome.",
                LauncherOperationKind.CompareSnapshots,
                runner,
                engine)
        {
            _previous = Add(FileFieldViewModel.Input(
                "Snapshot anterior", "O run mais antigo desta comparação.", dialogs, PickedFileKind.SnapshotJson));
            _current = Add(FileFieldViewModel.Input(
                "Snapshot atual", "O run mais recente desta comparação.", dialogs, PickedFileKind.SnapshotJson));
            _output = Add(FileFieldViewModel.Destination(
                "Relatório",
                "Onde guardar o relatório da comparação.",
                dialogs,
                PickedFileKind.HtmlReport,
                () => "comparison.html"));
        }

        public override bool CanBuild => _previous.HasValue && _current.HasValue && _output.HasValue;

        protected override LauncherOperationRequest Build() =>
            new CompareSnapshotsRequest(_previous.Path!, _current.Path!, _output.Path!);
    }

    /// <summary>Two exports and their manifests, compared directly without persisting snapshots.</summary>
    public sealed class CompareRunsFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _previousXml;
        private readonly FileFieldViewModel _previousManifest;
        private readonly FileFieldViewModel _currentXml;
        private readonly FileFieldViewModel _currentManifest;
        private readonly FileFieldViewModel _output;

        public CompareRunsFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Comparar duas revisões",
                "Anterior e atual são papéis declarados por si. A aplicação nunca deduz a ordem por data ou nome.",
                LauncherOperationKind.CompareRuns,
                runner,
                engine)
        {
            _previousXml = Add(FileFieldViewModel.Input(
                "Export XML anterior", "O export do run mais antigo.", dialogs, PickedFileKind.ClashXml));
            _previousManifest = Add(FileFieldViewModel.Input(
                "Manifest anterior", "O manifest do run mais antigo.", dialogs, PickedFileKind.RunManifestJson));
            _currentXml = Add(FileFieldViewModel.Input(
                "Export XML atual", "O export do run mais recente.", dialogs, PickedFileKind.ClashXml));
            _currentManifest = Add(FileFieldViewModel.Input(
                "Manifest atual", "O manifest do run mais recente.", dialogs, PickedFileKind.RunManifestJson));
            _output = Add(FileFieldViewModel.Destination(
                "Relatório",
                "Onde guardar o relatório da comparação.",
                dialogs,
                PickedFileKind.HtmlReport,
                () => "comparison.html"));
        }

        public override bool CanBuild =>
            _previousXml.HasValue && _previousManifest.HasValue
            && _currentXml.HasValue && _currentManifest.HasValue
            && _output.HasValue;

        protected override LauncherOperationRequest Build() => new CompareRunsRequest(
            _previousXml.Path!, _previousManifest.Path!, _currentXml.Path!, _currentManifest.Path!, _output.Path!);
    }

    /// <summary>An explicitly ordered run index. The declared order is the only sequence authority.</summary>
    public sealed class IndexSnapshotsFormViewModel : OperationFormViewModel
    {
        private readonly OrderedFileListViewModel _snapshots;
        private readonly FileFieldViewModel _output;

        public IndexSnapshotsFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Criar índice de runs",
                "A ordem que declarar é a ordem do índice. Nada é ordenado por data, nome ou revisão, "
                    + "e um snapshot repetido é mantido tal como o declarou.",
                LauncherOperationKind.IndexSnapshots,
                runner,
                engine)
        {
            _snapshots = Add(new OrderedFileListViewModel(
                "Snapshots, na ordem declarada", dialogs, PickedFileKind.SnapshotJson));
            _output = Add(FileFieldViewModel.Destination(
                "Índice",
                "Onde guardar o índice de runs.",
                dialogs,
                PickedFileKind.RunIndexJson,
                () => "run-index.json"));
        }

        public OrderedFileListViewModel Snapshots => _snapshots;

        public override bool CanBuild => _snapshots.Count > 0 && _output.HasValue;

        protected override LauncherOperationRequest Build() =>
            new IndexSnapshotsRequest(Snapshot(_snapshots), _output.Path!);
    }

    /// <summary>Adjacent-pair traversal of an explicit run index.</summary>
    public sealed class CompareIndexFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _index;
        private readonly FileFieldViewModel _output;

        public CompareIndexFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Comparar índice de runs",
                "Compara apenas pares adjacentes, pela ordem declarada no índice.",
                LauncherOperationKind.CompareIndex,
                runner,
                engine)
        {
            _index = Add(FileFieldViewModel.Input(
                "Índice de runs", "O índice que declara a ordem dos runs.", dialogs, PickedFileKind.RunIndexJson));
            _output = Add(FileFieldViewModel.Destination(
                "Relatório longitudinal",
                "Onde guardar o relatório longitudinal.",
                dialogs,
                PickedFileKind.HtmlReport,
                () => "longitudinal.html"));
        }

        public override bool CanBuild => _index.HasValue && _output.HasValue;

        protected override LauncherOperationRequest Build() =>
            new CompareIndexRequest(_index.Path!, _output.Path!);
    }

    /// <summary>One operational project catalog built from an existing run index.</summary>
    public sealed class CreateProjectFormViewModel : OperationFormViewModel
    {
        private readonly TextFieldViewModel _projectId;
        private readonly TextFieldViewModel _displayName;
        private readonly FileFieldViewModel _index;
        private readonly FileFieldViewModel _report;
        private readonly FileFieldViewModel _output;

        public CreateProjectFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Criar projeto",
                "O catálogo guarda apenas referências operacionais. Os snapshots continuam a ser a única evidência.",
                LauncherOperationKind.CreateProject,
                runner,
                engine)
        {
            _projectId = Add(new TextFieldViewModel(
                "Identificador do projeto", "Estável e usado também na governança de identidade."));
            _displayName = Add(new TextFieldViewModel("Nome", "O nome que aparece no relatório."));
            _index = Add(FileFieldViewModel.Input(
                "Índice de runs", "O índice já existente deste projeto.", dialogs, PickedFileKind.RunIndexJson));
            _report = Add(FileFieldViewModel.Destination(
                "Destino do relatório longitudinal",
                "Onde o relatório será regenerado. A pasta tem de existir.",
                dialogs,
                PickedFileKind.HtmlReport,
                () => "longitudinal.html"));
            _output = Add(FileFieldViewModel.Destination(
                "Ficheiro do projeto",
                "Onde guardar o catálogo do projeto.",
                dialogs,
                PickedFileKind.ProjectCatalogJson,
                () => "project.json"));
        }

        public override bool CanBuild =>
            _projectId.HasValue && _displayName.HasValue && _index.HasValue && _report.HasValue && _output.HasValue;

        protected override LauncherOperationRequest Build() => new CreateProjectRequest(
            _projectId.Trimmed, _displayName.Trimmed, _index.Path!, _report.Path!, _output.Path!);
    }

    /// <summary>
    /// Appends exactly one snapshot to a project's run index. The engine does not regenerate the
    /// report, so the form offers that as an explicit next step rather than doing it silently.
    /// </summary>
    public sealed class AppendProjectSnapshotFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _project;
        private readonly FileFieldViewModel _snapshot;

        public AppendProjectSnapshotFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Acrescentar snapshot ao projeto",
                "Acrescenta uma referência ao fim do índice. Nada é reordenado, removido ou desduplicado, "
                    + "e o relatório não é regenerado automaticamente.",
                LauncherOperationKind.AppendProjectSnapshot,
                runner,
                engine)
        {
            _project = Add(FileFieldViewModel.Input(
                "Projeto", "O catálogo do projeto.", dialogs, PickedFileKind.ProjectCatalogJson));
            _snapshot = Add(FileFieldViewModel.Input(
                "Snapshot a acrescentar", "O run a juntar ao fim do índice.", dialogs, PickedFileKind.SnapshotJson));
        }

        public override bool CanBuild => _project.HasValue && _snapshot.HasValue;

        public override string? FollowUpLabel => "Renderizar projeto agora";

        protected override LauncherOperationRequest Build() =>
            new AppendProjectSnapshotRequest(_project.Path!, _snapshot.Path!);

        protected override Task RunFollowUpAsync() =>
            _project.Path == null
                ? Task.CompletedTask
                : Runner.RunAsync(new RenderProjectRequest(_project.Path));
    }

    /// <summary>Regenerates a project's longitudinal report into the destination the catalog declares.</summary>
    public sealed class RenderProjectFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _project;

        public RenderProjectFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Regenerar relatório do projeto",
                "O destino vem do catálogo existente. A aplicação não escolhe nem inventa esse destino.",
                LauncherOperationKind.RenderProject,
                runner,
                engine)
        {
            _project = Add(FileFieldViewModel.Input(
                "Projeto", "O catálogo do projeto.", dialogs, PickedFileKind.ProjectCatalogJson));
        }

        public override bool CanBuild => _project.HasValue;

        protected override LauncherOperationRequest Build() => new RenderProjectRequest(_project.Path!);
    }
}
