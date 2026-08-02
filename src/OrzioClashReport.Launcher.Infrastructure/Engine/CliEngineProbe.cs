using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Infrastructure.Processes;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// Establishes which engine is installed by running it with <c>--version</c> as its single
    /// argument and reading exactly the one line the engine's published contract produces.
    /// </summary>
    public sealed class CliEngineProbe : IEngineProbe
    {
        /// <summary>The engine's published version line, and nothing looser than it.</summary>
        internal static readonly Regex VersionPattern = new Regex(
            @"^orzioclash (?<version>\d+\.\d+\.\d+-[A-Za-z0-9.]+)$",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        internal static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

        private readonly EngineLayout _layout;
        private readonly EngineManifestIntegrityVerifier _integrityVerifier;
        private readonly ProcessJobRunner _runner;

        public CliEngineProbe(
            EngineLayout layout, EngineManifestIntegrityVerifier integrityVerifier, ProcessJobRunner runner)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _integrityVerifier = integrityVerifier ?? throw new ArgumentNullException(nameof(integrityVerifier));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public async Task<EngineProbeResult> ProbeAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_layout.ExecutablePath))
            {
                return EngineProbeResult.Missing(
                    "No engine was found in this installation. Reinstall Orzio Clash Report to restore it.");
            }

            EngineIntegrityResult integrity = _integrityVerifier.Verify();
            if (integrity.Status == EngineIntegrityStatus.HashMismatch)
            {
                return EngineProbeResult.IntegrityFailure(
                    "The installed engine does not match the signature recorded at packaging time. "
                        + "Reinstall Orzio Clash Report before running any operation.");
            }

            if (integrity.Status == EngineIntegrityStatus.EngineMissing)
            {
                return EngineProbeResult.Missing(
                    "No engine was found in this installation. Reinstall Orzio Clash Report to restore it.");
            }

            if (!integrity.IsVerified)
            {
                return EngineProbeResult.IntegrityFailure(
                    integrity.Detail ?? "The engine's integrity could not be verified.");
            }

            var specification = new ProcessRunSpecification(
                _layout.ExecutablePath,
                new[] { "--version" },
                ProbeWorkingDirectory,
                ProbeTimeout);

            ProcessRunResult run = await _runner
                .RunAsync(specification, progress: null, cancellationToken)
                .ConfigureAwait(false);

            switch (run.Outcome)
            {
                case ProcessRunOutcome.StartFailed:
                    return EngineProbeResult.Unsupported(
                        "The engine could not be started: " + (run.StartFailureMessage ?? "unknown reason") + ".");
                case ProcessRunOutcome.TimedOut:
                    return EngineProbeResult.Unsupported(
                        "The engine did not answer the version check within five seconds.");
                case ProcessRunOutcome.Canceled:
                    return EngineProbeResult.Unsupported("The engine version check was cancelled.");
            }

            if (run.ExitCode != 0)
            {
                return EngineProbeResult.Unsupported("The engine version check ended with a failure.");
            }

            string reported = FirstLine(run.StandardOutput);
            Match match = VersionPattern.Match(reported);
            if (!match.Success)
            {
                return EngineProbeResult.Unsupported(
                    "The engine answered the version check in a format this launcher does not recognize.");
            }

            string version = match.Groups["version"].Value;
            string? expected = _integrityVerifier.TryReadDeclaredVersion();

            if (expected != null && !string.Equals(expected, version, StringComparison.Ordinal))
            {
                return EngineProbeResult.VersionMismatch(version, expected);
            }

            return EngineProbeResult.Ready(version);
        }

        /// <summary>
        /// The probe writes nothing, and the installation directory is never a working directory, so
        /// the temporary directory is the correct neutral location.
        /// </summary>
        private static string ProbeWorkingDirectory => Path.GetTempPath();

        internal static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            int end = text.IndexOfAny(new[] { '\r', '\n' });
            string line = end < 0 ? text : text.Substring(0, end);
            return line.Trim();
        }
    }
}
