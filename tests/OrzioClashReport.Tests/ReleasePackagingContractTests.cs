using System;
using System.IO;

namespace OrzioClashReport.Tests
{
    public sealed class ReleasePackagingContractTests
    {
        [Fact]
        public void ReleaseWorkflow_IncludesExpectedPreview3PackageFilesAndGovernanceNotes()
        {
            string workflow = File.ReadAllText(Path.Combine(GetRepositoryRoot(), ".github", "workflows", "release.yml"));

            Assert.Contains("docs/operations/identity-governance-cli.md", workflow, StringComparison.Ordinal);
            Assert.Contains("docs/operations/identity-governance-validation.md", workflow, StringComparison.Ordinal);
            Assert.Contains("docs/operations/identity-governance-review-report.md", workflow, StringComparison.Ordinal);
            Assert.Contains("docs/operations/pilot-evaluation.md", workflow, StringComparison.Ordinal);
            Assert.Contains("samples/sample-clash.xml", workflow, StringComparison.Ordinal);
            Assert.Contains("samples/sample-clash.run-manifest.json", workflow, StringComparison.Ordinal);
            Assert.Contains("samples/run-manifest.sample.json", workflow, StringComparison.Ordinal);
            Assert.Contains("samples/run-index.template.json", workflow, StringComparison.Ordinal);
            Assert.Contains("PDB files must not be included", workflow, StringComparison.Ordinal);
            Assert.Contains("Forbidden files were included in the package", workflow, StringComparison.Ordinal);
            Assert.Contains("Checksum file format is invalid.", workflow, StringComparison.Ordinal);

            Assert.Contains("create-identity-governance", workflow, StringComparison.Ordinal);
            Assert.Contains("append-identity-decision", workflow, StringComparison.Ordinal);
            Assert.Contains("validate-identity-governance", workflow, StringComparison.Ordinal);
            Assert.Contains("render-identity-governance-report", workflow, StringComparison.Ordinal);
            Assert.Contains("no Clash Ledger", workflow, StringComparison.Ordinal);
            Assert.Contains("no Reopened", workflow, StringComparison.Ordinal);
            Assert.Contains("review report does not project raw ClashObject.SourceModel", workflow, StringComparison.Ordinal);

            Assert.DoesNotContain("production-ready", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("enterprise-ready", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("AI-verified", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("commercially released", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("latest release", workflow, StringComparison.Ordinal);
        }

        [Fact]
        public void Documentation_PreservesPreviewHistoryAndControlledPilotBoundaries()
        {
            string repoRoot = GetRepositoryRoot();
            string readme = File.ReadAllText(Path.Combine(repoRoot, "README.md"));
            string internalPreview = File.ReadAllText(Path.Combine(repoRoot, "docs", "operations", "internal-preview.md"));
            string pilotGuide = File.ReadAllText(Path.Combine(repoRoot, "docs", "operations", "pilot-evaluation.md"));

            Assert.Contains("Current source and package candidate version: `0.1.0-preview.3`", readme, StringComparison.Ordinal);
            Assert.Contains("`v0.1.0-preview.2` did not package the identity-governance workflow.", readme, StringComparison.Ordinal);
            Assert.Contains("`v0.1.0-preview.3` does package the identity-governance workflow", readme, StringComparison.Ordinal);
            Assert.Contains("Legal distribution terms remain an owner decision.", readme, StringComparison.Ordinal);

            Assert.Contains("Current source and package candidate version: `0.1.0-preview.3`", internalPreview, StringComparison.Ordinal);
            Assert.Contains("`v0.1.0-preview.2` did not package the identity-governance workflow.", internalPreview, StringComparison.Ordinal);
            Assert.Contains("`v0.1.0-preview.3` does package the identity-governance workflow.", internalPreview, StringComparison.Ordinal);
            Assert.Contains("The standalone review report intentionally excludes raw `ClashObject.SourceModel`.", internalPreview, StringComparison.Ordinal);
            Assert.Contains("Legal distribution terms remain an owner decision.", internalPreview, StringComparison.Ordinal);

            Assert.Contains("internal controlled pilot", pilotGuide, StringComparison.Ordinal);
            Assert.Contains("explicit human decisions", pilotGuide, StringComparison.Ordinal);
            Assert.Contains("deterministic local reports", pilotGuide, StringComparison.Ordinal);
            Assert.Contains("read-only evidence validation", pilotGuide, StringComparison.Ordinal);
            Assert.Contains("The review report does not project raw `ClashObject.SourceModel`.", pilotGuide, StringComparison.Ordinal);
            Assert.Contains("Legal distribution terms remain an owner decision.", pilotGuide, StringComparison.Ordinal);

            AssertDoesNotContainProhibitedClaims(readme, "README");
            AssertDoesNotContainProhibitedClaims(internalPreview, "internal preview guide");
            AssertDoesNotContainProhibitedClaims(pilotGuide, "pilot evaluation guide");
        }

        private static void AssertDoesNotContainProhibitedClaims(string text, string description)
        {
            Assert.DoesNotContain("production-ready", text, StringComparison.Ordinal);
            Assert.DoesNotContain("enterprise-ready", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AI-verified", text, StringComparison.Ordinal);
            Assert.DoesNotContain("commercially released", text, StringComparison.Ordinal);
            Assert.DoesNotContain("latest release", text, StringComparison.Ordinal);
        }

        private static string GetRepositoryRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string solutionPath = Path.Combine(directory.FullName, "OrzioClashReport.sln");
                if (File.Exists(solutionPath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate the repository root.");
        }
    }
}
