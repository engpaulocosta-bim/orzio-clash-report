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
                "Form.CreateSnapshot.Title",
                "Form.CreateSnapshot.Description",
                LauncherOperationKind.CreateSnapshot,
                runner,
                engine)
        {
            _xml = Add(FileFieldViewModel.Input(
                "Field.Xml.Label", "Field.Xml.Hint", dialogs, PickedFileKind.ClashXml));
            _manifest = Add(FileFieldViewModel.Input(
                "Field.Manifest.Label", "Field.Manifest.Hint", dialogs, PickedFileKind.RunManifestJson));
            _output = Add(FileFieldViewModel.Destination(
                "Field.Snapshot.Label",
                "Field.Snapshot.Hint",
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
                "Form.CompareSnapshots.Title",
                "Form.ExplicitRoles",
                LauncherOperationKind.CompareSnapshots,
                runner,
                engine)
        {
            _previous = Add(FileFieldViewModel.Input(
                "Field.PreviousSnapshot.Label",
                "Field.PreviousSnapshot.Hint",
                dialogs,
                PickedFileKind.SnapshotJson));
            _current = Add(FileFieldViewModel.Input(
                "Field.CurrentSnapshot.Label",
                "Field.CurrentSnapshot.Hint",
                dialogs,
                PickedFileKind.SnapshotJson));
            _output = Add(FileFieldViewModel.Destination(
                "Field.Report.Label",
                "Field.Report.Hint",
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
                "Form.CompareRuns.Title",
                "Form.ExplicitRoles",
                LauncherOperationKind.CompareRuns,
                runner,
                engine)
        {
            _previousXml = Add(FileFieldViewModel.Input(
                "Field.PreviousXml.Label", "Field.PreviousXml.Hint", dialogs, PickedFileKind.ClashXml));
            _previousManifest = Add(FileFieldViewModel.Input(
                "Field.PreviousManifest.Label",
                "Field.PreviousManifest.Hint",
                dialogs,
                PickedFileKind.RunManifestJson));
            _currentXml = Add(FileFieldViewModel.Input(
                "Field.CurrentXml.Label", "Field.CurrentXml.Hint", dialogs, PickedFileKind.ClashXml));
            _currentManifest = Add(FileFieldViewModel.Input(
                "Field.CurrentManifest.Label",
                "Field.CurrentManifest.Hint",
                dialogs,
                PickedFileKind.RunManifestJson));
            _output = Add(FileFieldViewModel.Destination(
                "Field.Report.Label",
                "Field.Report.Hint",
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
                "Form.IndexSnapshots.Title",
                "Form.IndexSnapshots.Description",
                LauncherOperationKind.IndexSnapshots,
                runner,
                engine)
        {
            _snapshots = Add(new OrderedFileListViewModel(
                "Field.OrderedSnapshots.Label", dialogs, PickedFileKind.SnapshotJson));
            _output = Add(FileFieldViewModel.Destination(
                "Field.IndexDestination.Label",
                "Field.IndexDestination.Hint",
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
                "Form.CompareIndex.Title",
                "Form.CompareIndex.Description",
                LauncherOperationKind.CompareIndex,
                runner,
                engine)
        {
            _index = Add(FileFieldViewModel.Input(
                "Field.Index.Label", "Field.Index.Hint", dialogs, PickedFileKind.RunIndexJson));
            _output = Add(FileFieldViewModel.Destination(
                "Field.LongitudinalReport.Label",
                "Field.LongitudinalReport.Hint",
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
                "Form.CreateProject.Title",
                "Form.CreateProject.Description",
                LauncherOperationKind.CreateProject,
                runner,
                engine)
        {
            _projectId = Add(new TextFieldViewModel("Field.ProjectId.Label", "Field.ProjectId.Hint"));
            _displayName = Add(new TextFieldViewModel("Field.DisplayName.Label", "Field.DisplayName.Hint"));
            _index = Add(FileFieldViewModel.Input(
                "Field.Index.Label", "Field.ExistingIndex.Hint", dialogs, PickedFileKind.RunIndexJson));
            _report = Add(FileFieldViewModel.Destination(
                "Field.ReportDestination.Label",
                "Field.ReportDestination.Hint",
                dialogs,
                PickedFileKind.HtmlReport,
                () => "longitudinal.html"));
            _output = Add(FileFieldViewModel.Destination(
                "Field.ProjectFile.Label",
                "Field.ProjectFile.Hint",
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
                "Form.AppendProjectSnapshot.Title",
                "Form.AppendProjectSnapshot.Description",
                LauncherOperationKind.AppendProjectSnapshot,
                runner,
                engine)
        {
            _project = Add(FileFieldViewModel.Input(
                "Field.Project.Label", "Field.Project.Hint", dialogs, PickedFileKind.ProjectCatalogJson));
            _snapshot = Add(FileFieldViewModel.Input(
                "Field.AppendedSnapshot.Label",
                "Field.AppendedSnapshot.Hint",
                dialogs,
                PickedFileKind.SnapshotJson));
        }

        public override bool CanBuild => _project.HasValue && _snapshot.HasValue;

        protected override string? FollowUpLabelKey => "Form.AppendProjectSnapshot.FollowUp";

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
                "Form.RenderProject.Title",
                "Form.RenderProject.Description",
                LauncherOperationKind.RenderProject,
                runner,
                engine)
        {
            _project = Add(FileFieldViewModel.Input(
                "Field.Project.Label", "Field.Project.Hint", dialogs, PickedFileKind.ProjectCatalogJson));
        }

        public override bool CanBuild => _project.HasValue;

        protected override LauncherOperationRequest Build() => new RenderProjectRequest(_project.Path!);
    }
}
