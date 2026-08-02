using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Infrastructure;

namespace OrzioClashReport.Launcher.Tests;

public sealed class PathRedactionTests
{
    private static readonly string LocalAppData = Path.Combine(Path.GetTempPath(), "redaction", "localappdata");
    private static readonly string UserProfile = Path.Combine(Path.GetTempPath(), "redaction", "profile");
    private static readonly string Installation = Path.Combine(Path.GetTempPath(), "redaction", "program");
    private static readonly string Temporary = Path.Combine(Path.GetTempPath(), "redaction", "temp");

    private static Sha256PathRedactor Redactor =>
        new(LocalAppData, UserProfile, Installation, Temporary);

    [Fact]
    public void Redaction_KeepsNoDirectoryAndNoAbsolutePath()
    {
        string path = Path.Combine(UserProfile, "Clients", "Acme Tower", "clash-export.xml");

        RedactedPath redacted = Redactor.Redact(path);

        Assert.Equal("clash-export.xml", redacted.FileName);
        Assert.Equal(".xml", redacted.Extension);
        Assert.DoesNotContain("Acme", redacted.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Acme", redacted.PathHash, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Path.DirectorySeparatorChar.ToString(), redacted.FileName, StringComparison.Ordinal);
    }

    [Fact]
    public void SamePath_ProducesTheSameHashSoLinesCorrelate()
    {
        string path = Path.Combine(UserProfile, "report.html");

        Assert.Equal(Redactor.Redact(path).PathHash, Redactor.Redact(path).PathHash);
    }

    [Fact]
    public void DifferentPaths_ProduceDifferentHashes()
    {
        string first = Path.Combine(UserProfile, "a.html");
        string second = Path.Combine(UserProfile, "b.html");

        Assert.NotEqual(Redactor.Redact(first).PathHash, Redactor.Redact(second).PathHash);
    }

    [Fact]
    public void Hash_IsLowercaseHexSha256()
    {
        RedactedPath redacted = Redactor.Redact(Path.Combine(UserProfile, "a.html"));

        Assert.Equal(64, redacted.PathHash.Length);
        Assert.All(redacted.PathHash, c => Assert.True(
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'), $"Unexpected hash character '{c}'."));
    }

    [Fact]
    public void LocalApplicationData_WinsOverTheEnclosingUserProfile()
    {
        var redactor = new Sha256PathRedactor(
            Path.Combine(UserProfile, "AppData", "Local"), UserProfile, Installation, Temporary);

        RedactedPath redacted = redactor.Redact(
            Path.Combine(UserProfile, "AppData", "Local", "Orzio", "settings.json"));

        Assert.Equal(PathRootKind.LocalApplicationData, redacted.PathRootKind);
    }

    [Fact]
    public void RootKinds_AreClassifiedFromTheKnownRoots()
    {
        Assert.Equal(
            PathRootKind.LocalApplicationData,
            Redactor.Redact(Path.Combine(LocalAppData, "logs", "a.jsonl")).PathRootKind);
        Assert.Equal(
            PathRootKind.Temporary,
            Redactor.Redact(Path.Combine(Temporary, "a.tmp")).PathRootKind);
        Assert.Equal(
            PathRootKind.ProgramInstallation,
            Redactor.Redact(Path.Combine(Installation, "engine", "orzioclash.exe")).PathRootKind);
        Assert.Equal(
            PathRootKind.UserProfile,
            Redactor.Redact(Path.Combine(UserProfile, "Documents", "a.html")).PathRootKind);
        Assert.Equal(
            PathRootKind.Relative,
            Redactor.Redact(Path.Combine("relative", "a.html")).PathRootKind);
    }

    [Fact]
    public void UncPaths_AreClassifiedAsNetwork()
    {
        Assert.Equal(PathRootKind.Network, Redactor.Redact(@"\\fileserver\bim\a.xml").PathRootKind);
        Assert.Equal(PathRootKind.Network, Redactor.Redact("//fileserver/bim/a.xml").PathRootKind);
    }

    [Fact]
    public void LocalPaths_LiveUnderOrzioClashReportLauncherAndNeverInTheInstallation()
    {
        var paths = new LauncherLocalPaths(Path.Combine(LocalAppData, "Orzio", "ClashReportLauncher"));

        Assert.EndsWith(Path.Combine("Orzio", "ClashReportLauncher"), paths.RootDirectory, StringComparison.Ordinal);
        Assert.Equal(Path.Combine(paths.RootDirectory, "settings.json"), paths.SettingsFilePath);
        Assert.Equal(Path.Combine(paths.RootDirectory, "recent-items.json"), paths.RecentItemsFilePath);
        Assert.Equal(Path.Combine(paths.RootDirectory, "logs"), paths.LogsDirectory);
        Assert.Equal(Path.Combine(paths.RootDirectory, "jobs"), paths.JobsDirectory);
        Assert.Equal(Path.Combine(paths.RootDirectory, "diagnostics"), paths.DiagnosticsDirectory);
    }

    [Fact]
    public void LocalPaths_ForCurrentUser_UseTheOrzioLayout()
    {
        LauncherLocalPaths paths = LauncherLocalPaths.ForCurrentUser();

        Assert.EndsWith(Path.Combine("Orzio", "ClashReportLauncher"), paths.RootDirectory, StringComparison.Ordinal);
        Assert.True(Path.IsPathRooted(paths.RootDirectory));
    }
}
