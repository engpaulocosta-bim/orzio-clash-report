using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Infrastructure.Engine;

namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// One argument vector per operation, compared element by element against the engine's published
/// contracts. Nothing here may be relaxed without the engine's contract changing first.
/// </summary>
public sealed class EngineArgumentBuilderTests
{
    private static readonly IEngineArgumentBuilder Builder = new EngineArgumentBuilder();

    private static string Absolute(string name) => Path.Combine(Path.GetTempPath(), name);

    [Fact]
    public void QuickReport_PassesTheXmlPositionallyWithNoSubcommand()
    {
        string output = Absolute("report.html");

        IReadOnlyList<string> arguments = Builder.Build(new QuickReportRequest("input.xml", output));

        Assert.Equal(new[] { "input.xml", "-o", output }, arguments);
    }

    [Fact]
    public void CreateSnapshot()
    {
        string output = Absolute("run-001.json");

        IReadOnlyList<string> arguments = Builder.Build(
            new CreateSnapshotRequest("input.xml", "manifest.json", output));

        Assert.Equal(
            new[] { "snapshot", "--xml", "input.xml", "--manifest", "manifest.json", "-o", output },
            arguments);
    }

    [Fact]
    public void CompareRuns()
    {
        string output = Absolute("comparison.html");

        IReadOnlyList<string> arguments = Builder.Build(new CompareRunsRequest(
            "previous.xml", "previous.json", "current.xml", "current.json", output));

        Assert.Equal(
            new[]
            {
                "compare",
                "--previous-xml", "previous.xml",
                "--previous-manifest", "previous.json",
                "--current-xml", "current.xml",
                "--current-manifest", "current.json",
                "-o", output,
            },
            arguments);
    }

    [Fact]
    public void CompareSnapshots()
    {
        string output = Absolute("comparison.html");

        IReadOnlyList<string> arguments = Builder.Build(
            new CompareSnapshotsRequest("previous.json", "current.json", output));

        Assert.Equal(
            new[]
            {
                "compare-snapshots",
                "--previous-snapshot", "previous.json",
                "--current-snapshot", "current.json",
                "-o", output,
            },
            arguments);
    }

    [Fact]
    public void IndexSnapshots_RepeatsTheSnapshotOptionInDeclaredOrder()
    {
        string output = Absolute("run-index.json");

        IReadOnlyList<string> arguments = Builder.Build(new IndexSnapshotsRequest(
            new[] { "run-003.json", "run-001.json", "run-002.json" }, output));

        Assert.Equal(
            new[]
            {
                "index-snapshots",
                "--snapshot", "run-003.json",
                "--snapshot", "run-001.json",
                "--snapshot", "run-002.json",
                "-o", output,
            },
            arguments);
    }

    [Fact]
    public void IndexSnapshots_NeverReordersAndNeverDropsDuplicates()
    {
        string output = Absolute("run-index.json");

        // Names that would sort differently, plus a repeated reference. The declared order is the
        // only sequence authority, and a duplicate is a legitimate declaration.
        IReadOnlyList<string> arguments = Builder.Build(new IndexSnapshotsRequest(
            new[] { "z-run.json", "a-run.json", "z-run.json", "m-run.json" }, output));

        Assert.Equal(
            new[]
            {
                "index-snapshots",
                "--snapshot", "z-run.json",
                "--snapshot", "a-run.json",
                "--snapshot", "z-run.json",
                "--snapshot", "m-run.json",
                "-o", output,
            },
            arguments);
    }

    [Fact]
    public void CompareIndex()
    {
        string output = Absolute("longitudinal.html");

        IReadOnlyList<string> arguments = Builder.Build(new CompareIndexRequest("run-index.json", output));

        Assert.Equal(new[] { "compare-index", "--index", "run-index.json", "-o", output }, arguments);
    }

    [Fact]
    public void CreateProject()
    {
        string output = Absolute("project.json");

        IReadOnlyList<string> arguments = Builder.Build(new CreateProjectRequest(
            "tower-a", "Tower A", "run-index.json", "longitudinal.html", output));

        Assert.Equal(
            new[]
            {
                "create-project",
                "--project-id", "tower-a",
                "--name", "Tower A",
                "--index", "run-index.json",
                "--report", "longitudinal.html",
                "-o", output,
            },
            arguments);
    }

    [Fact]
    public void AppendProjectSnapshot_HasNoOutputOption()
    {
        IReadOnlyList<string> arguments = Builder.Build(
            new AppendProjectSnapshotRequest("project.json", "run-004.json"));

        Assert.Equal(
            new[] { "append-project-snapshot", "--project", "project.json", "--snapshot", "run-004.json" },
            arguments);
        Assert.DoesNotContain("-o", arguments);
    }

    [Fact]
    public void RenderProject_HasNoOutputOption()
    {
        IReadOnlyList<string> arguments = Builder.Build(new RenderProjectRequest("project.json"));

        Assert.Equal(new[] { "render-project", "--project", "project.json" }, arguments);
        Assert.DoesNotContain("-o", arguments);
    }

    [Fact]
    public void CreateIdentityGovernance()
    {
        string output = Absolute("identity-governance.json");

        IReadOnlyList<string> arguments = Builder.Build(
            new CreateIdentityGovernanceRequest("tower-a", output));

        Assert.Equal(
            new[] { "create-identity-governance", "--project-id", "tower-a", "-o", output },
            arguments);
    }

    [Fact]
    public void AppendIdentityDecision_Confirmation_CarriesThePersistentIdentityId()
    {
        IReadOnlyList<string> arguments = Builder.Build(AppendIdentityDecisionRequest.Confirm(
            "identity-governance.json",
            "decision-1",
            new IdentityEvidenceEndpoint("run-001", 0),
            new IdentityEvidenceEndpoint("run-002", 7),
            "reviewer-a",
            "identity-1"));

        Assert.Equal(
            new[]
            {
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-1",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "0",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "7",
                "--reviewer-alias", "reviewer-a",
                "--persistent-identity-id", "identity-1",
            },
            arguments);
    }

    [Fact]
    public void AppendIdentityDecision_Rejection_NeverCarriesAPersistentIdentityId()
    {
        IReadOnlyList<string> arguments = Builder.Build(AppendIdentityDecisionRequest.Reject(
            "identity-governance.json",
            "decision-2",
            new IdentityEvidenceEndpoint("run-001", 3),
            new IdentityEvidenceEndpoint("run-002", 4),
            "reviewer-a"));

        Assert.Equal(
            new[]
            {
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-2",
                "--decision-kind", "RejectSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "3",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "4",
                "--reviewer-alias", "reviewer-a",
            },
            arguments);
        Assert.DoesNotContain("--persistent-identity-id", arguments);
    }

    [Fact]
    public void AppendIdentityDecision_AppendsTheReasonLastWhenSupplied()
    {
        IReadOnlyList<string> arguments = Builder.Build(AppendIdentityDecisionRequest.Reject(
            "identity-governance.json",
            "decision-3",
            new IdentityEvidenceEndpoint("run-001", 0),
            new IdentityEvidenceEndpoint("run-002", 1),
            "reviewer-a",
            "different duct run"));

        Assert.Equal("--reason", arguments[arguments.Count - 2]);
        Assert.Equal("different duct run", arguments[arguments.Count - 1]);
    }

    [Fact]
    public void ValidateIdentityGovernance_HasNoOutputOption()
    {
        IReadOnlyList<string> arguments = Builder.Build(
            new ValidateIdentityGovernanceRequest("project.json", "identity-governance.json"));

        Assert.Equal(
            new[]
            {
                "validate-identity-governance",
                "--project", "project.json",
                "--governance", "identity-governance.json",
            },
            arguments);
        Assert.DoesNotContain("-o", arguments);
    }

    [Fact]
    public void RenderIdentityGovernanceReport()
    {
        string output = Absolute("identity-governance.html");

        IReadOnlyList<string> arguments = Builder.Build(new RenderIdentityGovernanceReportRequest(
            "project.json", "identity-governance.json", output));

        Assert.Equal(
            new[]
            {
                "render-identity-governance-report",
                "--project", "project.json",
                "--governance", "identity-governance.json",
                "-o", output,
            },
            arguments);
    }

    [Fact]
    public void EveryOperationWithAnOutputOption_PassesAnAbsoluteDestination()
    {
        foreach (LauncherOperationRequest request in AllRequests())
        {
            IReadOnlyList<string> arguments = Builder.Build(request);
            int index = arguments.ToList().IndexOf("-o");

            if (request.OutputPath == null)
            {
                Assert.Equal(-1, index);
                continue;
            }

            Assert.True(index >= 0, $"{request.Kind} should pass -o.");
            Assert.True(Path.IsPathRooted(arguments[index + 1]), $"{request.Kind} must pass an absolute -o.");
        }
    }

    [Fact]
    public void NoArgumentVector_ContainsAShellOrRedirection()
    {
        foreach (LauncherOperationRequest request in AllRequests())
        {
            foreach (string argument in Builder.Build(request))
            {
                Assert.DoesNotContain("cmd.exe", argument, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("powershell", argument, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("&&", argument, StringComparison.Ordinal);
                Assert.DoesNotContain("|", argument, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TheCanonicalDecisionKinds_AreNeverTranslated()
    {
        Assert.Equal(
            "ConfirmSameIdentity",
            EngineArgumentBuilder.CanonicalDecisionKind(HumanIdentityDecisionKind.ConfirmSameIdentity));
        Assert.Equal(
            "RejectSameIdentity",
            EngineArgumentBuilder.CanonicalDecisionKind(HumanIdentityDecisionKind.RejectSameIdentity));
    }

    internal static IEnumerable<LauncherOperationRequest> AllRequests()
    {
        yield return new QuickReportRequest("in.xml", Absolute("out.html"));
        yield return new CreateSnapshotRequest("in.xml", "manifest.json", Absolute("snapshot.json"));
        yield return new CompareRunsRequest("p.xml", "p.json", "c.xml", "c.json", Absolute("out.html"));
        yield return new CompareSnapshotsRequest("p.json", "c.json", Absolute("out.html"));
        yield return new IndexSnapshotsRequest(new[] { "a.json", "b.json" }, Absolute("index.json"));
        yield return new CompareIndexRequest("index.json", Absolute("out.html"));
        yield return new CreateProjectRequest("id", "name", "index.json", "report.html", Absolute("project.json"));
        yield return new AppendProjectSnapshotRequest("project.json", "snapshot.json");
        yield return new RenderProjectRequest("project.json");
        yield return new CreateIdentityGovernanceRequest("id", Absolute("governance.json"));
        yield return AppendIdentityDecisionRequest.Confirm(
            "governance.json",
            "d1",
            new IdentityEvidenceEndpoint("run-1", 0),
            new IdentityEvidenceEndpoint("run-2", 1),
            "alias",
            "identity-1");
        yield return new ValidateIdentityGovernanceRequest("project.json", "governance.json");
        yield return new RenderIdentityGovernanceReportRequest(
            "project.json", "governance.json", Absolute("review.html"));
    }
}
