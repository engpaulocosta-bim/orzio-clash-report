using System;

namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>Outcome of comparing the installed engine's SHA-256 against the packaged manifest.</summary>
    public enum EngineIntegrityStatus
    {
        /// <summary>The computed hash matched the manifest exactly.</summary>
        Verified = 0,

        /// <summary>The computed hash did not match the manifest.</summary>
        HashMismatch = 1,

        /// <summary>The engine executable was not found.</summary>
        EngineMissing = 2,

        /// <summary>No engine manifest was found next to the engine.</summary>
        ManifestMissing = 3,

        /// <summary>The manifest exists but could not be read as the expected format.</summary>
        ManifestUnreadable = 4,
    }

    /// <summary>One immutable engine integrity verification result.</summary>
    public sealed class EngineIntegrityResult
    {
        private EngineIntegrityResult(
            EngineIntegrityStatus status, string? expectedSha256, string? actualSha256, string? detail)
        {
            Status = status;
            ExpectedSha256 = expectedSha256;
            ActualSha256 = actualSha256;
            Detail = detail;
        }

        public EngineIntegrityStatus Status { get; }

        /// <summary>Lowercase hex digest declared by the packaged engine manifest.</summary>
        public string? ExpectedSha256 { get; }

        /// <summary>Lowercase hex digest computed over the installed executable.</summary>
        public string? ActualSha256 { get; }

        /// <summary>A short, already redacted explanation suitable for display.</summary>
        public string? Detail { get; }

        public bool IsVerified => Status == EngineIntegrityStatus.Verified;

        public static EngineIntegrityResult Verified(string sha256)
        {
            string normalized = Normalize(sha256, nameof(sha256));
            return new EngineIntegrityResult(EngineIntegrityStatus.Verified, normalized, normalized, detail: null);
        }

        public static EngineIntegrityResult HashMismatch(string expectedSha256, string actualSha256)
        {
            return new EngineIntegrityResult(
                EngineIntegrityStatus.HashMismatch,
                Normalize(expectedSha256, nameof(expectedSha256)),
                Normalize(actualSha256, nameof(actualSha256)),
                detail: null);
        }

        public static EngineIntegrityResult Failed(EngineIntegrityStatus status, string detail)
        {
            if (status == EngineIntegrityStatus.Verified || status == EngineIntegrityStatus.HashMismatch)
            {
                throw new ArgumentException(
                    "Verified and HashMismatch carry digests and have dedicated factories.", nameof(status));
            }

            if (string.IsNullOrWhiteSpace(detail))
            {
                throw new ArgumentException("Detail must not be empty or whitespace.", nameof(detail));
            }

            return new EngineIntegrityResult(status, expectedSha256: null, actualSha256: null, detail.Trim());
        }

        private static string Normalize(string digest, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(digest))
            {
                throw new ArgumentException("Digest must not be empty or whitespace.", parameterName);
            }

            return digest.Trim().ToLowerInvariant();
        }
    }
}
