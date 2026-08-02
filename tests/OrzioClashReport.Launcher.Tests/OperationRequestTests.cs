using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Tests;

public sealed class OperationRequestTests
{
    private static string Absolute(string name) =>
        Path.Combine(Path.GetTempPath(), name);

    [Fact]
    public void QuickReportRequest_RequiresAnAbsoluteOutputPath()
    {
        Assert.Throws<ArgumentException>(
            () => new QuickReportRequest("input.xml", "report.html"));
    }

    [Fact]
    public void QuickReportRequest_TrimsAndExposesTypedValues()
    {
        string output = Absolute("report.html");

        var request = new QuickReportRequest("  input.xml  ", output);

        Assert.Equal("input.xml", request.InputXmlPath);
        Assert.Equal(output, request.OutputHtmlPath);
        Assert.Equal(output, request.OutputPath);
        Assert.Equal(LauncherOperationKind.QuickReport, request.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void QuickReportRequest_RejectsEmptyInput(string input)
    {
        Assert.Throws<ArgumentException>(() => new QuickReportRequest(input, Absolute("report.html")));
    }

    [Fact]
    public void IndexSnapshotsRequest_PreservesDeclaredOrderAndDuplicates()
    {
        string[] declared = { "c.json", "a.json", "b.json", "a.json" };

        var request = new IndexSnapshotsRequest(declared, Absolute("run-index.json"));

        Assert.Equal(new[] { "c.json", "a.json", "b.json", "a.json" }, request.SnapshotPaths);
    }

    [Fact]
    public void IndexSnapshotsRequest_CopiesTheCallersList()
    {
        var declared = new List<string> { "a.json", "b.json" };

        var request = new IndexSnapshotsRequest(declared, Absolute("run-index.json"));
        declared[0] = "mutated.json";
        declared.Add("appended.json");

        Assert.Equal(new[] { "a.json", "b.json" }, request.SnapshotPaths);
    }

    [Fact]
    public void IndexSnapshotsRequest_RequiresAtLeastOneSnapshot()
    {
        Assert.Throws<ArgumentException>(
            () => new IndexSnapshotsRequest(Array.Empty<string>(), Absolute("run-index.json")));
    }

    [Fact]
    public void AppendProjectSnapshotRequest_HasNoOutputPath()
    {
        var request = new AppendProjectSnapshotRequest("project.json", "run-004.json");

        Assert.Null(request.OutputPath);
        Assert.Equal(LauncherOperationKind.AppendProjectSnapshot, request.Kind);
    }

    [Fact]
    public void RenderProjectRequest_HasNoOutputPath()
    {
        var request = new RenderProjectRequest("project.json");

        Assert.Null(request.OutputPath);
    }

    [Fact]
    public void ValidateIdentityGovernanceRequest_HasNoOutputPath()
    {
        var request = new ValidateIdentityGovernanceRequest("project.json", "governance.json");

        Assert.Null(request.OutputPath);
    }

    [Fact]
    public void EveryOperationKind_IsCoveredByExactlyOneRequestType()
    {
        LauncherOperationRequest[] requests =
        {
            new QuickReportRequest("in.xml", Absolute("out.html")),
            new CreateSnapshotRequest("in.xml", "manifest.json", Absolute("snapshot.json")),
            new CompareRunsRequest("p.xml", "p.json", "c.xml", "c.json", Absolute("out.html")),
            new CompareSnapshotsRequest("p.json", "c.json", Absolute("out.html")),
            new IndexSnapshotsRequest(new[] { "a.json" }, Absolute("index.json")),
            new CompareIndexRequest("index.json", Absolute("out.html")),
            new CreateProjectRequest("id", "name", "index.json", "report.html", Absolute("project.json")),
            new AppendProjectSnapshotRequest("project.json", "snapshot.json"),
            new RenderProjectRequest("project.json"),
            new CreateIdentityGovernanceRequest("id", Absolute("governance.json")),
            AppendIdentityDecisionRequest.Reject(
                "governance.json",
                "d1",
                new IdentityEvidenceEndpoint("run-1", 0),
                new IdentityEvidenceEndpoint("run-2", 1),
                "alias"),
            new ValidateIdentityGovernanceRequest("project.json", "governance.json"),
            new RenderIdentityGovernanceReportRequest("project.json", "governance.json", Absolute("review.html")),
        };

        LauncherOperationKind[] covered = requests.Select(request => request.Kind).ToArray();

        Assert.Equal(covered.Length, covered.Distinct().Count());
        Assert.Equal(
            Enum.GetValues<LauncherOperationKind>().OrderBy(kind => kind).ToArray(),
            covered.OrderBy(kind => kind).ToArray());
    }
}
