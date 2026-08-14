using System;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;

namespace OrzioClashReport.Launcher.Application.Engine
{
    /// <summary>
    /// Establishes what the engine actually is, in a fixed order: it must be present, then its bytes
    /// must match the packaged SHA-256, and only then is it executed — with <c>--version</c> as its one
    /// and only argument — to confirm it reports the version this launcher was built against.
    /// </summary>
    public sealed class EngineProbe
    {
        /// <summary>The engine only prints one line here; five seconds is generous and bounded.</summary>
        public static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(5);

        private const string VersionArgument = "--version";

        private readonly IEngineLocator _locator;
        private readonly IEngineIntegrityVerifier _integrityVerifier;
        private readonly IEngineExpectationSource _expectationSource;
        private readonly IEngineProcessRunner _processRunner;
        private readonly string _probeWorkingDirectory;

        public EngineProbe(
            IEngineLocator locator,
            IEngineIntegrityVerifier integrityVerifier,
            IEngineExpectationSource expectationSource,
            IEngineProcessRunner processRunner,
            string probeWorkingDirectory)
        {
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _integrityVerifier = integrityVerifier ?? throw new ArgumentNullException(nameof(integrityVerifier));
            _expectationSource = expectationSource ?? throw new ArgumentNullException(nameof(expectationSource));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));

            if (string.IsNullOrWhiteSpace(probeWorkingDirectory))
            {
                throw new ArgumentException("Probe working directory cannot be empty.", nameof(probeWorkingDirectory));
            }

            // Never the installation directory: probing must not be able to write there, even by accident.
            _probeWorkingDirectory = probeWorkingDirectory;
        }

        public async Task<EngineInfo> DescribeAsync(CancellationToken cancellationToken)
        {
            EngineLocation? location = _locator.Locate();
            if (location == null)
            {
                return new EngineInfo(
                    EngineStatusKind.Missing,
                    null,
                    LauncherBuildInfo.FallbackExpectedEngineVersion,
                    null,
                    EngineIntegrityResult.NotChecked,
                    "Não foi encontrado nenhum executável do motor na pasta de instalação.");
            }

            string expectedVersion = _expectationSource.ReadExpectedVersion(location)
                ?? LauncherBuildInfo.FallbackExpectedEngineVersion;

            EngineIntegrityResult integrity =
                await _integrityVerifier.VerifyAsync(location, cancellationToken).ConfigureAwait(false);

            if (integrity.Verdict != EngineIntegrityVerdict.Verified)
            {
                string detail = integrity.Verdict == EngineIntegrityVerdict.Mismatch
                    ? "O executável do motor não corresponde ao SHA-256 registado na instalação."
                    : "Não foi possível verificar a integridade do motor: falta o manifesto empacotado.";

                // An engine whose bytes could not be confirmed is never executed.
                return new EngineInfo(
                    EngineStatusKind.IntegrityFailure, null, expectedVersion, location, integrity, detail);
            }

            var request = new EngineProcessRequest(
                location.ExecutablePath,
                new[] { VersionArgument },
                _probeWorkingDirectory,
                VersionTimeout);

            EngineProcessResult result =
                await _processRunner.RunAsync(request, null, cancellationToken).ConfigureAwait(false);

            if (result.StartFailure != null)
            {
                return Unsupported(location, expectedVersion, integrity,
                    "O motor não pôde ser iniciado neste sistema.");
            }

            if (result.TimedOut)
            {
                return Unsupported(location, expectedVersion, integrity,
                    "O motor não respondeu a --version dentro de cinco segundos.");
            }

            if (result.Canceled)
            {
                return new EngineInfo(
                    EngineStatusKind.Checking, null, expectedVersion, location, integrity,
                    "A verificação do motor foi interrompida.");
            }

            if (result.ExitCode != 0)
            {
                return Unsupported(location, expectedVersion, integrity,
                    $"O motor terminou com o código {result.ExitCode} ao responder a --version.");
            }

            if (!EngineVersionParser.TryParse(result.StandardOutput, out string reportedVersion))
            {
                return Unsupported(location, expectedVersion, integrity,
                    "O motor não respondeu no formato publicado de --version.");
            }

            if (!EngineVersionParser.Matches(reportedVersion, expectedVersion))
            {
                return new EngineInfo(
                    EngineStatusKind.VersionMismatch,
                    reportedVersion,
                    expectedVersion,
                    location,
                    integrity,
                    $"O motor reporta {reportedVersion}, mas esta aplicação foi empacotada com {expectedVersion}.");
            }

            return new EngineInfo(
                EngineStatusKind.Ready,
                reportedVersion,
                expectedVersion,
                location,
                integrity,
                $"Motor {reportedVersion} verificado.");
        }

        private static EngineInfo Unsupported(
            EngineLocation location, string expectedVersion, EngineIntegrityResult integrity, string detail) =>
            new EngineInfo(EngineStatusKind.Unsupported, null, expectedVersion, location, integrity, detail);
    }
}
