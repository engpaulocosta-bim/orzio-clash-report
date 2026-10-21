using System.Net;
using System.Net.Http;
using System.Text;
using OrzioClashReport.Launcher.Infrastructure.Updates;

namespace OrzioClashReport.Launcher.Tests;

public sealed class DesktopUpdateServiceTests
{
    private const string Repo = "engpaulocosta-bim/orzio-clash-report";
    private const string ManifestName = "orzio-clash-update-manifest.json";

    [Theory]
    [InlineData("0.2.2", "0.2.1", true)]
    [InlineData("0.2.1-launcher-preview.10", "0.2.1-launcher-preview.9", true)]
    [InlineData("0.2.1", "0.2.1-launcher-preview.10", true)]
    [InlineData("0.2.1-launcher-preview.1", "0.2.1", false)]
    [InlineData("0.2.1-launcher-preview.9", "0.2.1-launcher-preview.10", false)]
    [InlineData("0.2.1", "0.2.1", false)]
    public void Semantic_version_comparison_respects_prereleases(string latest, string current, bool expected)
    {
        Assert.Equal(expected, DesktopVersionComparer.IsNewer(latest, current));
    }

    [Fact]
    public async Task Finds_newer_desktop_release_and_ignores_engine_releases()
    {
        string version = "0.2.1-launcher-preview.2";
        string tag = "desktop-v" + version;
        string manifestUrl = $"https://github.com/{Repo}/releases/download/{tag}/{ManifestName}";
        string installerUrl = $"https://github.com/{Repo}/releases/download/{tag}/orzio-clash-report-desktop-v{version}-win-x64-setup.exe";
        using var http = new HttpClient(new FakeHandler(request =>
        {
            string json = request.RequestUri!.AbsolutePath.EndsWith("/releases", StringComparison.Ordinal)
                ? $$"""[{"draft":false,"tag_name":"v0.1.0-preview.3","assets":[]},{"draft":false,"tag_name":"{{tag}}","assets":[{"name":"{{ManifestName}}","browser_download_url":"{{manifestUrl}}"}]}]"""
                : $$"""{"version":"{{version}}","installerUrl":"{{installerUrl}}","sha256":"{{new string('a', 64)}}","releaseNotesUrl":"https://github.com/{{Repo}}/releases/tag/{{tag}}"}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }));

        var service = new DesktopUpdateService(http);
        DesktopUpdateCheckResult result = await service.CheckAsync("0.2.1-launcher-preview.1");

        Assert.True(result.UpdateAvailable);
        Assert.Equal(version, result.Update?.Version);
        Assert.Equal(installerUrl, result.Update?.InstallerUrl);
    }

    [Fact]
    public async Task Never_offers_an_older_or_equal_version()
    {
        using var http = new HttpClient(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """[{"draft":false,"tag_name":"desktop-v0.2.1-launcher-preview.1","assets":[]}]""")
        }));
        var service = new DesktopUpdateService(http);

        DesktopUpdateCheckResult result = await service.CheckAsync("0.2.1-launcher-preview.1");

        Assert.False(result.UpdateAvailable);
        Assert.Null(result.Update);
    }

    [Fact]
    public async Task Rejects_an_installer_download_outside_the_official_release()
    {
        const string version = "0.2.1-launcher-preview.2";
        string tag = "desktop-v" + version;
        string manifestUrl = $"https://github.com/{Repo}/releases/download/{tag}/{ManifestName}";
        using var http = new HttpClient(new FakeHandler(request =>
        {
            string json = request.RequestUri!.AbsolutePath.EndsWith("/releases", StringComparison.Ordinal)
                ? $$"""[{"draft":false,"tag_name":"{{tag}}","assets":[{"name":"{{ManifestName}}","browser_download_url":"{{manifestUrl}}"}]}]"""
                : $$"""{"version":"{{version}}","installerUrl":"https://untrusted.example/installer.exe","sha256":"{{new string('a', 64)}}"}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        }));

        var service = new DesktopUpdateService(http);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.CheckAsync("0.2.1-launcher-preview.1"));
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
