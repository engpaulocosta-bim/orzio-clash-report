using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OrzioClashReport.Launcher.Contracts.Logging;

namespace OrzioClashReport.Launcher.Infrastructure.Logging
{
    /// <summary>
    /// The only path-to-log conversion in the launcher. It keeps the file name, the extension, a
    /// SHA-256 of the full path for correlation, and the class of location; everything that could
    /// identify a client, a project, a share, or a person is dropped before anything is written.
    /// </summary>
    public sealed class Sha256PathRedactor : IPathRedactor
    {
        private readonly string _localApplicationDataRoot;
        private readonly string _userProfileRoot;
        private readonly string _installationRoot;
        private readonly string _temporaryRoot;

        public Sha256PathRedactor(
            string? localApplicationDataRoot = null,
            string? userProfileRoot = null,
            string? installationRoot = null,
            string? temporaryRoot = null)
        {
            _localApplicationDataRoot = Normalize(localApplicationDataRoot
                ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            _userProfileRoot = Normalize(userProfileRoot
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            _installationRoot = Normalize(installationRoot ?? AppContext.BaseDirectory);
            _temporaryRoot = Normalize(temporaryRoot ?? Path.GetTempPath());
        }

        public RedactedPath Redact(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            string fileName;
            string extension;

            try
            {
                fileName = Path.GetFileName(path) ?? string.Empty;
                extension = Path.GetExtension(path) ?? string.Empty;
            }
            catch (ArgumentException)
            {
                // A path the platform rejects still has to be loggable, and its raw text must not leak.
                // Recording it with an empty name plus its hash keeps the entry honest and safe.
                fileName = string.Empty;
                extension = string.Empty;
            }

            return new RedactedPath(fileName, extension, ComputeHash(path), ClassifyRoot(path));
        }

        private PathRootKind ClassifyRoot(string path)
        {
            string normalized;
            try
            {
                normalized = Normalize(Path.GetFullPath(path));
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException
                || exception is IOException
                || exception is System.Security.SecurityException)
            {
                // The path cannot be resolved, so its class genuinely is unknown. Reporting Unknown is
                // accurate; guessing from the raw text would not be.
                return PathRootKind.Unknown;
            }

            if (normalized.StartsWith(@"\\", StringComparison.Ordinal)
                || normalized.StartsWith("//", StringComparison.Ordinal))
            {
                return PathRootKind.NetworkShare;
            }

            // Order matters: the installation directory and the launcher's own data directory both sit
            // inside broader roots, so the most specific classification has to be tested first.
            if (IsUnder(normalized, _installationRoot))
            {
                return PathRootKind.InstallationDirectory;
            }

            if (IsUnder(normalized, _temporaryRoot))
            {
                return PathRootKind.TemporaryDirectory;
            }

            if (IsUnder(normalized, _localApplicationDataRoot))
            {
                return PathRootKind.LocalApplicationData;
            }

            if (IsUnder(normalized, _userProfileRoot))
            {
                return PathRootKind.UserProfile;
            }

            return PathRootKind.OtherLocalVolume;
        }

        private static bool IsUnder(string candidate, string root)
        {
            if (root.Length == 0)
            {
                return false;
            }

            if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return candidate.Length == root.Length
                || candidate[root.Length] == Path.DirectorySeparatorChar
                || candidate[root.Length] == Path.AltDirectorySeparatorChar;
        }

        private static string Normalize(string? root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return string.Empty;
            }

            return root!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string ComputeHash(string path)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(path));
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
