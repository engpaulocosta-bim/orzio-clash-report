namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// What the private pilot release must be true about itself: one version, stated in one place,
/// agreeing everywhere, and claims that stay inside what has actually been verified.
/// </summary>
public sealed class PilotReleaseContractTests
{
    internal const string LauncherVersion = "0.2.0-launcher-preview.1";

    private static string Read(params string[] relative) =>
        File.ReadAllText(Path.Combine(RepositoryPaths.RepositoryRoot, Path.Combine(relative)));

    [Fact]
    public void TheLauncherProjectDeclaresThePilotVersion()
    {
        string project = Read(
            "src", "OrzioClashReport.Launcher.Desktop", "OrzioClashReport.Launcher.Desktop.csproj");

        Assert.Contains("<VersionPrefix>0.2.0</VersionPrefix>", project, StringComparison.Ordinal);
        Assert.Contains("<VersionSuffix>launcher-preview.1</VersionSuffix>", project, StringComparison.Ordinal);
    }

    [Fact]
    public void TheEngineVersionIsUnchangedByThisRelease()
    {
        string engine = Read("src", "OrzioClashReport.Cli", "OrzioClashReport.Cli.csproj");

        Assert.Contains("<VersionPrefix>0.1.0</VersionPrefix>", engine, StringComparison.Ordinal);
        Assert.Contains("<VersionSuffix>preview.3</VersionSuffix>", engine, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryPlaceThatNamesThePilotVersionAgrees()
    {
        Assert.Contains(LauncherVersion, Read("CHANGELOG.md"), StringComparison.Ordinal);
        Assert.Contains(LauncherVersion, Read("README.md"), StringComparison.Ordinal);
        Assert.Contains(LauncherVersion, Read("docs", "operations", "launcher-pilot.md"), StringComparison.Ordinal);
        Assert.Contains(LauncherVersion, Read("scripts", "package-launcher.ps1"), StringComparison.Ordinal);
        Assert.Contains(
            LauncherVersion,
            Read("installer", "windows", "OrzioClashReportLauncher.iss"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TheEngineAndLauncherReleaseWorkflowsCannotFireOnTheSameTag()
    {
        string engineWorkflow = Read(".github", "workflows", "release.yml");
        string launcherWorkflow = Read(".github", "workflows", "release-launcher.yml");

        // An engine tag such as v0.1.0-preview.3 still matches the engine workflow; a launcher tag
        // is excluded there and matched only here.
        Assert.Contains("\"!v*-launcher-*\"", engineWorkflow, StringComparison.Ordinal);
        Assert.Contains("\"v*-launcher-preview.*\"", launcherWorkflow, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLauncherWorkflowChecksTheTagAgainstTheProjectVersion()
    {
        string workflow = Read(".github", "workflows", "release-launcher.yml");

        Assert.Contains("does not match the launcher version", workflow, StringComparison.Ordinal);
        Assert.Contains("Tagged commit is not contained in origin/master.", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test --configuration Release --no-build", workflow, StringComparison.Ordinal);
        Assert.Contains("scripts/publish-launcher.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("scripts/package-launcher.ps1", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void TheChangelogStatesWhatIsAndIsNotValidated()
    {
        string changelog = Read("CHANGELOG.md");
        int start = changelog.IndexOf("## 0.2.0-launcher-preview.1", StringComparison.Ordinal);
        int end = changelog.IndexOf("## 0.1.0-preview.3", StringComparison.Ordinal);

        Assert.True(start >= 0 && end > start, "The pilot entry must precede the engine's last entry.");
        string entry = changelog.Substring(start, end - start);

        Assert.Contains("three real historical exports", entry, StringComparison.Ordinal);
        Assert.Contains("has not yet been run by a human on a clean Windows machine", entry, StringComparison.Ordinal);
        Assert.Contains("not code signed", entry, StringComparison.Ordinal);
        Assert.Contains("experimental", entry, StringComparison.Ordinal);

        Assert.DoesNotContain("production-ready", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("enterprise-ready", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("fully validated", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void TheChangelogDoesNotClaimTheEngineChanged()
    {
        string changelog = Read("CHANGELOG.md");
        int start = changelog.IndexOf("## 0.2.0-launcher-preview.1", StringComparison.Ordinal);
        int end = changelog.IndexOf("## 0.1.0-preview.3", StringComparison.Ordinal);
        string entry = changelog.Substring(start, end - start);

        Assert.Contains("engine is unchanged", entry, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheReadmeKeepsTheCliAvailableAndSaysTheEngineIsUnchanged()
    {
        string readme = Read("README.md");

        Assert.Contains("the CLI itself remains available", readme, StringComparison.Ordinal);
        Assert.Contains("The engine is unchanged by it.", readme, StringComparison.Ordinal);
        Assert.Contains("not code signed", readme, StringComparison.Ordinal);
        Assert.Contains("no telemetry", readme, StringComparison.Ordinal);

        // The engine's own status statements must survive untouched.
        Assert.Contains(
            "Current source and package candidate version: `0.1.0-preview.3`",
            readme,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ThePilotIsScopedToASmallNumberOfInvitedEvaluators()
    {
        string guide = Read("docs", "operations", "launcher-pilot.md");

        Assert.Contains("private pilot", guide, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("invited evaluators", guide, StringComparison.Ordinal);
        Assert.Contains("authorised private distribution only", guide, StringComparison.Ordinal);
    }
}
