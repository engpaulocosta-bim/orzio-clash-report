using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Infrastructure.Process;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// Drives the real process runner against a real child process, across every behaviour the engine
    /// could plausibly produce — including the ones it is not supposed to.
    /// </summary>
    public sealed class ProcessJobRunnerTests : IDisposable
    {
        private readonly string _workingDirectory;
        private readonly ProcessJobRunner _runner = new ProcessJobRunner();

        public ProcessJobRunnerTests()
        {
            _workingDirectory = Path.Combine(Path.GetTempPath(), "orzio-runner-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_workingDirectory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_workingDirectory, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temporary directory is not worth failing a test over.
            }
        }

        [Fact]
        public async Task ASuccessfulRunProducesItsOutputAndExitsZero()
        {
            string output = Path.Combine(_workingDirectory, "report.html");

            EngineProcessResult result = await RunAsync("succeed-with-output", "-o", output);

            Assert.True(result.CompletedNormally);
            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(output));
            Assert.Contains("Report written to", result.StandardOutput);
        }

        [Fact]
        public async Task ASuccessfulRunThatWritesNoFileStillExitsZero()
        {
            EngineProcessResult result = await RunAsync("succeed-without-output");

            Assert.Equal(0, result.ExitCode);
            Assert.Empty(Directory.GetFiles(_workingDirectory));
        }

        [Fact]
        public async Task AFailedRunCapturesStandardErrorAndExitsOne()
        {
            EngineProcessResult result = await RunAsync("fail");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("not a Clash Detective export", result.StandardError);
            Assert.Empty(result.StandardOutput);
        }

        [Fact]
        public async Task AnUnexpectedExitCodeIsReportedExactlyAsObserved()
        {
            EngineProcessResult result = await RunAsync("unexpected-exit", "42");

            Assert.Equal(42, result.ExitCode);
            Assert.True(result.CompletedNormally);
        }

        [Fact]
        public async Task ARunThatNeverEndsIsTerminatedAtTheTimeout()
        {
            EngineProcessResult result = await RunAsync(
                TimeSpan.FromMilliseconds(500), CancellationToken.None, "hang");

            Assert.True(result.TimedOut);
            Assert.False(result.Canceled);
            Assert.Null(result.ExitCode);
        }

        [Fact]
        public async Task CancellationStopsTheRunAndIsReportedAsCancelledNotFailed()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                Task<EngineProcessResult> run = RunAsync(
                    TimeSpan.FromMinutes(5), cancellation.Token, "hang");

                cancellation.CancelAfter(TimeSpan.FromMilliseconds(250));

                EngineProcessResult result = await run;

                Assert.True(result.Canceled);
                Assert.False(result.TimedOut);
                Assert.Null(result.ExitCode);
            }
        }

        [Fact]
        public async Task AVeryLargeStandardOutputIsBoundedButKeepsBothEnds()
        {
            EngineProcessResult result = await RunAsync("huge-stdout", "20000");

            Assert.Equal(0, result.ExitCode);
            Assert.True(result.StandardOutputTruncated);
            Assert.Equal(64 * 1024, result.StandardOutput.Length);
            Assert.StartsWith("BEGIN-MARKER", result.StandardOutput);
            Assert.EndsWith("END-MARKER\n", result.StandardOutput.Replace("\r\n", "\n"));
        }

        [Fact]
        public async Task AVeryLargeStandardErrorIsBoundedTheSameWay()
        {
            EngineProcessResult result = await RunAsync("huge-stderr", "20000");

            Assert.True(result.StandardErrorTruncated);
            Assert.Equal(64 * 1024, result.StandardError.Length);
            Assert.StartsWith("BEGIN-MARKER", result.StandardError);
        }

        [Fact]
        public async Task UndecodableOutputIsCapturedWithReplacementCharactersInsteadOfThrowing()
        {
            EngineProcessResult result = await RunAsync("bad-encoding");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("before", result.StandardOutput);
            Assert.Contains("after", result.StandardOutput);
            Assert.Contains('\uFFFD', result.StandardOutput);
        }

        [Fact]
        public async Task ArgumentsReachTheEngineExactlyAsSuppliedIncludingSpacesAndQuotes()
        {
            string[] awkward =
            {
                "echo-arguments",
                @"C:\Clients\ACME Tower\run 004.xml",
                "-o",
                @"C:\Reports\report ""final"".html",
                "--reason",
                "spaces & ampersands | pipes ; semicolons",
            };

            EngineProcessResult result = await RunAsync(awkward);

            string[] echoed = result.StandardOutput
                .Replace("\r\n", "\n")
                .TrimEnd('\n')
                .Split('\n');

            Assert.Equal(awkward, echoed);
        }

        [Fact]
        public async Task TheEngineRunsInTheWorkingDirectoryItWasGiven()
        {
            EngineProcessResult result = await RunAsync("echo-working-directory");

            Assert.Equal(
                Path.GetFullPath(_workingDirectory),
                Path.GetFullPath(result.StandardOutput.Trim()));
        }

        [Fact]
        public async Task AMissingExecutableIsAStartFailureRatherThanAnException()
        {
            var request = new EngineProcessRequest(
                Path.Combine(_workingDirectory, "does-not-exist"),
                new[] { "--version" },
                _workingDirectory,
                TimeSpan.FromSeconds(5));

            EngineProcessResult result = await _runner.RunAsync(request, null, CancellationToken.None);

            Assert.NotNull(result.StartFailure);
            Assert.False(result.CompletedNormally);
            Assert.Null(result.ExitCode);
        }

        [Fact]
        public async Task ProgressReportsEveryOutputLineAsItArrives()
        {
            var lines = new List<string>();
            var progress = new Progress<EngineJobProgress>(update =>
            {
                if (update.Line != null)
                {
                    lock (lines)
                    {
                        lines.Add(update.Line);
                    }
                }
            });

            var request = new EngineProcessRequest(
                FakeEngineLocation.ExecutablePath,
                new[] { "echo-arguments", "one", "two" },
                _workingDirectory,
                TimeSpan.FromSeconds(30));

            await _runner.RunAsync(request, progress, CancellationToken.None);

            // Progress is delivered on the captured context; give the queued callbacks a moment.
            await Task.Delay(200);

            lock (lines)
            {
                Assert.Contains("one", lines);
                Assert.Contains("two", lines);
            }
        }

        private Task<EngineProcessResult> RunAsync(params string[] arguments) =>
            RunAsync(TimeSpan.FromSeconds(60), CancellationToken.None, arguments);

        private Task<EngineProcessResult> RunAsync(
            TimeSpan timeout, CancellationToken cancellationToken, params string[] arguments)
        {
            var request = new EngineProcessRequest(
                FakeEngineLocation.ExecutablePath, arguments, _workingDirectory, timeout);

            return _runner.RunAsync(request, null, cancellationToken);
        }
    }
}
