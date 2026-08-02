using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Tests;

public sealed class AdvancedOperationFormTests
{
    private static string Absolute(string name) => Path.Combine(Path.GetTempPath(), name);

    private sealed class Harness
    {
        internal Harness()
        {
            Gateway = new StubEngineGateway(OperationResult.Success(
                exitCode: 0,
                artifacts: null,
                warnings: null,
                standardOutput: "done",
                standardError: string.Empty,
                duration: TimeSpan.FromSeconds(1)));

            Dialogs = new StubFileDialogs();

            Jobs = new OperationJobService(
                Gateway, new RecordingJournal(), new InMemoryRecentItemsStore(), new StubFileSystem());

            Engine = new EngineStatusViewModel(new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3")));
            Engine.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        internal StubEngineGateway Gateway { get; }

        internal StubFileDialogs Dialogs { get; }

        internal OperationJobService Jobs { get; }

        internal EngineStatusViewModel Engine { get; }

        internal OperationRunnerViewModel Runner() => new(
            Jobs, new StubCollisionPrompt(OutputCollisionResolution.Cancel), new NoOpOutputRevealer());
    }

    private static void Fill(OperationFormViewModel form, params string[] fileNames)
    {
        int index = 0;
        foreach (ViewModelBase field in form.Fields)
        {
            if (field is FileFieldViewModel file)
            {
                file.Path = Absolute(fileNames[index++]);
            }
        }
    }

    [Fact]
    public void EveryFormIsUnrunnable_UntilItsFieldsAreFilled()
    {
        var harness = new Harness();

        OperationFormViewModel[] forms =
        {
            new CreateSnapshotFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs),
            new CompareSnapshotsFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs),
            new CompareRunsFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs),
            new IndexSnapshotsFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs),
            new CompareIndexFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs),
            new CreateProjectFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs),
            new AppendProjectSnapshotFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs),
            new RenderProjectFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs),
        };

        foreach (OperationFormViewModel form in forms)
        {
            Assert.False(form.CanBuild, $"{form.Title} should not be runnable while empty.");
            Assert.False(form.RunCommand.CanExecute(null));
            Assert.NotEmpty(form.Fields);
        }
    }

    [Fact]
    public async Task CreateSnapshot_SendsATypedRequest()
    {
        var harness = new Harness();
        var form = new CreateSnapshotFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);

        Fill(form, "run.xml", "run-manifest.json", "run-001.json");
        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<CreateSnapshotRequest>(harness.Gateway.LastRequest);
        Assert.Equal(Absolute("run.xml"), request.XmlPath);
        Assert.Equal(Absolute("run-manifest.json"), request.ManifestPath);
        Assert.Equal(Absolute("run-001.json"), request.OutputSnapshotPath);
    }

    [Fact]
    public async Task CompareSnapshots_KeepsPreviousAndCurrentAsDeclaredRoles()
    {
        var harness = new Harness();
        var form = new CompareSnapshotsFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);

        // Deliberately "out of order" by name: the roles are the human's declaration, not a sort.
        Fill(form, "run-009.json", "run-002.json", "comparison.html");
        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<CompareSnapshotsRequest>(harness.Gateway.LastRequest);
        Assert.Equal(Absolute("run-009.json"), request.PreviousSnapshotPath);
        Assert.Equal(Absolute("run-002.json"), request.CurrentSnapshotPath);
    }

    [Fact]
    public async Task CompareRuns_SendsBothExportsAndBothManifests()
    {
        var harness = new Harness();
        var form = new CompareRunsFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);

        Fill(form, "previous.xml", "previous.json", "current.xml", "current.json", "comparison.html");
        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<CompareRunsRequest>(harness.Gateway.LastRequest);
        Assert.Equal(Absolute("previous.xml"), request.PreviousXmlPath);
        Assert.Equal(Absolute("previous.json"), request.PreviousManifestPath);
        Assert.Equal(Absolute("current.xml"), request.CurrentXmlPath);
        Assert.Equal(Absolute("current.json"), request.CurrentManifestPath);
    }

    [Fact]
    public async Task IndexSnapshots_SendsTheDeclaredOrderExactly()
    {
        var harness = new Harness();
        var form = new IndexSnapshotsFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);

        form.Snapshots.Append(Absolute("z.json"), Absolute("a.json"), Absolute("m.json"));
        Fill(form, "run-index.json");

        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<IndexSnapshotsRequest>(harness.Gateway.LastRequest);
        Assert.Equal(
            new[] { Absolute("z.json"), Absolute("a.json"), Absolute("m.json") },
            request.SnapshotPaths);
    }

    [Fact]
    public void TheOrderedList_NeverSortsByNameDateOrRevision()
    {
        var harness = new Harness();
        var list = new OrderedFileListViewModel("Snapshots", harness.Dialogs, PickedFileKind.SnapshotJson);

        list.Append(
            Absolute("run-2026-01-09.json"),
            Absolute("run-2024-03-01.json"),
            Absolute("aaa-first-alphabetically.json"),
            Absolute("run-2025-06-15.json"));

        Assert.Equal(
            new[]
            {
                Absolute("run-2026-01-09.json"),
                Absolute("run-2024-03-01.json"),
                Absolute("aaa-first-alphabetically.json"),
                Absolute("run-2025-06-15.json"),
            },
            list.Paths);
    }

    [Fact]
    public void TheOrderedList_WarnsAboutDuplicatesButKeepsThem()
    {
        var harness = new Harness();
        var list = new OrderedFileListViewModel("Snapshots", harness.Dialogs, PickedFileKind.SnapshotJson);

        list.Append(Absolute("a.json"), Absolute("b.json"), Absolute("a.json"));

        Assert.Equal(3, list.Count);
        Assert.True(list.HasDuplicates);
        Assert.False(list.Files[0].IsDuplicate);
        Assert.False(list.Files[1].IsDuplicate);
        Assert.True(list.Files[2].IsDuplicate);
        Assert.Equal(
            new[] { Absolute("a.json"), Absolute("b.json"), Absolute("a.json") },
            list.Paths);
    }

    [Fact]
    public void TheOrderedList_ReordersOnlyWhenAHumanMovesAnEntry()
    {
        var harness = new Harness();
        var list = new OrderedFileListViewModel("Snapshots", harness.Dialogs, PickedFileKind.SnapshotJson);
        list.Append(Absolute("a.json"), Absolute("b.json"), Absolute("c.json"));

        list.Selected = list.Files[2];
        list.MoveUpCommand.Execute(null);

        Assert.Equal(
            new[] { Absolute("a.json"), Absolute("c.json"), Absolute("b.json") },
            list.Paths);
        Assert.Equal(2, list.Files[1].Position);
    }

    [Fact]
    public async Task CreateProject_SendsTheTypedMetadataAndReferences()
    {
        var harness = new Harness();
        var form = new CreateProjectFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);

        ((TextFieldViewModel)form.Fields[0]).Value = "  tower-a  ";
        ((TextFieldViewModel)form.Fields[1]).Value = "Tower A";
        Fill(form, "run-index.json", "longitudinal.html", "project.json");

        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<CreateProjectRequest>(harness.Gateway.LastRequest);
        Assert.Equal("tower-a", request.ProjectId);
        Assert.Equal("Tower A", request.DisplayName);
        Assert.Equal(Absolute("run-index.json"), request.IndexPath);
        Assert.Equal(Absolute("longitudinal.html"), request.ReportPath);
        Assert.Equal(Absolute("project.json"), request.OutputProjectPath);
    }

    [Fact]
    public async Task AppendProjectSnapshot_OffersRenderingOnlyAfterItSucceeds()
    {
        var harness = new Harness();
        var form = new AppendProjectSnapshotFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);

        Assert.Equal("Renderizar projeto agora", form.FollowUpLabel);
        Assert.False(form.ShowsFollowUp);

        Fill(form, "project.json", "run-004.json");
        await form.RunCommand.ExecuteAsync(null);

        Assert.IsType<AppendProjectSnapshotRequest>(harness.Gateway.LastRequest);
        Assert.True(form.ShowsFollowUp);

        await form.FollowUpCommand.ExecuteAsync(null);

        var render = Assert.IsType<RenderProjectRequest>(harness.Gateway.LastRequest);
        Assert.Equal(Absolute("project.json"), render.ProjectPath);
    }

    [Fact]
    public async Task RenderProject_TakesItsDestinationFromTheCatalogOnly()
    {
        var harness = new Harness();
        var form = new RenderProjectFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);

        Fill(form, "project.json");
        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<RenderProjectRequest>(harness.Gateway.LastRequest);
        Assert.Null(request.OutputPath);
    }

    [Fact]
    public void LongitudinalFormsAreMarkedExperimental()
    {
        var harness = new Harness();

        Assert.True(new CompareRunsFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs).IsExperimental);
        Assert.True(new CompareIndexFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs).IsExperimental);
        Assert.True(new RenderProjectFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs).IsExperimental);
        Assert.False(new CreateSnapshotFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs).IsExperimental);
    }

    [Fact]
    public void ASectionShowsItsFirstFormAndOffersTheOthers()
    {
        var harness = new Harness();
        var first = new CreateSnapshotFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);
        var second = new CompareSnapshotsFormViewModel(harness.Runner(), harness.Engine, harness.Dialogs);

        var section = new OperationSectionViewModel("Snapshots", "…", new OperationFormViewModel[] { first, second });

        Assert.Same(first, section.SelectedForm);
        Assert.Equal(2, section.Forms.Count);

        section.SelectedForm = second;
        Assert.Same(second, section.SelectedForm);
    }
}
