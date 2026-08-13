using System;

namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// Immutable result of one engine integrity verification. Hashes are lower-case hexadecimal
    /// SHA-256 digests, or <c>null</c> when the corresponding side could not be computed or read.
    /// </summary>
    public sealed class EngineIntegrityResult
    {
        public EngineIntegrityVerdict Verdict { get; }
        public string? ExpectedSha256 { get; }
        public string? ActualSha256 { get; }

        public EngineIntegrityResult(EngineIntegrityVerdict verdict, string? expectedSha256, string? actualSha256)
        {
            if (!Enum.IsDefined(typeof(EngineIntegrityVerdict), verdict))
            {
                throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown integrity verdict.");
            }

            if (verdict == EngineIntegrityVerdict.Verified
                && (string.IsNullOrEmpty(expectedSha256) || string.IsNullOrEmpty(actualSha256)))
            {
                throw new ArgumentException(
                    "A verified integrity result requires both the expected and the actual digest.", nameof(verdict));
            }

            Verdict = verdict;
            ExpectedSha256 = expectedSha256;
            ActualSha256 = actualSha256;
        }

        public static EngineIntegrityResult NotChecked { get; } =
            new EngineIntegrityResult(EngineIntegrityVerdict.NotChecked, null, null);
    }
}
