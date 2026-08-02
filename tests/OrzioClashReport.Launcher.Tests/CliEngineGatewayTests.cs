using System.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Infrastructure.Engine;
using OrzioClashReport.Launcher.Infrastructure.Processes;

namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// The gateway driven against a real child process, through exactly the argument vectors it builds.
/// </summary>
public sealed class CliEngineGatewayTests : IDisposable
{
    private readonly string _workspace;

    public CliEngineGatewayTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), "orzio-gateway-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspace);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspace))
        {
            Directory.Delete(_workspace, recursive: true);
        }
    }

    private CliEngineGateway Gateway =>
        new(FakeEngine.ExecutablePath, new EngineArgumentBuilder(), new ProcessJobRunner());

    private string Input(string stem)
    {
        string path = Path.Combine(_workspace, stem + ".xml");
        File.WriteAllText(path, "<exchange />");
        return path;
    }

    private string Output(string name) => Path.Combine(_workspace, name);

    [Fact]
    public async Task Success_ProducesTheReportAndReportsIt()
    {
        string output = Output("report.html");

        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("success"), output), progress: null, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(output));

        LauncherArtifact artifact = Assert.Single(result.Artifacts);
        Assert.Equal(ArtifactKind.HtmlReport, artifact.Kind);
        Assert.Equal(output, artifact.Path);
        Assert.True(artifact.SizeInBytes > 0);
    }

    [Fact]
    public async Task SuccessWithoutAnOutputFile_IsReportedAsOutputMissing()
    {
        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("no-output"), Output("report.html")),
            progress: null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(LauncherErrorCode.OutputMissing, result.Error!.Code);
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Artifacts);
    }

    [Fact]
    public async Task AnEmptyOutputFile_IsReportedAsOutputMissing()
    {
        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("empty-output"), Output("report.html")),
            progress: null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(LauncherErrorCode.OutputMissing, result.Error!.Code);
    }

    [Fact]
    public async Task ExitOne_IsReportedAsAnEngineExecutionFailureWithTheEnginesOwnMessage()
    {
        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("fail"), Output("report.html")),
            progress: null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(LauncherErrorCode.EngineExecutionFailure, result.Error!.Code);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("could not be parsed", result.Error.Detail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnexpectedExitCode_UsesTheSameUnclassifiedFailure()
    {
        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("unexpected-exit"), Output("report.html")),
            progress: null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(LauncherErrorCode.EngineExecutionFailure, result.Error!.Code);
        Assert.Equal(42, result.ExitCode);
    }

    [Fact]
    public async Task Cancellation_StopsTheEngineAndReportsCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        string output = Output("report.html");

        Task<OperationResult> running = Gateway.ExecuteAsync(
            new QuickReportRequest(Input("slow"), output),
            new Progress<EngineOutputChunk>(_ => cancellation.Cancel()),
            cancellation.Token);

        var stopwatch = Stopwatch.StartNew();
        OperationResult result = await running;
        stopwatch.Stop();

        Assert.False(result.Succeeded);
        Assert.Equal(LauncherErrorCode.Canceled, result.Error!.Code);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMinutes(1), "Cancellation must not wait for the engine to finish.");

        // The engine is really gone: it never got to write the file it would have written.
        Assert.False(File.Exists(output));
    }

    [Fact]
    public async Task HugeStandardOutput_IsCapturedButBounded()
    {
        string output = Output("report.html");

        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("huge-stdout"), output), progress: null, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.StandardOutput.Length <= (64 * 1024) + 64);
        Assert.Contains(result.Warnings, warning => warning.Code == LauncherWarningCode.OutputTruncated);
    }

    [Fact]
    public async Task HugeStandardError_IsCapturedButBoundedAndStillSucceeds()
    {
        string output = Output("report.html");

        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("huge-stderr"), output), progress: null, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.StandardError.Length <= (64 * 1024) + 64);
        Assert.Contains(result.Warnings, warning => warning.Code == LauncherWarningCode.EngineWroteToStandardError);
    }

    [Fact]
    public async Task InvalidEncodingOnAStream_NeverFailsTheOperation()
    {
        string output = Output("report.html");

        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("bad-encoding"), output), progress: null, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(output));
    }

    [Fact]
    public async Task Progress_ReportsEachLineAsItArrives()
    {
        var lines = new List<EngineOutputChunk>();

        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("success"), Output("report.html")),
            new Progress<EngineOutputChunk>(lines.Add),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains(lines, chunk => chunk.Text.Contains("raw clashes", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AMissingEngine_IsReportedAsAStartFailure()
    {
        var gateway = new CliEngineGateway(
            Path.Combine(_workspace, "does-not-exist"), new EngineArgumentBuilder(), new ProcessJobRunner());

        OperationResult result = await gateway.ExecuteAsync(
            new QuickReportRequest(Input("success"), Output("report.html")),
            progress: null,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(LauncherErrorCode.EngineStartFailure, result.Error!.Code);
    }

    [Fact]
    public void TheWorkingDirectory_IsTheOutputDirectoryAndNeverTheInstallation()
    {
        string output = Output("report.html");

        string workingDirectory = CliEngineGateway.WorkingDirectoryFor(
            new QuickReportRequest(Input("success"), output));

        Assert.Equal(_workspace, workingDirectory);
        Assert.NotEqual(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), workingDirectory);
    }

    [Fact]
    public void OperationsWithoutAnOutputOption_RunFromTheirProjectDirectory()
    {
        string project = Path.Combine(_workspace, "project.json");
        File.WriteAllText(project, "{}");

        Assert.Equal(
            _workspace,
            CliEngineGateway.WorkingDirectoryFor(new RenderProjectRequest(project)));
        Assert.Equal(
            _workspace,
            CliEngineGateway.WorkingDirectoryFor(new AppendProjectSnapshotRequest(project, "run.json")));
        Assert.Equal(
            _workspace,
            CliEngineGateway.WorkingDirectoryFor(
                new ValidateIdentityGovernanceRequest(project, "governance.json")));
    }

    [Fact]
    public async Task AMissingDestinationDirectory_IsRefusedBeforeTheEngineStarts()
    {
        string output = Path.Combine(_workspace, "missing", "report.html");

        OperationResult result = await Gateway.ExecuteAsync(
            new QuickReportRequest(Input("success"), output), progress: null, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(LauncherErrorCode.InvalidRequest, result.Error!.Code);
        Assert.Null(result.ExitCode);
    }

    [Fact]
    public async Task TheTimeout_TerminatesAnEngineThatNeverFinishes()
    {
        var specification = new ProcessRunSpecification(
            FakeEngine.ExecutablePath,
            new[] { Input("slow"), "-o", Output("report.html") },
            _workspace,
            TimeSpan.FromSeconds(2));

        var stopwatch = Stopwatch.StartNew();
        ProcessRunResult result = await new ProcessJobRunner()
            .RunAsync(specification, progress: null, CancellationToken.None);
        stopwatch.Stop();

        Assert.Equal(ProcessRunOutcome.TimedOut, result.Outcome);
        Assert.Null(result.ExitCode);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(60));
    }
}
