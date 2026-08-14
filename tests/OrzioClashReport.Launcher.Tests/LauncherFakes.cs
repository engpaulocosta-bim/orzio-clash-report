using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Platform;

namespace OrzioClashReport.Launcher.Tests
{
    internal sealed class FakeEngineLocator : IEngineLocator
    {
        private readonly EngineLocation? _location;

        public FakeEngineLocator(EngineLocation? location) => _location = location;

        public EngineLocation? Locate() => _location;
    }

    internal sealed class FakeIntegrityVerifier : IEngineIntegrityVerifier
    {
        private readonly EngineIntegrityResult _result;

        public FakeIntegrityVerifier(EngineIntegrityVerdict verdict)
            : this(new EngineIntegrityResult(
                verdict,
                verdict == EngineIntegrityVerdict.NotChecked ? null : "expected",
                verdict == EngineIntegrityVerdict.Verified ? "expected" : "actual"))
        {
        }

        public FakeIntegrityVerifier(EngineIntegrityResult result) => _result = result;

        public Task<EngineIntegrityResult> VerifyAsync(EngineLocation location, CancellationToken cancellationToken) =>
            Task.FromResult(_result);
    }

    internal sealed class FakeExpectationSource : IEngineExpectationSource
    {
        private readonly string? _version;

        public FakeExpectationSource(string? version) => _version = version;

        public string? ReadExpectedVersion(EngineLocation location) => _version;
    }

    /// <summary>
    /// Stands in for the engine process. It records every request so tests can assert on the exact
    /// argument vector and working directory the launcher would have used.
    /// </summary>
    internal sealed class FakeProcessRunner : IEngineProcessRunner
    {
        private readonly Func<EngineProcessRequest, EngineProcessResult> _respond;

        public FakeProcessRunner(EngineProcessResult result)
            : this(_ => result)
        {
        }

        public FakeProcessRunner(Func<EngineProcessRequest, EngineProcessResult> respond)
        {
            _respond = respond;
        }

        public List<EngineProcessRequest> Requests { get; } = new List<EngineProcessRequest>();

        public Task<EngineProcessResult> RunAsync(
            EngineProcessRequest request,
            IProgress<EngineJobProgress>? progress,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_respond(request));
        }

        public static EngineProcessResult Completed(int exitCode, string standardOutput = "", string standardError = "") =>
            new EngineProcessResult(
                exitCode, standardOutput, standardError, false, false, false, false, null, TimeSpan.FromMilliseconds(1));

        public static EngineProcessResult TimedOut() =>
            new EngineProcessResult(
                null, string.Empty, string.Empty, false, false, true, false, null, TimeSpan.FromSeconds(5));

        public static EngineProcessResult Canceled() =>
            new EngineProcessResult(
                null, string.Empty, string.Empty, false, false, false, true, null, TimeSpan.FromSeconds(1));

        public static EngineProcessResult StartFailure(string message) =>
            new EngineProcessResult(
                null, string.Empty, string.Empty, false, false, false, false, message, TimeSpan.Zero);
    }

    internal sealed class CollectingLauncherLog : ILauncherLog
    {
        public List<LauncherLogEntry> Entries { get; } = new List<LauncherLogEntry>();

        public void Write(LauncherLogEntry entry) => Entries.Add(entry);
    }

    internal sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; set; }
    }
}
