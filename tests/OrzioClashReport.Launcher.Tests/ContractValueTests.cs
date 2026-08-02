using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Tests;

public sealed class ContractValueTests
{
    [Fact]
    public void EngineProbeResult_ReadyIsTheOnlyUsableStatus()
    {
        Assert.True(EngineProbeResult.Ready("0.1.0-preview.3").IsUsable);
        Assert.False(EngineProbeResult.VersionMismatch("0.1.0-preview.2", "0.1.0-preview.3").IsUsable);
        Assert.False(EngineProbeResult.Missing("not found").IsUsable);
        Assert.False(EngineProbeResult.IntegrityFailure("hash mismatch").IsUsable);
        Assert.False(EngineProbeResult.Unsupported("unreadable version line").IsUsable);
    }

    [Fact]
    public void EngineIntegrityResult_NormalizesDigestsToLowercaseHex()
    {
        EngineIntegrityResult result = EngineIntegrityResult.HashMismatch("ABCD", "abce");

        Assert.Equal("abcd", result.ExpectedSha256);
        Assert.Equal("abce", result.ActualSha256);
        Assert.False(result.IsVerified);
    }

    [Fact]
    public void OperationResult_FailureCarriesNoArtifacts()
    {
        OperationResult result = OperationResult.Failure(
            new LauncherError(LauncherErrorCode.EngineExecutionFailure, "The engine reported a failure."),
            exitCode: 1,
            warnings: null,
            standardOutput: string.Empty,
            standardError: "boom",
            duration: TimeSpan.FromSeconds(1));

        Assert.False(result.Succeeded);
        Assert.Empty(result.Artifacts);
        Assert.NotNull(result.Error);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void OperationResult_SuccessFreezesArtifactList()
    {
        var artifacts = new List<LauncherArtifact>
        {
            new(ArtifactKind.HtmlReport, "/tmp/report.html", 128),
        };

        OperationResult result = OperationResult.Success(
            exitCode: 0,
            artifacts,
            warnings: null,
            standardOutput: "done",
            standardError: string.Empty,
            duration: TimeSpan.FromSeconds(2));

        artifacts.Add(new LauncherArtifact(ArtifactKind.SnapshotJson, "/tmp/snapshot.json", 8));

        Assert.Single(result.Artifacts);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void JobId_RoundTripsAndRejectsUnsafeCharacters()
    {
        JobId generated = JobId.New();

        Assert.False(generated.IsEmpty);
        Assert.Equal(generated, JobId.Parse(generated.Value));
        Assert.Throws<ArgumentException>(() => JobId.Parse("../escape"));
        Assert.Throws<ArgumentException>(() => JobId.Parse("with space"));
        Assert.Throws<ArgumentException>(() => JobId.Parse(" "));
    }

    [Fact]
    public void JobSnapshot_KnowsWhichStatesAreTerminal()
    {
        Assert.False(Snapshot(JobState.Pending).IsTerminal);
        Assert.False(Snapshot(JobState.Running).IsTerminal);
        Assert.True(Snapshot(JobState.Succeeded).IsTerminal);
        Assert.True(Snapshot(JobState.Failed).IsTerminal);
        Assert.True(Snapshot(JobState.Canceled).IsTerminal);
    }

    [Fact]
    public void DiagnosticBundle_AllowsExactlySixNamedFiles()
    {
        string[] fileNames = DiagnosticBundleContents.All
            .Select(DiagnosticBundleContents.FileNameFor)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "launcher-version.json",
                "engine-info.json",
                "operating-system.json",
                "job-summary.json",
                "redacted-log.jsonl",
                "integrity-check.json",
            },
            fileNames);
        Assert.Equal(Enum.GetValues<DiagnosticBundleItem>().Length, fileNames.Length);
    }

    [Fact]
    public void LauncherLogEntry_SortsFieldsAndCopiesThem()
    {
        var fields = new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" };

        var entry = new LauncherLogEntry(
            DateTimeOffset.UnixEpoch, LauncherLogLevel.Information, "job.started", fields);
        fields["c"] = "3";

        Assert.Equal(new[] { "a", "b" }, entry.Fields.Keys.ToArray());
    }

    [Fact]
    public void RedactedPath_LowercasesTheHashAndKeepsNoDirectory()
    {
        var redacted = new RedactedPath("report.html", ".html", "ABCDEF", PathRootKind.UserProfile);

        Assert.Equal("report.html", redacted.FileName);
        Assert.Equal("abcdef", redacted.PathHash);
        Assert.Equal(PathRootKind.UserProfile, redacted.PathRootKind);
    }

    [Fact]
    public void OutputCollision_DefaultsToCancelAndNeverToReplace()
    {
        Assert.Equal(OutputCollisionDecision.Cancel, default(OutputCollisionDecision));
        Assert.Equal(OutputCollisionDecision.Cancel, OutputCollisionResolution.Cancel.Decision);
        Assert.Equal(OutputCollisionDecision.Replace, OutputCollisionResolution.Replace.Decision);
        Assert.Null(OutputCollisionResolution.Replace.ReplacementPath);
    }

    [Fact]
    public void OutputCollision_DifferentPathMustBeAbsolute()
    {
        Assert.Throws<ArgumentException>(() => OutputCollisionResolution.UseDifferentPath("report.html"));

        string absolute = Path.Combine(Path.GetTempPath(), "report-2.html");
        OutputCollisionResolution resolution = OutputCollisionResolution.UseDifferentPath(absolute);

        Assert.Equal(OutputCollisionDecision.UseDifferentPath, resolution.Decision);
        Assert.Equal(absolute, resolution.ReplacementPath);
    }

    [Fact]
    public void LauncherSettings_AreImmutableAndCopyOnChange()
    {
        LauncherSettings settings = LauncherSettings.Default;
        LauncherSettings dark = settings.WithTheme(ThemePreference.Dark);

        Assert.Equal(ThemePreference.System, settings.Theme);
        Assert.Equal(ThemePreference.Dark, dark.Theme);
        Assert.Null(dark.LastOutputDirectory);
        Assert.True(dark.ShowExperimentalWarnings);
    }

    private static JobSnapshot Snapshot(JobState state)
    {
        return new JobSnapshot(
            JobId.New(),
            LauncherOperationKind.QuickReport,
            state,
            DateTimeOffset.UnixEpoch,
            completedAtUtc: null,
            result: null);
    }
}
