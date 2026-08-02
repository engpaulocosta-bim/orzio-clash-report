namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// The packaging contract, asserted against the scripts and the installer script themselves, so a
/// change that would quietly weaken it fails here rather than on an evaluator's machine.
/// </summary>
public sealed class InstallerContractTests
{
    private static string Read(params string[] relative) =>
        File.ReadAllText(Path.Combine(RepositoryPaths.RepositoryRoot, Path.Combine(relative)));

    private static string InstallerScript => Read("installer", "windows", "OrzioClashReportLauncher.iss");

    private static string PublishScript => Read("scripts", "publish-launcher.ps1");

    private static string PackageScript => Read("scripts", "package-launcher.ps1");

    private static string SmokeScript => Read("scripts", "smoke-launcher.ps1");

    [Fact]
    public void TheInstallerScriptAndItsScriptsExist()
    {
        Assert.NotEmpty(InstallerScript);
        Assert.NotEmpty(PublishScript);
        Assert.NotEmpty(PackageScript);
        Assert.NotEmpty(SmokeScript);
    }

    [Fact]
    public void TheNormalInstallNeedsNoAdministrator()
    {
        Assert.Contains("PrivilegesRequired=lowest", InstallerScript, StringComparison.Ordinal);
        Assert.Contains(
            "DefaultDirName={localappdata}\\Programs\\Orzio\\ClashReportLauncher",
            InstallerScript,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MachineWideIsOnlyAnExplicitFallback()
    {
        // The dialog override is what lets a blocked environment escalate deliberately.
        Assert.Contains("PrivilegesRequiredOverridesAllowed=dialog", InstallerScript, StringComparison.Ordinal);
        Assert.Contains("AppLocker", InstallerScript, StringComparison.Ordinal);
    }

    [Fact]
    public void TheStartMenuEntryExistsAndTheDesktopShortcutIsOptionalAndUnchecked()
    {
        Assert.Contains("{group}\\{#AppName}", InstallerScript, StringComparison.Ordinal);
        Assert.Contains("Name: \"desktopicon\"", InstallerScript, StringComparison.Ordinal);
        Assert.Contains("Flags: unchecked", InstallerScript, StringComparison.Ordinal);
    }

    [Fact]
    public void UninstallRemovesWhatItInstalledAndNothingElse()
    {
        Assert.Contains("Type: filesandordirs; Name: \"{app}\\engine\"", InstallerScript, StringComparison.Ordinal);
        Assert.Contains("Type: filesandordirs; Name: \"{app}\\samples\"", InstallerScript, StringComparison.Ordinal);
        Assert.Contains("Type: filesandordirs; Name: \"{app}\\docs\"", InstallerScript, StringComparison.Ordinal);
    }

    [Fact]
    public void UninstallNeverRemovesTheUsersOwnData()
    {
        // Only the launcher's own subdirectory may ever be deleted, and only on an explicit opt-in.
        Assert.Contains(
            "DelTree(ExpandConstant('{localappdata}\\Orzio\\ClashReportLauncher')",
            InstallerScript,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "DelTree(ExpandConstant('{localappdata}\\Orzio')",
            InstallerScript,
            StringComparison.Ordinal);

        Assert.Contains("RemoveLocalDataPage.Values[0] := False", InstallerScript, StringComparison.Ordinal);
        Assert.Contains("if RemoveLocalData = '1' then", InstallerScript, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePublishScriptProducesTheDeclaredLayout()
    {
        Assert.Contains("engine/win-x64", PublishScript, StringComparison.Ordinal);
        Assert.Contains("engine-manifest.json", PublishScript, StringComparison.Ordinal);
        Assert.Contains("--runtime win-x64", PublishScript, StringComparison.Ordinal);
        Assert.Contains("--self-contained true", PublishScript, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLauncherIsNotTrimmedAtThisStage()
    {
        Assert.Contains("-p:PublishTrimmed=false", PublishScript, StringComparison.Ordinal);
        Assert.DoesNotContain("-p:PublishTrimmed=true", PublishScript, StringComparison.Ordinal);
    }

    [Fact]
    public void TheEnginePublishReusesTheReleaseModel()
    {
        string release = Read(".github", "workflows", "release.yml");

        foreach (string expected in new[]
                 {
                     "--runtime win-x64",
                     "--self-contained true",
                     "-p:PublishSingleFile=true",
                     "-p:DebugType=None",
                     "-p:DebugSymbols=false",
                 })
        {
            Assert.Contains(expected, release, StringComparison.Ordinal);
            Assert.Contains(expected, PublishScript, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheEngineDigestIsComputedOverThePublishedExecutable()
    {
        Assert.Contains("Get-Sha256 -Path $engineExe", PublishScript, StringComparison.Ordinal);
        Assert.Contains("sha256             = $engineSha256", PublishScript, StringComparison.Ordinal);

        // The version written into the manifest is read back from the engine that was published.
        Assert.Contains("& $engineExe --version", PublishScript, StringComparison.Ordinal);
    }

    [Fact]
    public void PackagingRejectsWhatMustNeverBeDistributed()
    {
        foreach (string pattern in new[] { "pdb", "tmp", "nwd|nwf|nwc|rvt", "png|jpg|jpeg|gif" })
        {
            Assert.Contains(pattern, PackageScript, StringComparison.Ordinal);
        }

        Assert.Contains(
            "Files that must never be distributed were found", PackageScript, StringComparison.Ordinal);
    }

    [Fact]
    public void PackagingVerifiesTheManifestBeforeCompiling()
    {
        Assert.Contains(
            "The engine manifest digest does not match the staged engine",
            PackageScript,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PackagingWritesTheInstallerChecksumAndAdmitsThereIsNoSignature()
    {
        Assert.Contains("-Algorithm SHA256", PackageScript, StringComparison.Ordinal);
        Assert.Contains(".sha256", PackageScript, StringComparison.Ordinal);
        Assert.Contains("NOT code signed", PackageScript, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSmokeScriptChecksTheInstalledStateAndWhatSurvivesUninstall()
    {
        foreach (string expected in new[]
                 {
                     "The launcher executable is installed.",
                     "The engine reports its version in the published format",
                     "The installed engine matches the packaged SHA-256.",
                     "The report destination is outside the installation directory.",
                     "The report produced before uninstalling still exists.",
                     "No service named after Orzio was left behind.",
                     "No scheduled task named after Orzio was left behind.",
                 })
        {
            Assert.Contains(expected, SmokeScript, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheSmokeScriptDoesNotClaimWhatOnlyAHumanCanConfirm()
    {
        Assert.Contains("Still to confirm by hand", SmokeScript, StringComparison.Ordinal);
        Assert.Contains("without an administrator prompt", SmokeScript, StringComparison.Ordinal);
        Assert.Contains("opens from the Start menu", SmokeScript, StringComparison.Ordinal);
    }

    [Fact]
    public void TheInstallerSpeaksBothLanguages()
    {
        string installer = InstallerScript;

        Assert.Contains("Name: \"portuguese\"", installer, StringComparison.Ordinal);
        Assert.Contains("Name: \"english\"", installer, StringComparison.Ordinal);

        // Every custom message the wizard shows exists in both languages.
        foreach (string message in new[]
                 {
                     "LocalDataPageCaption",
                     "LocalDataPageDescription",
                     "LocalDataPageText",
                     "RemoveLocalData",
                 })
        {
            Assert.Contains($"portuguese.{message}=", installer, StringComparison.Ordinal);
            Assert.Contains($"english.{message}=", installer, StringComparison.Ordinal);
            Assert.Contains($"{{cm:{message}}}", installer, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ThePilotGuideStatesTheHonestStatus()
    {
        string guide = Read("docs", "operations", "launcher-pilot.md");

        Assert.Contains("Not code signed", guide, StringComparison.Ordinal);
        Assert.Contains("SmartScreen", guide, StringComparison.Ordinal);
        Assert.Contains("AppLocker", guide, StringComparison.Ordinal);
        Assert.Contains("experimental", guide, StringComparison.Ordinal);
        Assert.Contains("three real historical exports", guide, StringComparison.Ordinal);
        Assert.Contains("uninstalling never deletes them", guide, StringComparison.Ordinal);
        Assert.Contains("no telemetry", guide, StringComparison.Ordinal);

        Assert.DoesNotContain("production-ready", guide, StringComparison.Ordinal);
        Assert.DoesNotContain("enterprise-ready", guide, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePilotGuideAsksEveryQuestionThePilotNeedsAnswered()
    {
        string guide = Read("docs", "operations", "launcher-pilot.md");

        foreach (string topic in new[]
                 {
                     "administrator prompt",
                     "SmartScreen",
                     "AppLocker",
                     "Relatório rápido",
                     "understand from the screen alone",
                     "errors did you hit",
                     "easy or confusing",
                     "confirming and rejecting",
                     "crash, hang, or close unexpectedly",
                     "professional tool",
                     "sections did you actually use",
                 })
        {
            Assert.Contains(topic, guide, StringComparison.Ordinal);
        }
    }
}
