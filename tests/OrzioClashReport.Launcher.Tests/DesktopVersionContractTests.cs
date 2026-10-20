using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using OrzioClashReport.Launcher.Application;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// The desktop version has exactly one source, src/LauncherVersion.props. Every consumer (the
    /// application, the installer, the release workflow) reads it; a second copy is how a release ends
    /// up tagged with one version while the application reports another and never sees its own update.
    /// </summary>
    public sealed class DesktopVersionContractTests
    {
        private static readonly XElement VersionGroup =
            XDocument.Load(Path.Combine(RepositoryLayout.RootDirectory, "src", "LauncherVersion.props"))
                .Root!.Element("PropertyGroup")!;

        private static string Prefix => VersionGroup.Element("LauncherVersionPrefix")!.Value.Trim();

        private static string Suffix => VersionGroup.Element("LauncherVersionSuffix")!.Value.Trim();

        private static string Full => Suffix.Length == 0 ? Prefix : Prefix + "-" + Suffix;

        [Fact]
        public void TheApplicationReportsTheVersionFromTheSingleSource()
        {
            Assert.Equal(Full, LauncherBuildInfo.LauncherVersion);
        }

        [Fact]
        public void TheVersionIsAValidSemanticVersion()
        {
            Assert.Matches(new Regex(@"\A[0-9]+\.[0-9]+\.[0-9]+\z"), Prefix);
            Assert.Matches(new Regex(@"\A([0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?\z"), Suffix);
        }

        [Fact]
        public void NoProjectCarriesItsOwnLauncherVersion()
        {
            foreach (string project in new[] { "OrzioClashReport.Launcher.Desktop", "OrzioClashReport.Launcher.Application" })
            {
                string content = File.ReadAllText(RepositoryLayout.ProjectFile(project));

                Assert.Contains(@"<Import Project=""..\LauncherVersion.props"" />", content, StringComparison.Ordinal);
                Assert.Contains("<VersionPrefix>$(LauncherVersionPrefix)</VersionPrefix>", content, StringComparison.Ordinal);
                Assert.Contains("<VersionSuffix>$(LauncherVersionSuffix)</VersionSuffix>", content, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void TheInstallerReceivesItsVersionFromThePackagingScript()
        {
            string installer = File.ReadAllText(Path.Combine(
                RepositoryLayout.RootDirectory, "installer", "windows", "OrzioClashReportLauncher.iss"));
            string package = File.ReadAllText(Path.Combine(RepositoryLayout.RootDirectory, "scripts", "package-launcher.ps1"));

            Assert.DoesNotMatch(new Regex(@"(?m)^#define LauncherVersion "), installer);
            Assert.DoesNotMatch(new Regex(@"(?m)^#define LauncherFullVersion "), installer);
            Assert.Contains("#error LauncherFullVersion is not defined", installer, StringComparison.Ordinal);
            Assert.Contains("get-launcher-version.ps1", package, StringComparison.Ordinal);
            Assert.Contains("/DLauncherFullVersion=", package, StringComparison.Ordinal);
        }

        [Fact]
        public void TheReleaseWorkflowReadsTheSingleSourceAndOnlyPublishesFromMaster()
        {
            string workflow = File.ReadAllText(Path.Combine(
                RepositoryLayout.RootDirectory, ".github", "workflows", "desktop-release.yml"));

            Assert.Contains("./scripts/get-launcher-version.ps1", workflow, StringComparison.Ordinal);
            Assert.Contains("github.ref == 'refs/heads/master'", workflow, StringComparison.Ordinal);
            Assert.Contains("github.event_name != 'pull_request'", workflow, StringComparison.Ordinal);
            Assert.Contains("orzio-clash-update-manifest.json", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("LauncherBuildInfo.cs", workflow, StringComparison.Ordinal);
        }
    }
}
