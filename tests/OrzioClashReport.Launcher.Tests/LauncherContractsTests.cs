using System;
using System.Collections.Generic;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class LauncherContractsTests
    {
        [Fact]
        public void EngineJobRequestCopiesTheArgumentVectorSoLaterMutationCannotChangeIt()
        {
            var arguments = new List<string> { "input.xml", "-o", "report.html" };

            var request = new EngineJobRequest(
                "job-1", LauncherOperationKind.QuickReport, arguments, "/out", "/out/report.html");

            arguments[0] = "tampered.xml";

            Assert.Equal(new[] { "input.xml", "-o", "report.html" }, request.ArgumentList);
        }

        [Fact]
        public void EngineJobRequestRejectsAnEmptyOrNullArgument()
        {
            Assert.Throws<ArgumentException>(() => new EngineJobRequest(
                "job-1", LauncherOperationKind.QuickReport, new[] { "input.xml", string.Empty }, "/out", null));

            Assert.Throws<ArgumentException>(() => new EngineJobRequest(
                "job-1", LauncherOperationKind.QuickReport, new[] { "input.xml", null! }, "/out", null));
        }

        [Fact]
        public void EngineJobRequestRequiresAWorkingDirectoryAndArguments()
        {
            Assert.Throws<ArgumentException>(() => new EngineJobRequest(
                "job-1", LauncherOperationKind.QuickReport, Array.Empty<string>(), "/out", null));

            Assert.Throws<ArgumentException>(() => new EngineJobRequest(
                "job-1", LauncherOperationKind.QuickReport, new[] { "input.xml" }, "  ", null));
        }

        [Fact]
        public void EngineJobResultRequiresATerminalState()
        {
            Assert.Throws<ArgumentException>(() => CreateResult(EngineJobState.Running, null));
            Assert.Throws<ArgumentException>(() => CreateResult(EngineJobState.Pending, null));
        }

        [Fact]
        public void AFailedResultCarriesAnErrorAndASucceededOneDoesNot()
        {
            var error = new LauncherError(
                LauncherErrorKind.EngineExecutionFailure, "The engine reported a failure.", "Read the engine output.", 1);

            Assert.Throws<ArgumentException>(() => CreateResult(EngineJobState.Failed, null));
            Assert.Throws<ArgumentException>(() => CreateResult(EngineJobState.Succeeded, error));

            EngineJobResult failed = CreateResult(EngineJobState.Failed, error);
            Assert.Equal(LauncherErrorKind.EngineExecutionFailure, failed.Error!.Kind);
            Assert.Equal(1, failed.Error.ExitCode);
        }

        [Fact]
        public void ProcessResultCannotBeTimedOutAndCanceledAtOnce()
        {
            Assert.Throws<ArgumentException>(() => new EngineProcessResult(
                null, string.Empty, string.Empty, false, false, true, true, null, TimeSpan.Zero));
        }

        [Fact]
        public void ANormallyCompletedProcessMustReportAnExitCode()
        {
            Assert.Throws<ArgumentException>(() => new EngineProcessResult(
                null, string.Empty, string.Empty, false, false, false, false, null, TimeSpan.Zero));

            var completed = new EngineProcessResult(
                0, "out", string.Empty, false, false, false, false, null, TimeSpan.FromSeconds(1));

            Assert.True(completed.CompletedNormally);
            Assert.Equal(0, completed.ExitCode);
        }

        [Fact]
        public void AReadyEngineMustHaveReportedAVersion()
        {
            Assert.Throws<ArgumentException>(() => new EngineInfo(
                EngineStatusKind.Ready, null, "0.1.0-preview.3", null, EngineIntegrityResult.NotChecked, "detail"));

            var info = new EngineInfo(
                EngineStatusKind.Ready,
                "0.1.0-preview.3",
                "0.1.0-preview.3",
                new EngineLocation("/install/engine/orzioclash.exe", "/install/engine/engine-manifest.json"),
                new EngineIntegrityResult(EngineIntegrityVerdict.Verified, "abc", "abc"),
                "Engine verified.");

            Assert.True(info.IsReady);
        }

        [Theory]
        [InlineData(EngineStatusKind.Checking)]
        [InlineData(EngineStatusKind.VersionMismatch)]
        [InlineData(EngineStatusKind.IntegrityFailure)]
        [InlineData(EngineStatusKind.Missing)]
        [InlineData(EngineStatusKind.Unsupported)]
        public void OnlyAReadyEngineIsUsable(EngineStatusKind status)
        {
            var info = new EngineInfo(
                status, null, "0.1.0-preview.3", null, EngineIntegrityResult.NotChecked, "detail");

            Assert.False(info.IsReady);
        }

        [Fact]
        public void AVerifiedIntegrityResultRequiresBothDigests()
        {
            Assert.Throws<ArgumentException>(() => new EngineIntegrityResult(
                EngineIntegrityVerdict.Verified, "abc", null));

            Assert.Equal(EngineIntegrityVerdict.NotChecked, EngineIntegrityResult.NotChecked.Verdict);
        }

        [Fact]
        public void ALogEntryCarriesPathsOnlyInRedactedForm()
        {
            var entry = new LauncherLogEntry(
                DateTimeOffset.UnixEpoch, LauncherLogLevel.Information, "job.started", "Job started.");

            LauncherLogEntry withPath = entry.WithPath(
                "output", new RedactedPath("report.html", ".html", new string('a', 64), PathRootKind.UserProfile));

            Assert.Equal("report.html", withPath.Fields["output.fileName"]);
            Assert.Equal(".html", withPath.Fields["output.extension"]);
            Assert.Equal(new string('a', 64), withPath.Fields["output.pathHash"]);
            Assert.Equal("UserProfile", withPath.Fields["output.pathRootKind"]);

            // The original entry is untouched: log entries are immutable.
            Assert.Empty(entry.Fields);
        }

        [Fact]
        public void SettingsDefaultToSystemThemeAndNoRememberedDirectory()
        {
            LauncherSettings settings = LauncherSettings.Default;

            Assert.Equal(LauncherThemePreference.System, settings.Theme);
            Assert.Null(settings.LastOutputDirectory);
            Assert.True(settings.ShowExperimentalWarnings);

            LauncherSettings updated = settings.WithTheme(LauncherThemePreference.Dark);
            Assert.Equal(LauncherThemePreference.Dark, updated.Theme);
            Assert.Equal(LauncherThemePreference.System, settings.Theme);
        }

        [Fact]
        public void OnlyHtmlProducingOperationsAreEverReplaceable()
        {
            Assert.True(LauncherOperationMetadata.ProducesReplaceableHtmlOutput(LauncherOperationKind.QuickReport));
            Assert.True(LauncherOperationMetadata.ProducesReplaceableHtmlOutput(LauncherOperationKind.CompareIndex));

            // Snapshots, run indexes, project catalogs and governance documents use create-new
            // semantics inside the engine; the launcher must never offer to overwrite one.
            Assert.False(LauncherOperationMetadata.ProducesReplaceableHtmlOutput(LauncherOperationKind.Snapshot));
            Assert.False(LauncherOperationMetadata.ProducesReplaceableHtmlOutput(LauncherOperationKind.IndexSnapshots));
            Assert.False(LauncherOperationMetadata.ProducesReplaceableHtmlOutput(LauncherOperationKind.CreateProject));
            Assert.False(LauncherOperationMetadata.ProducesReplaceableHtmlOutput(LauncherOperationKind.CreateIdentityGovernance));
        }

        [Fact]
        public void OperationsWithoutAPublishedOutputOptionAreDeclaredAsSuch()
        {
            Assert.False(LauncherOperationMetadata.SupportsOutputOption(LauncherOperationKind.AppendProjectSnapshot));
            Assert.False(LauncherOperationMetadata.SupportsOutputOption(LauncherOperationKind.RenderProject));
            Assert.False(LauncherOperationMetadata.SupportsOutputOption(LauncherOperationKind.AppendIdentityDecision));
            Assert.False(LauncherOperationMetadata.SupportsOutputOption(LauncherOperationKind.ValidateIdentityGovernance));

            Assert.True(LauncherOperationMetadata.SupportsOutputOption(LauncherOperationKind.QuickReport));
            Assert.True(LauncherOperationMetadata.SupportsOutputOption(LauncherOperationKind.Snapshot));
        }

        private static EngineJobResult CreateResult(EngineJobState state, LauncherError? error) =>
            new EngineJobResult(
                "job-1",
                LauncherOperationKind.QuickReport,
                state,
                error == null ? 0 : 1,
                string.Empty,
                string.Empty,
                false,
                false,
                TimeSpan.Zero,
                error,
                Array.Empty<LauncherArtifact>(),
                Array.Empty<LauncherWarning>());
    }
}
