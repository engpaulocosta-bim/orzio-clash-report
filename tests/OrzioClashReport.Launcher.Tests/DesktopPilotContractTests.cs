using System;
using System.IO;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// The pilot package's honesty checks. A questionnaire that quietly loses a topic, or a document
    /// that starts claiming more than was verified, is exactly the failure this project cannot
    /// afford: it would make an evaluator's answers unusable.
    /// </summary>
    public sealed class DesktopPilotContractTests
    {
        private static string PilotGuide => Read("docs", "operations", "desktop-pilot.md");

        private static string Changelog => Read("CHANGELOG.md");

        private static string Readme => Read("README.md");

        [Fact]
        public void TheQuestionnaireCoversEveryRequiredTopic()
        {
            foreach (string topic in new[]
            {
                "### Installation",
                "### SmartScreen",
                "### AppLocker and managed machines",
                "### Quick report",
                "### Understanding the operations",
                "### Errors",
                "### Navigation",
                "### Confirm and reject",
                "### Failure recovery",
                "### Visual perception",
                "### What you actually used",
            })
            {
                Assert.Contains(topic, PilotGuide, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void ThePilotGuideStatesTheUnsignedInstallerPlainly()
        {
            Assert.Contains("not code signed", PilotGuide, StringComparison.Ordinal);
            Assert.Contains("SmartScreen will warn you on first run", PilotGuide, StringComparison.Ordinal);
            Assert.Contains("Get-FileHash", PilotGuide, StringComparison.Ordinal);
        }

        [Fact]
        public void ThePilotGuideDoesNotClaimLongitudinalValidation()
        {
            Assert.Contains("remain experimental", PilotGuide, StringComparison.Ordinal);
            Assert.Contains(
                "Longitudinal matching, lifecycle and continuity validated on real sequential exports | **No**",
                PilotGuide,
                StringComparison.Ordinal);

            Assert.DoesNotContain("longitudinal validation complete", PilotGuide, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("validated longitudinally", PilotGuide, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ThePilotGuideDoesNotClaimTheSmokeHasBeenRun()
        {
            Assert.Contains(
                "The launcher was smoke-tested on a clean Windows machine | **Pending",
                PilotGuide,
                StringComparison.Ordinal);
        }

        [Fact]
        public void ThePilotGuideTellsEvaluatorsNotToSendRealClientData()
        {
            Assert.Contains("Never send a real client export", PilotGuide, StringComparison.Ordinal);
            Assert.Contains("Read the bundle preview before sending it", PilotGuide, StringComparison.Ordinal);
        }

        [Fact]
        public void ThePilotGuideExplainsThatUninstallLeavesTheUsersFilesAlone()
        {
            Assert.Contains("the uninstaller never deletes them", PilotGuide, StringComparison.Ordinal);
        }

        [Fact]
        public void NoDocumentOverclaimsMaturity()
        {
            foreach (string document in new[] { PilotGuide, Changelog, Readme })
            {
                Assert.DoesNotContain("production-ready", document, StringComparison.Ordinal);
                Assert.DoesNotContain("enterprise-ready", document, StringComparison.Ordinal);
                Assert.DoesNotContain("AI-verified", document, StringComparison.Ordinal);
                Assert.DoesNotContain("commercially released", document, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void TheChangelogRecordsTheLauncherWithoutRewritingTheEnginesHistory()
        {
            Assert.Contains("## 0.2.0-launcher-preview.1", Changelog, StringComparison.Ordinal);
            Assert.Contains("The engine is unchanged", Changelog, StringComparison.Ordinal);
            Assert.Contains("Core remains `netstandard2.0`", Changelog, StringComparison.Ordinal);
            Assert.Contains("The CLI remains available", Changelog, StringComparison.Ordinal);

            // The earlier preview's own entry stays exactly as it was.
            Assert.Contains("## 0.1.0-preview.3", Changelog, StringComparison.Ordinal);
            Assert.Contains("## 0.1.0-preview.2", Changelog, StringComparison.Ordinal);
        }

        [Fact]
        public void TheChangelogStatesWhatIsStillUnverified()
        {
            Assert.Contains("has not yet been built or run on Windows", Changelog, StringComparison.Ordinal);
            Assert.Contains("The installer is not code signed", Changelog, StringComparison.Ordinal);
        }

        [Fact]
        public void TheReadmeSeparatesBuiltAndTestedFromInstalledAndRun()
        {
            Assert.Contains("**Desktop launcher, built and tested**: yes", Readme, StringComparison.Ordinal);
            Assert.Contains("**Desktop launcher, installed and run on Windows**: not yet", Readme, StringComparison.Ordinal);
        }

        [Fact]
        public void TheGuardrailDocumentsAgreeOnTheLauncherBoundaries()
        {
            string agents = Read("AGENTS.md");
            string skill = Read(".claude", "skills", "orzio-clash-report", "SKILL.md");

            foreach (string claim in new[]
            {
                "## Desktop launcher",
                "Core stays `netstandard2.0`",
                "The launcher never assembles a command line",
                "with no shell intermediary",
                "Run order stays an explicit human declaration",
                "An algorithmic",
            })
            {
                Assert.Contains(claim, agents, StringComparison.Ordinal);
                Assert.Contains(claim, skill, StringComparison.Ordinal);
            }
        }

        private static string Read(params string[] parts) =>
            File.ReadAllText(Path.Combine(RepositoryLayout.RootDirectory, Path.Combine(parts)));
    }
}
