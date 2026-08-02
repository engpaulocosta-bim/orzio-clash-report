using System.Security.Cryptography;
using System.Text;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Infrastructure.Engine;

namespace OrzioClashReport.Launcher.Tests;

public sealed class EngineIntegrityTests : IDisposable
{
    private readonly string _installation;

    public EngineIntegrityTests()
    {
        _installation = Path.Combine(Path.GetTempPath(), "orzio-engine-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_installation);
    }

    public void Dispose()
    {
        if (Directory.Exists(_installation))
        {
            Directory.Delete(_installation, recursive: true);
        }
    }

    private EngineLayout Layout => new(_installation);

    [Fact]
    public void EngineLayout_PlacesTheEngineUnderEngineWinX64()
    {
        EngineLayout layout = Layout;

        Assert.Equal(
            Path.Combine(_installation, "engine", "win-x64"),
            layout.EngineDirectory);
        Assert.Equal(Path.Combine(layout.EngineDirectory, "engine-manifest.json"), layout.ManifestPath);
        Assert.Equal(Path.Combine(_installation, "samples"), layout.SamplesDirectory);
        Assert.StartsWith(layout.EngineDirectory, layout.ExecutablePath, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingEngine_IsReportedRatherThanAssumedFine()
    {
        EngineIntegrityResult result = new EngineManifestIntegrityVerifier(Layout).Verify();

        Assert.Equal(EngineIntegrityStatus.EngineMissing, result.Status);
        Assert.False(result.IsVerified);
    }

    [Fact]
    public void AMissingManifest_IsReportedSeparatelyFromAMissingEngine()
    {
        WriteEngine("engine bytes");

        EngineIntegrityResult result = new EngineManifestIntegrityVerifier(Layout).Verify();

        Assert.Equal(EngineIntegrityStatus.ManifestMissing, result.Status);
    }

    [Fact]
    public void AMatchingDigest_Verifies()
    {
        string content = "engine bytes";
        WriteEngine(content);
        WriteManifest("0.1.0-preview.3", Sha256Of(content));

        EngineIntegrityResult result = new EngineManifestIntegrityVerifier(Layout).Verify();

        Assert.True(result.IsVerified);
        Assert.Equal(Sha256Of(content), result.ActualSha256);
        Assert.Equal(result.ExpectedSha256, result.ActualSha256);
    }

    [Fact]
    public void ATamperedEngine_ProducesAHashMismatchWithBothDigests()
    {
        WriteEngine("engine bytes");
        WriteManifest("0.1.0-preview.3", Sha256Of("different bytes"));

        EngineIntegrityResult result = new EngineManifestIntegrityVerifier(Layout).Verify();

        Assert.Equal(EngineIntegrityStatus.HashMismatch, result.Status);
        Assert.Equal(Sha256Of("different bytes"), result.ExpectedSha256);
        Assert.Equal(Sha256Of("engine bytes"), result.ActualSha256);
    }

    [Fact]
    public void AnUnreadableManifest_IsReportedRatherThanIgnored()
    {
        WriteEngine("engine bytes");
        File.WriteAllText(Layout.ManifestPath, "{ this is not json");

        EngineIntegrityResult result = new EngineManifestIntegrityVerifier(Layout).Verify();

        Assert.Equal(EngineIntegrityStatus.ManifestUnreadable, result.Status);
        Assert.NotNull(result.Detail);
    }

    [Fact]
    public void AnUnknownSchemaVersion_IsRefusedRatherThanGuessed()
    {
        string content = "engine bytes";
        WriteEngine(content);
        File.WriteAllText(
            Layout.ManifestPath,
            $$"""
              {
                "schemaVersion": 99,
                "engineVersion": "0.1.0-preview.3",
                "executableFileName": "orzioclash.exe",
                "sha256": "{{Sha256Of(content)}}"
              }
              """);

        EngineIntegrityResult result = new EngineManifestIntegrityVerifier(Layout).Verify();

        Assert.Equal(EngineIntegrityStatus.ManifestUnreadable, result.Status);
    }

    [Fact]
    public void TheDeclaredVersion_IsReadFromTheManifest()
    {
        WriteEngine("engine bytes");
        WriteManifest("0.1.0-preview.3", Sha256Of("engine bytes"));

        Assert.Equal("0.1.0-preview.3", new EngineManifestIntegrityVerifier(Layout).TryReadDeclaredVersion());
    }

    [Fact]
    public void TheVersionPattern_AcceptsOnlyThePublishedVersionLine()
    {
        Assert.Matches(CliEngineProbe.VersionPattern, MatchTarget("orzioclash 0.1.0-preview.3"));
        Assert.Matches(CliEngineProbe.VersionPattern, MatchTarget("orzioclash 12.4.9-launcher.preview.1"));

        Assert.DoesNotMatch(CliEngineProbe.VersionPattern, MatchTarget("orzioclash 0.1.0"));
        Assert.DoesNotMatch(CliEngineProbe.VersionPattern, MatchTarget("orzioclash  0.1.0-preview.3"));
        Assert.DoesNotMatch(CliEngineProbe.VersionPattern, MatchTarget("Orzioclash 0.1.0-preview.3"));
        Assert.DoesNotMatch(CliEngineProbe.VersionPattern, MatchTarget("something orzioclash 0.1.0-preview.3"));
        Assert.DoesNotMatch(CliEngineProbe.VersionPattern, MatchTarget("orzioclash 0.1.0-preview.3 extra"));
        Assert.DoesNotMatch(CliEngineProbe.VersionPattern, MatchTarget(string.Empty));
    }

    [Fact]
    public void TheVersionPattern_CapturesTheVersionOnly()
    {
        Assert.Equal(
            "0.1.0-preview.3",
            CliEngineProbe.VersionPattern.Match("orzioclash 0.1.0-preview.3").Groups["version"].Value);
    }

    [Fact]
    public void TheProbe_ReadsOnlyTheFirstLine()
    {
        Assert.Equal("orzioclash 0.1.0-preview.3", CliEngineProbe.FirstLine("orzioclash 0.1.0-preview.3\r\ntrailing"));
        Assert.Equal("orzioclash 0.1.0-preview.3", CliEngineProbe.FirstLine("orzioclash 0.1.0-preview.3\n"));
        Assert.Equal(string.Empty, CliEngineProbe.FirstLine(string.Empty));
    }

    [Fact]
    public void TheProbeTimeout_IsFiveSeconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), CliEngineProbe.ProbeTimeout);
    }

    private static string MatchTarget(string value) => value;

    private void WriteEngine(string content)
    {
        Directory.CreateDirectory(Layout.EngineDirectory);
        File.WriteAllText(Layout.ExecutablePath, content);
    }

    private void WriteManifest(string version, string sha256)
    {
        Directory.CreateDirectory(Layout.EngineDirectory);
        File.WriteAllText(
            Layout.ManifestPath,
            $$"""
              {
                "schemaVersion": 1,
                "engineVersion": "{{version}}",
                "executableFileName": "orzioclash.exe",
                "sha256": "{{sha256}}"
              }
              """);
    }

    private static string Sha256Of(string content)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }
}
