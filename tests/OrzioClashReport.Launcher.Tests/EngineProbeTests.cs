using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class EngineProbeTests
    {
        private static readonly EngineLocation Location =
            new EngineLocation("/install/engine/win-x64/orzioclash.exe", "/install/engine/win-x64/engine-manifest.json");

        private const string ExpectedVersion = "0.1.0-preview.3";

        [Fact]
        public async Task AnAbsentEngineIsMissing()
        {
            EngineInfo info = await DescribeAsync(location: null, runner: new FakeProcessRunner(Ok()));

            Assert.Equal(EngineStatusKind.Missing, info.Status);
            Assert.False(info.IsReady);
        }

        [Fact]
        public async Task AMismatchedHashIsAnIntegrityFailureAndTheEngineIsNeverExecuted()
        {
            var runner = new FakeProcessRunner(Ok());

            EngineInfo info = await DescribeAsync(
                Location, runner, verdict: EngineIntegrityVerdict.Mismatch);

            Assert.Equal(EngineStatusKind.IntegrityFailure, info.Status);
            Assert.Empty(runner.Requests);
        }

        [Fact]
        public async Task AnUnreadableManifestIsAlsoAnIntegrityFailure()
        {
            var runner = new FakeProcessRunner(Ok());

            EngineInfo info = await DescribeAsync(
                Location, runner, verdict: EngineIntegrityVerdict.ManifestUnavailable);

            Assert.Equal(EngineStatusKind.IntegrityFailure, info.Status);
            Assert.Empty(runner.Requests);
        }

        [Fact]
        public async Task TheVersionProbePassesExactlyOneArgument()
        {
            var runner = new FakeProcessRunner(Ok());

            await DescribeAsync(Location, runner);

            EngineProcessRequest request = Assert.Single(runner.Requests);
            Assert.Equal(new[] { "--version" }, request.ArgumentList);
            Assert.Equal(Location.ExecutablePath, request.ExecutablePath);
            Assert.Equal(EngineProbe.VersionTimeout, request.Timeout);
        }

        [Fact]
        public async Task TheVersionProbeNeverRunsInTheInstallationDirectory()
        {
            var runner = new FakeProcessRunner(Ok());

            await DescribeAsync(Location, runner);

            EngineProcessRequest request = Assert.Single(runner.Requests);
            Assert.DoesNotContain("install", request.WorkingDirectory);
        }

        [Fact]
        public async Task TheVersionTimeoutIsFiveSeconds()
        {
            Assert.Equal(5, EngineProbe.VersionTimeout.TotalSeconds);
        }

        [Fact]
        public async Task AMatchingVersionIsReady()
        {
            EngineInfo info = await DescribeAsync(
                Location, new FakeProcessRunner(Ok("orzioclash " + ExpectedVersion + "\n")));

            Assert.Equal(EngineStatusKind.Ready, info.Status);
            Assert.Equal(ExpectedVersion, info.ReportedVersion);
            Assert.True(info.IsReady);
        }

        [Fact]
        public async Task ADifferentVersionIsAVersionMismatchAndCarriesBothVersions()
        {
            EngineInfo info = await DescribeAsync(
                Location, new FakeProcessRunner(Ok("orzioclash 0.1.0-preview.2\n")));

            Assert.Equal(EngineStatusKind.VersionMismatch, info.Status);
            Assert.Equal("0.1.0-preview.2", info.ReportedVersion);
            Assert.Equal(ExpectedVersion, info.ExpectedVersion);
        }

        [Theory]
        [InlineData("not a version line")]
        [InlineData("")]
        [InlineData("orzioclash 0.1.0")]
        public async Task UnparseableOutputIsUnsupported(string standardOutput)
        {
            EngineInfo info = await DescribeAsync(Location, new FakeProcessRunner(Ok(standardOutput)));

            Assert.Equal(EngineStatusKind.Unsupported, info.Status);
        }

        [Fact]
        public async Task ANonZeroExitIsUnsupported()
        {
            EngineInfo info = await DescribeAsync(
                Location, new FakeProcessRunner(FakeProcessRunner.Completed(1)));

            Assert.Equal(EngineStatusKind.Unsupported, info.Status);
        }

        [Fact]
        public async Task ATimeoutIsUnsupported()
        {
            EngineInfo info = await DescribeAsync(
                Location, new FakeProcessRunner(FakeProcessRunner.TimedOut()));

            Assert.Equal(EngineStatusKind.Unsupported, info.Status);
        }

        [Fact]
        public async Task AFailureToStartIsUnsupported()
        {
            EngineInfo info = await DescribeAsync(
                Location, new FakeProcessRunner(FakeProcessRunner.StartFailure("not executable")));

            Assert.Equal(EngineStatusKind.Unsupported, info.Status);
        }

        [Fact]
        public async Task AnInterruptedProbeStaysInTheCheckingState()
        {
            EngineInfo info = await DescribeAsync(
                Location, new FakeProcessRunner(FakeProcessRunner.Canceled()));

            Assert.Equal(EngineStatusKind.Checking, info.Status);
        }

        [Fact]
        public async Task TheManifestIsTheAuthorityForTheExpectedVersion()
        {
            EngineInfo info = await DescribeAsync(
                Location,
                new FakeProcessRunner(Ok("orzioclash 9.9.9-custom.1\n")),
                expectedVersion: "9.9.9-custom.1");

            Assert.Equal(EngineStatusKind.Ready, info.Status);
            Assert.Equal("9.9.9-custom.1", info.ExpectedVersion);
        }

        private static EngineProcessResult Ok(string standardOutput = "orzioclash 0.1.0-preview.3\n") =>
            FakeProcessRunner.Completed(0, standardOutput);

        private static Task<EngineInfo> DescribeAsync(
            EngineLocation? location,
            FakeProcessRunner runner,
            EngineIntegrityVerdict verdict = EngineIntegrityVerdict.Verified,
            string? expectedVersion = ExpectedVersion)
        {
            var probe = new EngineProbe(
                new FakeEngineLocator(location),
                new FakeIntegrityVerifier(verdict),
                new FakeExpectationSource(expectedVersion),
                runner,
                Path.GetTempPath());

            return probe.DescribeAsync(CancellationToken.None);
        }
    }
}
