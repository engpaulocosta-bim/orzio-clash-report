using System;
using System.Text.RegularExpressions;

namespace OrzioClashReport.Launcher.Application.Engine
{
    /// <summary>
    /// Reads the engine's published <c>--version</c> contract. That contract is exactly one line,
    /// <c>orzioclash &lt;major&gt;.&lt;minor&gt;.&lt;patch&gt;-&lt;suffix&gt;</c>, and this parser accepts
    /// nothing else: anything unexpected is an unsupported engine, never a guess.
    /// </summary>
    public static class EngineVersionParser
    {
        /// <summary>
        /// The published version line, anchored. It mirrors the check the release workflow performs
        /// against the tag, so the launcher and the release pipeline agree on what a version looks like.
        /// </summary>
        public const string VersionPattern = @"^orzioclash (\d+\.\d+\.\d+-[A-Za-z0-9.]+)$";

        private static readonly Regex VersionRegex =
            new Regex(VersionPattern, RegexOptions.CultureInvariant);

        /// <summary>
        /// Extracts the version from raw engine stdout. Surrounding blank lines and a trailing carriage
        /// return are tolerated because they are transport artifacts, not content; any other extra
        /// output is rejected.
        /// </summary>
        public static bool TryParse(string? standardOutput, out string version)
        {
            version = string.Empty;

            if (string.IsNullOrWhiteSpace(standardOutput))
            {
                return false;
            }

            string[] lines = standardOutput!.Split('\n');
            string? candidate = null;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }

                if (candidate != null)
                {
                    // The contract is a single line. A second non-empty line means this is not the
                    // engine's published version output.
                    return false;
                }

                candidate = line;
            }

            if (candidate == null)
            {
                return false;
            }

            Match match = VersionRegex.Match(candidate);
            if (!match.Success)
            {
                return false;
            }

            version = match.Groups[1].Value;
            return true;
        }

        /// <summary>
        /// Compares an observed version against the version this launcher build was packaged with.
        /// Comparison is ordinal: a preview suffix is part of the identity, never something to round off.
        /// </summary>
        public static bool Matches(string observedVersion, string expectedVersion)
        {
            if (observedVersion == null)
            {
                throw new ArgumentNullException(nameof(observedVersion));
            }

            if (expectedVersion == null)
            {
                throw new ArgumentNullException(nameof(expectedVersion));
            }

            return string.Equals(observedVersion, expectedVersion, StringComparison.Ordinal);
        }
    }
}
