using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// Hashes the engine executable and compares it with the digest recorded by the packaging script.
    /// A mismatch is reported and the engine is refused; it is never "fixed" by rewriting the manifest.
    /// </summary>
    public sealed class Sha256EngineIntegrityVerifier : IEngineIntegrityVerifier
    {
        private readonly EngineManifestReader _manifestReader;

        public Sha256EngineIntegrityVerifier(EngineManifestReader manifestReader)
        {
            _manifestReader = manifestReader ?? throw new ArgumentNullException(nameof(manifestReader));
        }

        public async Task<EngineIntegrityResult> VerifyAsync(
            EngineLocation location, CancellationToken cancellationToken)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            EngineManifest? manifest = _manifestReader.TryRead(location.ManifestPath, out _);
            if (manifest == null)
            {
                return new EngineIntegrityResult(EngineIntegrityVerdict.ManifestUnavailable, null, null);
            }

            string? actual = await ComputeSha256Async(location.ExecutablePath, cancellationToken)
                .ConfigureAwait(false);

            if (actual == null)
            {
                return new EngineIntegrityResult(EngineIntegrityVerdict.ManifestUnavailable, manifest.Sha256, null);
            }

            EngineIntegrityVerdict verdict =
                string.Equals(actual, manifest.Sha256, StringComparison.Ordinal)
                    ? EngineIntegrityVerdict.Verified
                    : EngineIntegrityVerdict.Mismatch;

            return new EngineIntegrityResult(verdict, manifest.Sha256, actual);
        }

        public static async Task<string?> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                using (var stream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true))
                using (var sha256 = SHA256.Create())
                {
                    byte[] digest = await sha256.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);

                    var builder = new StringBuilder(digest.Length * 2);
                    foreach (byte value in digest)
                    {
                        builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                    }

                    return builder.ToString();
                }
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is System.Security.SecurityException)
            {
                // The executable cannot be read. That is reported as an unverifiable engine, which
                // blocks execution, rather than being treated as a passing check.
                return null;
            }
        }
    }
}
