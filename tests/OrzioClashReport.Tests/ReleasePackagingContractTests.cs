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

        [Fact]
        public void Documentation_DescribesExplicitPersistentIdentityBoundaryForPreview3()
        {
            string repoRoot = GetRepositoryRoot();
            string readme = File.ReadAllText(Path.Combine(repoRoot, "README.md"));
            string internalPreview = File.ReadAllText(Path.Combine(repoRoot, "docs", "operations", "internal-preview.md"));
            string agents = File.ReadAllText(Path.Combine(repoRoot, "AGENTS.md"));
            string skill = File.ReadAllText(Path.Combine(repoRoot, ".claude", "skills", "orzio-clash-report", "SKILL.md"));
            string releaseChecklist = File.ReadAllText(Path.Combine(repoRoot, "docs", "operations", "release-checklist.md"));

            Assert.Contains("Persistent clash identity exists only through an explicit human `ConfirmSameIdentity`", readme, StringComparison.Ordinal);
            Assert.Contains("decision carrying a `persistentIdentityId`.", readme, StringComparison.Ordinal);
            Assert.Contains("The packaged preview does not assign identity", readme, StringComparison.Ordinal);
            Assert.Contains("automatically, propagate identity, infer transitivity", readme, StringComparison.Ordinal);
            Assert.Contains("build a project-wide identity graph", readme, StringComparison.Ordinal);
            Assert.Contains("integrate persistent identities into longitudinal lifecycle", readme, StringComparison.Ordinal);

            Assert.Contains("Persistent clash identity exists only through an explicit human `ConfirmSameIdentity`", internalPreview, StringComparison.Ordinal);
            Assert.Contains("decision carrying a `persistentIdentityId`.", internalPreview, StringComparison.Ordinal);
            Assert.Contains("The preview does not assign identity", internalPreview, StringComparison.Ordinal);
            Assert.Contains("automatically, propagate identity, infer transitivity, perform graph merge", internalPreview, StringComparison.Ordinal);
            Assert.Contains("project-wide identity graph or Clash Ledger", internalPreview, StringComparison.Ordinal);
            Assert.Contains("longitudinal lifecycle", internalPreview, StringComparison.Ordinal);

            Assert.Contains("Persistent clash identity exists only through an explicit human `ConfirmSameIdentity`", agents, StringComparison.Ordinal);
            Assert.Contains("decision carrying a `persistentIdentityId`.", agents, StringComparison.Ordinal);
            Assert.Contains("The preview does not assign identity", agents, StringComparison.Ordinal);
            Assert.Contains("automatically, propagate identity, infer transitivity", agents, StringComparison.Ordinal);
            Assert.Contains("build a project-wide identity graph", agents, StringComparison.Ordinal);
            Assert.Contains("integrate persistent identities into longitudinal lifecycle", agents, StringComparison.Ordinal);

            Assert.Contains("Persistent clash identity exists only through an explicit human `ConfirmSameIdentity`", skill, StringComparison.Ordinal);
            Assert.Contains("decision carrying a `persistentIdentityId`.", skill, StringComparison.Ordinal);
            Assert.Contains("The preview does not assign identity", skill, StringComparison.Ordinal);
            Assert.Contains("automatically, propagate identity, infer transitivity", skill, StringComparison.Ordinal);
            Assert.Contains("build a project-wide identity graph", skill, StringComparison.Ordinal);
            Assert.Contains("integrate persistent identities into longitudinal lifecycle", skill, StringComparison.Ordinal);

            Assert.Contains("The preview claims persistent identity only through explicit human", releaseChecklist, StringComparison.Ordinal);
            Assert.Contains("`ConfirmSameIdentity` decisions carrying `persistentIdentityId`.", releaseChecklist, StringComparison.Ordinal);
            Assert.Contains("No document claims automatic identity assignment, automatic propagation,", releaseChecklist, StringComparison.Ordinal);
            Assert.Contains("transitivity, graph merge, a project-wide identity graph, Clash Ledger", releaseChecklist, StringComparison.Ordinal);
            Assert.Contains("identity integration, `Reopened`, inferred chronology, automatic clash responsibility,", releaseChecklist, StringComparison.Ordinal);

            AssertDoesNotContainPreview3WideIdentityDenial(readme, "README");
            AssertDoesNotContainPreview3WideIdentityDenial(internalPreview, "internal preview guide");
            AssertDoesNotContainPreview3WideIdentityDenial(agents, "AGENTS");
            AssertDoesNotContainPreview3WideIdentityDenial(skill, "repo skill");
            AssertDoesNotContainPreview3WideIdentityDenial(releaseChecklist, "release checklist");
        }

        [Fact]
        public void Changelog_Preview3ClarifiesExplicitPersistentIdentityBoundaryWithoutRewritingPreview2()
        {
            string changelog = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "CHANGELOG.md"));
            string preview3Section = ExtractMarkdownSection(changelog, "0.1.0-preview.3");
            string preview2Section = ExtractMarkdownSection(changelog, "0.1.0-preview.2");

            Assert.Contains("Persistent clash identity exists only through an explicit human", preview3Section, StringComparison.Ordinal);
            Assert.Contains("`ConfirmSameIdentity` decision carrying a `persistentIdentityId`.", preview3Section, StringComparison.Ordinal);
            Assert.Contains("No automatic identity assignment, propagation, transitivity, graph merge, project-wide", preview3Section, StringComparison.Ordinal);
            Assert.Contains("identity graph, longitudinal identity integration, chronology, or clash responsibility.", preview3Section, StringComparison.Ordinal);
            Assert.DoesNotContain("No persistent clash identity.", preview3Section, StringComparison.Ordinal);

            Assert.Contains("No persistent clash identity.", preview2Section, StringComparison.Ordinal);
        }

        private static void AssertDoesNotContainProhibitedClaims(string text, string description)
        {
            Assert.DoesNotContain("production-ready", text, StringComparison.Ordinal);
            Assert.DoesNotContain("enterprise-ready", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AI-verified", text, StringComparison.Ordinal);
            Assert.DoesNotContain("commercially released", text, StringComparison.Ordinal);
            Assert.DoesNotContain("latest release", text, StringComparison.Ordinal);
        }

        private static void AssertDoesNotContainPreview3WideIdentityDenial(string text, string description)
        {
            Assert.DoesNotContain("does not provide persistent clash identity", text, StringComparison.Ordinal);
            Assert.DoesNotContain("does not claim persistent clash identity", text, StringComparison.Ordinal);
            Assert.DoesNotContain("No persistent clash identity", text, StringComparison.Ordinal);
        }

        private static string ExtractMarkdownSection(string markdown, string heading)
        {
            string headingMarker = "## " + heading;
            int start = markdown.IndexOf(headingMarker, StringComparison.Ordinal);
            if (start < 0)
            {
                throw new InvalidOperationException("Could not find heading: " + heading);
            }

            start += headingMarker.Length;

            int end = markdown.IndexOf("\n## ", start, StringComparison.Ordinal);
            if (end < 0)
            {
                end = markdown.Length;
            }

            return markdown.Substring(start, end - start);
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
