using System;
using System.IO;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// The installer's promises, checked as text because nothing here can be executed outside
    /// Windows. These are the claims that would be expensive to discover broken on an evaluator's
    /// machine: where it installs, what it needs, and what it removes.
    /// </summary>
    public sealed class InstallerContractTests
    {
        private static string InstallerScript =>
            File.ReadAllText(Path.Combine(
                RepositoryLayout.RootDirectory, "installer", "windows", "OrzioClashReportLauncher.iss"));

        private static string PublishScript => ReadScript("publish-launcher.ps1");

        private static string PackageScript => ReadScript("package-launcher.ps1");

        private static string SmokeScript => ReadScript("smoke-launcher.ps1");

        [Fact]
        public void TheInstallerRunsWithoutAdministratorRights()
        {
            Assert.Contains("PrivilegesRequired=lowest", InstallerScript, StringComparison.Ordinal);
        }

        [Fact]
        public void MachineWideInstallationExistsOnlyAsAnExplicitFallback()
        {
            // The dialog is what makes it explicit: elevation is offered, never assumed.
            Assert.Contains("PrivilegesRequiredOverridesAllowed=dialog", InstallerScript, StringComparison.Ordinal);
            Assert.Contains("AppLocker", InstallerScript, StringComparison.Ordinal);
        }

        [Fact]
        public void TheDefaultDirectoryIsPerUser()
        {
            // {autopf} is {localappdata}\Programs in the default non-elevated mode.
            Assert.Contains(@"DefaultDirName={autopf}\Orzio\ClashReportLauncher", InstallerScript, StringComparison.Ordinal);
        }

        [Fact]
        public void TheStartMenuEntryExistsAndTheDesktopShortcutIsOptionalAndUnchecked()
        {
            Assert.Contains(@"Name: ""{group}\{#LauncherName}""", InstallerScript, StringComparison.Ordinal);
            Assert.Contains(@"Name: ""desktopicon""", InstallerScript, StringComparison.Ordinal);
            Assert.Contains("Flags: unchecked", InstallerScript, StringComparison.Ordinal);
        }

        [Fact]
        public void UninstallRemovesOnlyWhatTheInstallerLaidDown()
        {
            Assert.Contains(@"Type: filesandordirs; Name: ""{app}\engine""", InstallerScript, StringComparison.Ordinal);
            Assert.Contains(@"Type: filesandordirs; Name: ""{app}\samples""", InstallerScript, StringComparison.Ordinal);
            Assert.Contains(@"Type: filesandordirs; Name: ""{app}\docs""", InstallerScript, StringComparison.Ordinal);

            // Nothing outside {app} is ever listed for automatic deletion.
            foreach (string line in InstallerScript.Split('\n'))
            {
                if (line.TrimStart().StartsWith("Type:", StringComparison.Ordinal))
                {
                    Assert.Contains("{app}", line, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void DeletingLocalDataIsOfferedOnceAndDefaultsToNo()
        {
            Assert.Contains("CurUninstallStepChanged", InstallerScript, StringComparison.Ordinal);
            Assert.Contains("MB_YESNO, IDNO", InstallerScript, StringComparison.Ordinal);
            Assert.Contains(@"{localappdata}\Orzio\ClashReportLauncher", InstallerScript, StringComparison.Ordinal);
        }

        [Fact]
        public void UninstallNeverTouchesTheUsersOwnFiles()
        {
            foreach (string extension in new[] { ".xml", ".nwd", ".nwf", ".nwc", ".rvt" })
            {
                Assert.DoesNotContain(extension, InstallerScript, StringComparison.OrdinalIgnoreCase);
            }

            // The whole vendor folder is never deleted: only the launcher's own subfolder is offered.
            Assert.DoesNotContain(@"DelTree(ExpandConstant('{localappdata}\Orzio')", InstallerScript, StringComparison.Ordinal);
        }

        [Fact]
        public void TheInstalledLayoutIsTheOneTheLauncherLooksFor()
        {
            Assert.Contains("engine/win-x64", PublishScript, StringComparison.Ordinal);
            Assert.Contains("orzioclash.exe", PublishScript, StringComparison.Ordinal);
            Assert.Contains("engine-manifest.json", PublishScript, StringComparison.Ordinal);
            Assert.Contains("OrzioClashReport.Launcher.Desktop.exe", InstallerScript, StringComparison.Ordinal);
        }

        [Fact]
        public void TheEngineHashIsComputedFromTheRealPublishedExecutable()
        {
            Assert.Contains("Get-FileHash", PublishScript, StringComparison.Ordinal);
            Assert.Contains("-Algorithm SHA256", PublishScript, StringComparison.Ordinal);

            // The packaging step verifies the manifest against the staged file, so a stale or invented
            // digest cannot reach an installer.
            Assert.Contains("does not match the staged executable", PackageScript, StringComparison.Ordinal);
        }

        [Fact]
        public void TheEnginePublishModelMatchesTheReleaseWorkflow()
        {
            string releaseWorkflow = File.ReadAllText(
                Path.Combine(RepositoryLayout.RootDirectory, ".github", "workflows", "release.yml"));

            foreach (string option in new[]
            {
                "--runtime win-x64",
                "--self-contained true",
                "-p:PublishSingleFile=true",
                "-p:DebugType=None",
                "-p:DebugSymbols=false",
            })
            {
                Assert.Contains(option, releaseWorkflow, StringComparison.Ordinal);
                Assert.Contains(option.Replace("--", "--"), PublishScript, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void TrimmingIsOffForThisPhase()
        {
            Assert.Contains("-p:PublishTrimmed=false", PublishScript, StringComparison.Ordinal);
        }

        [Fact]
        public void PackagingRejectsSymbolsTemporariesModelsAndImages()
        {
            Assert.Contains("pdb|tmp|nwd|nwf|nwc|rvt|png|jpg|jpeg|gif", PackageScript, StringComparison.Ordinal);
            Assert.Contains("Forbidden files were found in the staging tree", PackageScript, StringComparison.Ordinal);
        }

        [Fact]
        public void TheInstallerChecksumIsPublishedAndSigningIsNotClaimed()
        {
            Assert.Contains("Installer SHA-256", PackageScript, StringComparison.Ordinal);
            Assert.Contains("NOT code signed", PackageScript, StringComparison.Ordinal);
            Assert.Contains("not code signed", InstallerScript, StringComparison.OrdinalIgnoreCase);

            Assert.DoesNotContain("SignTool", PackageScript, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SignedBy", InstallerScript, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TheSmokeScriptCoversAllTenSteps()
        {
            foreach (string step in new[]
            {
                "Install without administrator rights",
                "Start menu shortcut exists",
                "Launcher is installed",
                "Engine matches its packaged manifest",
                "Generate a report from the bundled sample, outside the installation",
                "No launcher process is left running",
                "Uninstall",
                "The generated report survives the uninstall",
                "No service was left behind",
                "No scheduled task was left behind",
            })
            {
                Assert.Contains(step, SmokeScript, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void TheSmokeScriptRefusesToRunElevatedSoItTestsTheRealInstallPath()
        {
            Assert.Contains("This smoke run is elevated", SmokeScript, StringComparison.Ordinal);
        }

        [Fact]
        public void TheSmokeScriptStatesWhatItDoesNotCover()
        {
            Assert.Contains("Still requires a human", SmokeScript, StringComparison.Ordinal);
            Assert.Contains("SmartScreen", SmokeScript, StringComparison.Ordinal);
            Assert.Contains("AppLocker", SmokeScript, StringComparison.Ordinal);
        }

        private static string ReadScript(string fileName) =>
            File.ReadAllText(Path.Combine(RepositoryLayout.RootDirectory, "scripts", fileName));
    }
}
