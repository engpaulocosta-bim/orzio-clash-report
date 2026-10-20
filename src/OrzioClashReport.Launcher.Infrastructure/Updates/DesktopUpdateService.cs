using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Infrastructure.Updates
{
    public sealed record DesktopUpdateManifest(string Version, string InstallerUrl, string Sha256, string? ReleaseNotesUrl);

    public sealed record DesktopUpdateCheckResult(bool UpdateAvailable, string CurrentVersion, DesktopUpdateManifest? Update);

    /// <summary>Only contacts the public, official GitHub releases endpoint after a user request.</summary>
    public sealed class DesktopUpdateService
    {
        private const string Repository = "engpaulocosta-bim/orzio-clash-report";
        private const string ReleasePrefix = "desktop-v";
        private const string ManifestAssetName = "orzio-clash-update-manifest.json";
        private const string ReleaseApi = "https://api.github.com/repos/" + Repository + "/releases?per_page=100";
        private const long MaximumInstallerBytes = 250L * 1024L * 1024L;
        private readonly HttpClient _http;

        public DesktopUpdateService() : this(CreateHttpClient())
        {
        }

        internal DesktopUpdateService(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        private static HttpClient CreateHttpClient()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OrzioClashReport", "1.0"));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return http;
        }

        public async Task<DesktopUpdateCheckResult> CheckAsync(string currentVersion, CancellationToken cancellationToken = default)
        {
            if (!DesktopVersionComparer.IsValid(currentVersion))
            {
                throw new ArgumentException("Invalid installed launcher version.", nameof(currentVersion));
            }

            using HttpResponseMessage releasesResponse = await _http.GetAsync(ReleaseApi, cancellationToken)
                .ConfigureAwait(false);
            releasesResponse.EnsureSuccessStatusCode();
            await using Stream releasesStream = await releasesResponse.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using JsonDocument releases = await JsonDocument.ParseAsync(releasesStream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (releases.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("GitHub returned an unexpected releases response.");
            }

            string? bestTag = null;
            string? bestVersion = null;
            string? manifestUrl = null;
            foreach (JsonElement release in releases.RootElement.EnumerateArray())
            {
                if (!release.TryGetProperty("draft", out JsonElement draft) || draft.ValueKind != JsonValueKind.False
                    || !release.TryGetProperty("tag_name", out JsonElement tagElement))
                {
                    continue;
                }

                string? tag = tagElement.GetString();
                if (tag is null || !tag.StartsWith(ReleasePrefix, StringComparison.Ordinal))
                {
                    continue; // Ignore the separate engine / CLI release channel.
                }

                string version = tag.Substring(ReleasePrefix.Length);
                if (!DesktopVersionComparer.IsValid(version)
                    || !DesktopVersionComparer.IsNewer(version, currentVersion)
                    || (bestVersion != null && !DesktopVersionComparer.IsNewer(version, bestVersion)))
                {
                    continue;
                }

                if (!release.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement asset in assets.EnumerateArray())
                {
                    if (!asset.TryGetProperty("name", out JsonElement name)
                        || name.GetString() != ManifestAssetName
                        || !asset.TryGetProperty("browser_download_url", out JsonElement url))
                    {
                        continue;
                    }

                    string? candidate = url.GetString();
                    string expectedManifestUrl = $"https://github.com/{Repository}/releases/download/{tag}/{ManifestAssetName}";
                    if (!string.Equals(candidate, expectedManifestUrl, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    bestTag = tag;
                    bestVersion = version;
                    manifestUrl = candidate;
                    break;
                }
            }

            if (manifestUrl is null)
            {
                return new DesktopUpdateCheckResult(false, currentVersion, null);
            }

            using HttpResponseMessage manifestResponse = await _http.GetAsync(manifestUrl, cancellationToken)
                .ConfigureAwait(false);
            manifestResponse.EnsureSuccessStatusCode();
            await using Stream manifestStream = await manifestResponse.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            DesktopUpdateManifest? manifest = await JsonSerializer.DeserializeAsync<DesktopUpdateManifest>(
                manifestStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken)
                .ConfigureAwait(false);

            if (manifest is null || !string.Equals(manifest.Version, bestVersion, StringComparison.Ordinal)
                || !IsTrustedInstaller(manifest) || string.IsNullOrWhiteSpace(manifest.Sha256)
                || !Regex.IsMatch(manifest.Sha256, @"\A[0-9a-fA-F]{64}\z"))
            {
                throw new InvalidDataException($"Release {bestTag} has an invalid update manifest.");
            }

            return new DesktopUpdateCheckResult(true, currentVersion, manifest);
        }

        private static bool IsTrustedInstaller(DesktopUpdateManifest manifest)
        {
            if (!DesktopVersionComparer.IsValid(manifest.Version))
            {
                return false;
            }

            string fileName = $"orzio-clash-report-desktop-v{manifest.Version}-win-x64-setup.exe";
            string expectedUrl = $"https://github.com/{Repository}/releases/download/{ReleasePrefix}{manifest.Version}/{fileName}";
            return string.Equals(manifest.InstallerUrl, expectedUrl, StringComparison.Ordinal);
        }

        /// <summary>Downloads to local user data, verifies SHA-256, then opens the normal installer wizard.</summary>
        public async Task<string> DownloadAndStartInstallerAsync(DesktopUpdateManifest manifest, CancellationToken cancellationToken = default)
        {
            if (!IsTrustedInstaller(manifest) || manifest.Sha256 is null
                || !Regex.IsMatch(manifest.Sha256, @"\A[0-9a-fA-F]{64}\z"))
            {
                throw new InvalidDataException("Update manifest failed validation.");
            }

            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Desktop updates require Windows.");
            }

            string updates = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Orzio", "ClashReportLauncher", "Updates");
            Directory.CreateDirectory(updates);
            string fileName = $"orzio-clash-report-desktop-v{manifest.Version}-win-x64-setup.exe";
            string installer = Path.Combine(updates, fileName);
            string temporary = Path.Combine(updates, Guid.NewGuid().ToString("N") + ".download");

            try
            {
                using HttpResponseMessage response = await _http.GetAsync(
                    manifest.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength > MaximumInstallerBytes)
                {
                    throw new InvalidDataException("Installer exceeds the permitted size.");
                }

                await using (Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                await using (FileStream destination = new FileStream(
                    temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    byte[] buffer = new byte[81920];
                    long written = 0;
                    int read;
                    while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        written += read;
                        if (written > MaximumInstallerBytes)
                        {
                            throw new InvalidDataException("Installer exceeds the permitted size.");
                        }

                        await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    }
                }

                await using FileStream verification = File.OpenRead(temporary);
                byte[] hash = await SHA256.HashDataAsync(verification, cancellationToken).ConfigureAwait(false);
                string actual = Convert.ToHexString(hash);
                if (!actual.Equals(manifest.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Downloaded installer SHA-256 does not match the release.");
                }

                File.Move(temporary, installer, overwrite: true);
                using var started = System.Diagnostics.Process.Start(new ProcessStartInfo(installer) { UseShellExecute = false });
                if (started is null)
                {
                    throw new InvalidOperationException("Windows did not start the installer.");
                }
                return installer;
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }
    }

    internal static class DesktopVersionComparer
    {
        private static readonly Regex Pattern = new Regex(
            @"\A[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?\z",
            RegexOptions.CultureInvariant);

        internal static bool IsValid(string? value) => value != null && Pattern.IsMatch(value);

        internal static bool IsNewer(string candidate, string current) => Compare(candidate, current) > 0;

        internal static int Compare(string a, string b)
        {
            if (!IsValid(a) || !IsValid(b))
            {
                throw new ArgumentException("Only valid semantic version identifiers can be compared.");
            }

            string[] left = a.Split('-', 2);
            string[] right = b.Split('-', 2);
            Version leftCore = Version.Parse(left[0]);
            Version rightCore = Version.Parse(right[0]);
            int comparison = leftCore.CompareTo(rightCore);
            if (comparison != 0) return comparison;
            if (left.Length == 1) return right.Length == 1 ? 0 : 1;
            if (right.Length == 1) return -1;

            string[] leftParts = left[1].Replace('-', '.').Split('.');
            string[] rightParts = right[1].Replace('-', '.').Split('.');
            for (int i = 0; i < Math.Min(leftParts.Length, rightParts.Length); i++)
            {
                bool leftNumber = long.TryParse(leftParts[i], out long x);
                bool rightNumber = long.TryParse(rightParts[i], out long y);
                comparison = leftNumber && rightNumber ? x.CompareTo(y)
                    : leftNumber ? -1
                    : rightNumber ? 1
                    : string.CompareOrdinal(leftParts[i], rightParts[i]);
                if (comparison != 0) return comparison;
            }

            return leftParts.Length.CompareTo(rightParts.Length);
        }
    }
}
