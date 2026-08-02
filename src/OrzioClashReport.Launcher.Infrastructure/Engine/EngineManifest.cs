using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// The manifest the packaging script writes next to the published engine. Its SHA-256 is computed
    /// over the executable that was actually published; a hash written by hand is never acceptable.
    /// </summary>
    public sealed class EngineManifest
    {
        [JsonConstructor]
        public EngineManifest(int schemaVersion, string engineVersion, string executableFileName, string sha256)
        {
            SchemaVersion = schemaVersion;
            EngineVersion = engineVersion;
            ExecutableFileName = executableFileName;
            Sha256 = sha256;
        }

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; }

        [JsonPropertyName("engineVersion")]
        public string EngineVersion { get; }

        [JsonPropertyName("executableFileName")]
        public string ExecutableFileName { get; }

        [JsonPropertyName("sha256")]
        public string Sha256 { get; }

        public const int SupportedSchemaVersion = 1;
    }

    /// <summary>
    /// Compares the installed engine's SHA-256 against the packaged manifest. A launcher that cannot
    /// prove which engine it is about to run says so instead of running it.
    /// </summary>
    public sealed class EngineManifestIntegrityVerifier : IEngineIntegrityVerifier
    {
        private static readonly JsonSerializerOptions ReadOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
        };

        private readonly EngineLayout _layout;

        public EngineManifestIntegrityVerifier(EngineLayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        public EngineIntegrityResult Verify()
        {
            if (!File.Exists(_layout.ExecutablePath))
            {
                return EngineIntegrityResult.Failed(
                    EngineIntegrityStatus.EngineMissing,
                    "No engine executable was found in the installation's engine directory.");
            }

            if (!File.Exists(_layout.ManifestPath))
            {
                return EngineIntegrityResult.Failed(
                    EngineIntegrityStatus.ManifestMissing,
                    "No engine manifest was found beside the engine executable.");
            }

            EngineManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<EngineManifest>(
                    File.ReadAllText(_layout.ManifestPath), ReadOptions);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                return EngineIntegrityResult.Failed(
                    EngineIntegrityStatus.ManifestUnreadable,
                    "The engine manifest could not be read: " + ex.Message);
            }

            if (manifest == null || string.IsNullOrWhiteSpace(manifest.Sha256))
            {
                return EngineIntegrityResult.Failed(
                    EngineIntegrityStatus.ManifestUnreadable,
                    "The engine manifest does not declare a SHA-256 digest.");
            }

            if (manifest.SchemaVersion != EngineManifest.SupportedSchemaVersion)
            {
                return EngineIntegrityResult.Failed(
                    EngineIntegrityStatus.ManifestUnreadable,
                    "The engine manifest declares schema version "
                        + manifest.SchemaVersion.ToString(CultureInfo.InvariantCulture)
                        + ", which this launcher does not support.");
            }

            string actual;
            try
            {
                actual = ComputeSha256(_layout.ExecutablePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return EngineIntegrityResult.Failed(
                    EngineIntegrityStatus.ManifestUnreadable,
                    "The engine executable could not be read for verification: " + ex.Message);
            }

            string expected = manifest.Sha256.Trim().ToLowerInvariant();
            return string.Equals(expected, actual, StringComparison.Ordinal)
                ? EngineIntegrityResult.Verified(actual)
                : EngineIntegrityResult.HashMismatch(expected, actual);
        }

        /// <summary>Reads the declared engine version, or <c>null</c> when the manifest is unusable.</summary>
        public string? TryReadDeclaredVersion()
        {
            if (!File.Exists(_layout.ManifestPath))
            {
                return null;
            }

            try
            {
                EngineManifest? manifest = JsonSerializer.Deserialize<EngineManifest>(
                    File.ReadAllText(_layout.ManifestPath), ReadOptions);

                return string.IsNullOrWhiteSpace(manifest?.EngineVersion) ? null : manifest!.EngineVersion.Trim();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // An unreadable manifest is reported by Verify; here it simply means no expectation.
                return null;
            }
        }

        internal static string ComputeSha256(string filePath)
        {
            using FileStream stream = File.OpenRead(filePath);
            byte[] digest = SHA256.HashData(stream);
            return Convert.ToHexString(digest).ToLowerInvariant();
        }
    }
}
