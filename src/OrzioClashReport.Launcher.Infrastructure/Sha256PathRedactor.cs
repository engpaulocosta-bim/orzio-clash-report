using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Infrastructure
{
    /// <summary>
    /// Reduces a path to file name, extension, SHA-256, and root kind. The absolute path never leaves
    /// this type, which is what keeps client directory structures out of the default log.
    /// </summary>
    public sealed class Sha256PathRedactor : IPathRedactor
    {
        private readonly string _localApplicationDataRoot;
        private readonly string _userProfileRoot;
        private readonly string _programInstallationRoot;
        private readonly string _temporaryRoot;

        public Sha256PathRedactor()
            : this(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify),
                AppContext.BaseDirectory,
                Path.GetTempPath())
        {
        }

        public Sha256PathRedactor(
            string localApplicationDataRoot,
            string userProfileRoot,
            string programInstallationRoot,
            string temporaryRoot)
        {
            _localApplicationDataRoot = Normalize(localApplicationDataRoot);
            _userProfileRoot = Normalize(userProfileRoot);
            _programInstallationRoot = Normalize(programInstallationRoot);
            _temporaryRoot = Normalize(temporaryRoot);
        }

        public RedactedPath Redact(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            string fileName = SafeFileName(path);
            string extension = SafeExtension(path);

            return new RedactedPath(fileName, extension, ComputeHash(path), ClassifyRoot(path));
        }

        private static string SafeFileName(string path)
        {
            try
            {
                return Path.GetFileName(path) ?? string.Empty;
            }
            catch (ArgumentException)
            {
                // A path the platform rejects still deserves a log line, just without a name.
                return string.Empty;
            }
        }

        private static string SafeExtension(string path)
        {
            try
            {
                return Path.GetExtension(path) ?? string.Empty;
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }

        private static string ComputeHash(string path)
        {
            byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(path));
            var builder = new StringBuilder(digest.Length * 2);
            foreach (byte value in digest)
            {
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private PathRootKind ClassifyRoot(string path)
        {
            if (path.Length == 0)
            {
                return PathRootKind.Unknown;
            }

            // A UNC prefix is recognized before rootedness, because a non-Windows runtime does not
            // treat a backslash-prefixed UNC path as rooted.
            if (path.StartsWith("\\\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
            {
                return PathRootKind.Network;
            }

            if (!Path.IsPathRooted(path))
            {
                return PathRootKind.Relative;
            }

            string normalized = Normalize(path);

            // Most specific root first: local application data and the temporary directory both
            // usually live under the user profile.
            if (IsUnder(normalized, _localApplicationDataRoot))
            {
                return PathRootKind.LocalApplicationData;
            }

            if (IsUnder(normalized, _temporaryRoot))
            {
                return PathRootKind.Temporary;
            }

            if (IsUnder(normalized, _programInstallationRoot))
            {
                return PathRootKind.ProgramInstallation;
            }

            if (IsUnder(normalized, _userProfileRoot))
            {
                return PathRootKind.UserProfile;
            }

            return PathRootKind.OtherLocal;
        }

        private static bool IsUnder(string candidate, string root)
        {
            if (root.Length == 0)
            {
                return false;
            }

            StringComparison comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return candidate.StartsWith(root, comparison);
        }

        private static string Normalize(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string trimmed = path.Trim();
            string separator = Path.DirectorySeparatorChar.ToString();
            trimmed = trimmed.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return trimmed.EndsWith(separator, StringComparison.Ordinal) ? trimmed : trimmed + separator;
        }
    }
}
