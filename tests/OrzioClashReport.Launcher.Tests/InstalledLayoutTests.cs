using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Infrastructure.Engine;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// Builds the tree the installer lays down and points the launcher's own discovery and
    /// verification code at it. This is what keeps publish-launcher.ps1 and the running application
    /// agreeing on one contract instead of two that merely look alike.
    /// </summary>
    public sealed class InstalledLayoutTests : IDisposable
    {
        private readonly string _installDirectory;
        private readonly string _engineDirectory;

        public InstalledLayoutTests()
        {
            _installDirectory = Path.Combine(Path.GetTempPath(), "orzio-install-" + Guid.NewGuid().ToString("N"));
            _engineDirectory = Path.Combine(
                _installDirectory,
                InstalledEngineLocator.EngineFolderName,
                InstalledEngineLocator.RuntimeFolderName);

            Directory.CreateDirectory(_engineDirectory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_installDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Temporary cleanup only.
            }
        }

        [Fact]
        public void TheLauncherFindsTheEngineWhereTheInstallerPutsIt()
        {
            string enginePath = WriteEngine("engine bytes");
            WriteManifest(Sha256Of("engine bytes"), "0.1.0-preview.3");

            EngineLocation? location = new InstalledEngineLocator(_installDirectory).Locate();

            Assert.NotNull(location);
            Assert.Equal(enginePath, location!.ExecutablePath);
            Assert.Equal(
                Path.Combine(_engineDirectory, InstalledEngineLocator.ManifestFileName),
                location.ManifestPath);
        }

        [Fact]
        public void NoEngineMeansNoLocationRatherThanASearchElsewhere()
        {
            Assert.Null(new InstalledEngineLocator(_installDirectory).Locate());
        }

        [Fact]
        public async Task AManifestWrittenTheWayThePublishScriptWritesItVerifies()
        {
            const string bytes = "the exact bytes that were published";

            WriteEngine(bytes);
            WriteManifest(Sha256Of(bytes), "0.1.0-preview.3");

            EngineLocation location = new InstalledEngineLocator(_installDirectory).Locate()!;
            var verifier = new Sha256EngineIntegrityVerifier(new EngineManifestReader());

            EngineIntegrityResult result = await verifier.VerifyAsync(location, CancellationToken.None);

            Assert.Equal(EngineIntegrityVerdict.Verified, result.Verdict);
            Assert.Equal(result.ExpectedSha256, result.ActualSha256);

            Assert.Equal(
                "0.1.0-preview.3",
                new ManifestEngineExpectationSource(new EngineManifestReader()).ReadExpectedVersion(location));
        }

        [Fact]
        public async Task AnEngineThatWasSwappedAfterInstallationFailsVerification()
        {
            WriteEngine("the bytes that were published");
            WriteManifest(Sha256Of("the bytes that were published"), "0.1.0-preview.3");

            // Someone replaced the executable in place, leaving the manifest untouched.
            WriteEngine("different bytes entirely");

            EngineLocation location = new InstalledEngineLocator(_installDirectory).Locate()!;
            var verifier = new Sha256EngineIntegrityVerifier(new EngineManifestReader());

            EngineIntegrityResult result = await verifier.VerifyAsync(location, CancellationToken.None);

            Assert.Equal(EngineIntegrityVerdict.Mismatch, result.Verdict);
            Assert.NotEqual(result.ExpectedSha256, result.ActualSha256);
        }

        [Fact]
        public async Task AMissingManifestIsUnverifiableRatherThanAcceptable()
        {
            WriteEngine("engine bytes");

            EngineLocation location = new InstalledEngineLocator(_installDirectory).Locate()!;
            var verifier = new Sha256EngineIntegrityVerifier(new EngineManifestReader());

            EngineIntegrityResult result = await verifier.VerifyAsync(location, CancellationToken.None);

            Assert.Equal(EngineIntegrityVerdict.ManifestUnavailable, result.Verdict);
        }

        [Fact]
        public void AManifestWithAnUnsupportedSchemaIsRejected()
        {
            WriteEngine("engine bytes");

            File.WriteAllText(
                Path.Combine(_engineDirectory, InstalledEngineLocator.ManifestFileName),
                "{\n  \"schemaVersion\": 2,\n  \"engineVersion\": \"9.9.9-x.1\",\n"
                + "  \"fileName\": \"orzioclash.exe\",\n  \"sha256\": \"" + new string('a', 64) + "\"\n}");

            Assert.Null(new EngineManifestReader().TryRead(
                Path.Combine(_engineDirectory, InstalledEngineLocator.ManifestFileName), out string reason));

            Assert.NotEmpty(reason);
        }

        private string WriteEngine(string content)
        {
            string path = Path.Combine(_engineDirectory, InstalledEngineLocator.ExecutableFileName);
            File.WriteAllText(path, content);
            return path;
        }

        /// <summary>Writes the manifest exactly as publish-launcher.ps1 does: schema 1, lower-case digest.</summary>
        private void WriteManifest(string sha256, string engineVersion)
        {
            File.WriteAllText(
                Path.Combine(_engineDirectory, InstalledEngineLocator.ManifestFileName),
                "{\n"
                + "  \"schemaVersion\": 1,\n"
                + $"  \"engineVersion\": \"{engineVersion}\",\n"
                + "  \"fileName\": \"orzioclash.exe\",\n"
                + $"  \"sha256\": \"{sha256}\"\n"
                + "}");
        }

        private static string Sha256Of(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));

                var builder = new StringBuilder(digest.Length * 2);
                foreach (byte value in digest)
                {
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }
    }
}
