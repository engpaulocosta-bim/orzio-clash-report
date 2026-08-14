using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Infrastructure.Engine;
using OrzioClashReport.Launcher.Infrastructure.Logging;
using OrzioClashReport.Launcher.Infrastructure.Platform;
using OrzioClashReport.Launcher.Infrastructure.Process;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// The advanced operations driven through the same executor and a real child process: what gets
    /// created, what is refused, and what the engine actually receives.
    /// </summary>
    public sealed class AdvancedOperationsEndToEndTests : IDisposable
    {
        private readonly string _root;
        private readonly string _projectDirectory;
        private readonly string _installationDirectory;
        private readonly LauncherStorageLocations _locations;

        public AdvancedOperationsEndToEndTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "orzio-advanced-" + Guid.NewGuid().ToString("N"));
            _projectDirectory = Path.Combine(_root, "project");
            _installationDirectory = Path.Combine(_root, "install");

            Directory.CreateDirectory(_projectDirectory);
            Directory.CreateDirectory(_installationDirectory);

            _locations = new LauncherStorageLocations(Path.Combine(_root, "appdata"));
            _locations.EnsureCreated();
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
                // Temporary cleanup only.
            }
        }

        [Fact]
        public async Task CreatingASnapshotProducesTheSnapshotAndRecordsItAsEvidence()
        {
            string output = Path.Combine(_projectDirectory, "run-001.json");

            EngineJobResult result = await ExecuteAsync(
                LauncherOperationKind.Snapshot,
                EngineArgumentBuilder.Snapshot(File_("run.xml"), File_("run.manifest.json"), output),
                output);

            Assert.Equal(EngineJobState.Succeeded, result.State);
            Assert.Equal(LauncherArtifactKind.RunSnapshot, Assert.Single(result.Artifacts).Kind);
            Assert.True(File.Exists(output));
        }

        [Fact]
        public async Task AnExistingSnapshotIsNeverReplacedEvenWhenReplacementIsRequested()
        {
            string output = Path.Combine(_projectDirectory, "run-001.json");
            File.WriteAllText(output, "ORIGINAL EVIDENCE");

            foreach (OutputCollisionDecision decision in new[]
            {
                OutputCollisionDecision.None,
                OutputCollisionDecision.ChooseAnotherName,
                OutputCollisionDecision.ReplaceExisting,
            })
            {
                EngineJobResult result = await ExecuteAsync(
                    LauncherOperationKind.Snapshot,
                    EngineArgumentBuilder.Snapshot(File_("run.xml"), File_("run.manifest.json"), output),
                    output,
                    decision);

                Assert.Equal(LauncherErrorKind.OutputCollision, result.Error!.Kind);
                Assert.Equal("ORIGINAL EVIDENCE", File.ReadAllText(output));
            }
        }

        [Theory]
        [InlineData(LauncherOperationKind.IndexSnapshots)]
        [InlineData(LauncherOperationKind.CreateProject)]
        [InlineData(LauncherOperationKind.CreateIdentityGovernance)]
        public void NoEvidenceArtifactIsEverReplaceable(LauncherOperationKind operation)
        {
            Assert.False(LauncherOperationMetadata.ProducesReplaceableHtmlOutput(operation));
        }

        [Fact]
        public async Task TheRunIndexIsCreatedWithTheDeclaredOrderReachingTheEngineUnchanged()
        {
            string output = Path.Combine(_projectDirectory, "run-index.json");

            var declared = new List<string>
            {
                File_("run-c.json"),
                File_("run-a.json"),
                File_("run-c.json"),
                File_("run-b.json"),
            };

            IReadOnlyList<string> arguments = EngineArgumentBuilder.IndexSnapshots(declared, output);

            EngineJobResult result = await ExecuteAsync(
                LauncherOperationKind.IndexSnapshots, arguments, output);

            Assert.Equal(EngineJobState.Succeeded, result.State);
            Assert.Equal(LauncherArtifactKind.RunIndex, Assert.Single(result.Artifacts).Kind);

            var received = new List<string>();
            for (int i = 0; i < arguments.Count - 1; i++)
            {
                if (arguments[i] == "--snapshot")
                {
                    received.Add(arguments[i + 1]);
                }
            }

            Assert.Equal(declared, received);
        }

        [Fact]
        public async Task AppendingToAProjectSendsNoDestinationAndProducesNoArtifact()
        {
            string project = File_("project.json");

            EngineJobResult result = await ExecuteAsync(
                LauncherOperationKind.AppendProjectSnapshot,
                EngineArgumentBuilder.AppendProjectSnapshot(project, File_("run-004.json")),
                outputPath: null,
                workingDirectory: _root);

            Assert.Equal(EngineJobState.Succeeded, result.State);
            Assert.Empty(result.Artifacts);
        }

        [Fact]
        public async Task RenderingAProjectSendsNoDestinationBecauseTheCatalogOwnsIt()
        {
            string project = File_("project.json");

            EngineJobResult result = await ExecuteAsync(
                LauncherOperationKind.RenderProject,
                EngineArgumentBuilder.RenderProject(project),
                outputPath: null,
                workingDirectory: _root);

            Assert.Equal(EngineJobState.Succeeded, result.State);
            Assert.Empty(result.Artifacts);
        }

        [Fact]
        public void ARequestCannotSmuggleADestinationIntoAnOperationThatHasNone()
        {
            Assert.Throws<ArgumentException>(() => new LauncherOperationRequest(
                LauncherOperationKind.RenderProject,
                new[] { "render-project", "--project", "/p/project.json" },
                _projectDirectory,
                Path.Combine(_projectDirectory, "report.html"),
                OutputCollisionDecision.None,
                "project.json"));
        }

        [Fact]
        public async Task ALongitudinalReportIsReplaceableOnlyWithAnExplicitDecision()
        {
            string output = Path.Combine(_projectDirectory, "longitudinal.html");
            File.WriteAllText(output, "PREVIOUS");

            EngineJobResult refused = await ExecuteAsync(
                LauncherOperationKind.CompareIndex,
                EngineArgumentBuilder.CompareIndex(File_("run-index.json"), output),
                output);

            Assert.Equal(LauncherErrorKind.OutputCollision, refused.Error!.Kind);
            Assert.Equal("PREVIOUS", File.ReadAllText(output));

            EngineJobResult replaced = await ExecuteAsync(
                LauncherOperationKind.CompareIndex,
                EngineArgumentBuilder.CompareIndex(File_("run-index.json"), output),
                output,
                OutputCollisionDecision.ReplaceExisting);

            Assert.Equal(EngineJobState.Succeeded, replaced.State);
            Assert.DoesNotContain("PREVIOUS", File.ReadAllText(output));
        }

        private string File_(string fileName)
        {
            string path = Path.Combine(_projectDirectory, fileName);
            System.IO.File.WriteAllText(path, "{}");
            return path;
        }

        private Task<EngineJobResult> ExecuteAsync(
            LauncherOperationKind operation,
            IReadOnlyList<string> arguments,
            string? outputPath,
            OutputCollisionDecision decision = OutputCollisionDecision.None,
            string? workingDirectory = null)
        {
            var request = new LauncherOperationRequest(
                operation,
                arguments,
                workingDirectory ?? (outputPath == null ? _projectDirectory : Path.GetDirectoryName(outputPath)!),
                outputPath,
                decision,
                outputPath == null ? operation.ToString() : Path.GetFileName(outputPath));

            var processRunner = new ProcessJobRunner();
            var fileProbe = new FileSystemProbe();

            var probe = new EngineProbe(
                new FakeEngineLocator(new EngineLocation(
                    FakeEngineLocation.ExecutablePath,
                    Path.Combine(_installationDirectory, "engine-manifest.json"))),
                new FakeIntegrityVerifier(EngineIntegrityVerdict.Verified),
                new FakeExpectationSource("0.1.0-preview.3"),
                processRunner,
                Path.GetTempPath());

            var executor = new LauncherOperationExecutor(
                new CliEngineGateway(probe, processRunner, fileProbe),
                fileProbe,
                new JsonRecentItemsStore(_locations.RecentItemsFilePath),
                new FileSystemJobJournal(_locations.JobsDirectory),
                new CollectingLauncherLog(),
                new Sha256PathRedactor(),
                new FixedClock(DateTimeOffset.UnixEpoch),
                _installationDirectory);

            return executor.ExecuteAsync(request, null, CancellationToken.None);
        }
    }
}
