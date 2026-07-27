using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Analysis;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Core.Presentation;
using OrzioClashReport.Output.Html;

namespace OrzioClashReport.Tests
{
    public sealed class LongitudinalRenderingTests
    {
        private const string ClashTestName = "Coordination & Clearance <Main>";
        private static readonly DateTimeOffset RunACreatedAt = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset RunBCreatedAt = new DateTimeOffset(2026, 7, 2, 8, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset RunCCreatedAt = new DateTimeOffset(2026, 7, 3, 8, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset RunDCreatedAt = new DateTimeOffset(2026, 7, 4, 8, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Render_NullResult_ThrowsArgumentNullException()
        {
            var renderer = new HtmlLongitudinalClashReportRenderer();

            var exception = Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));
            Assert.Equal("result", exception.ParamName);
        }

        [Fact]
        public void Constructor_IsPublicParameterlessAndHasNoDependencies()
        {
            Type rendererType = typeof(HtmlLongitudinalClashReportRenderer);

            Assert.True(rendererType.IsPublic);
            Assert.True(rendererType.IsSealed);

            ConstructorInfo[] constructors = rendererType.GetConstructors();
            ConstructorInfo constructor = Assert.Single(constructors);
            Assert.Empty(constructor.GetParameters());
        }

        [Fact]
        public void Render_RepeatedCalls_AreByteIdentical()
        {
            var presentation = CreateGoldenPresentation();
            var renderer = new HtmlLongitudinalClashReportRenderer();

            string first = renderer.Render(presentation);
            string second = renderer.Render(presentation);

            Assert.Equal(first, second, StringComparer.Ordinal);
            Assert.DoesNotContain("\r", first, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_MatchesLongitudinalGoldenFile()
        {
            string actual = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());
            string goldenPath = Path.Combine(AppContext.BaseDirectory, "Golden", "longitudinal-report.golden.html");
            string expected = NormalizeGoldenLineEndings(File.ReadAllText(goldenPath));

            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }

        private static string NormalizeGoldenLineEndings(string value) =>
            value.Replace("\r\n", "\n").Replace("\r", "\n");

        [Fact]
        public void Render_DocumentShell_IsHtml5SelfContainedAndStyledInline()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            Assert.StartsWith("<!doctype html>\n<html lang=\"en\">", html, StringComparison.Ordinal);
            Assert.Contains("<meta charset=\"utf-8\">", html, StringComparison.Ordinal);
            Assert.Contains("<title>Orzio Clash Longitudinal Report</title>", html, StringComparison.Ordinal);
            Assert.Contains("<style>", html, StringComparison.Ordinal);
            Assert.Contains("</style>", html, StringComparison.Ordinal);
            Assert.Contains("longitudinal-header", html, StringComparison.Ordinal);
            Assert.Contains("longitudinal-summary-section", html, StringComparison.Ordinal);
            Assert.Contains("interpretation-warning", html, StringComparison.Ordinal);
            Assert.Contains("run-sequence-section", html, StringComparison.Ordinal);
            Assert.Contains("continuity-paths-section", html, StringComparison.Ordinal);
            Assert.Contains("standalone-selected-matches-section", html, StringComparison.Ordinal);
            Assert.Contains("non-path-lifecycle-section", html, StringComparison.Ordinal);
            Assert.Contains("transition-sections", html, StringComparison.Ordinal);
            Assert.Contains("longitudinal-classification-note", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_DoesNotContainScriptExternalStylesheetUrlOrExternalAsset()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("rel=\"stylesheet\"", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("href=", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("src=", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("http://", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("https://", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Render_LongitudinalSummary_UsesExactPresentationCounts()
        {
            var presentation = CreateGoldenPresentation();
            string html = new HtmlLongitudinalClashReportRenderer().Render(presentation);

            Assert.Contains("<h3>Indexed runs</h3><p>4</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Adjacent comparisons</h3><p>3</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Selected matches</h3><p>5</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Continuity links</h3><p>2</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Continuity paths</h3><p>1</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Standalone selected matches</h3><p>2</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Lifecycle entries</h3><p>10</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Non-path lifecycle entries</h3><p>7</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>StillOpen</h3><p>4</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>New</h3><p>1</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Resolved</h3><p>3</p>", html, StringComparison.Ordinal);
            Assert.Contains("<h3>Unverifiable</h3><p>2</p>", html, StringComparison.Ordinal);

            Assert.Equal(4, presentation.RunCount);
            Assert.Equal(3, presentation.AdjacentComparisonCount);
            Assert.Equal(5, presentation.SelectedMatchCount);
            Assert.Equal(2, presentation.ContinuityLinkCount);
            Assert.Equal(1, presentation.ContinuityPathCount);
        }

        [Fact]
        public void Render_RunsAppearInDeclaredOrderWithDeclaredRevisions()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            AssertInOrder(
                html,
                "run-A &lt;alpha&gt;",
                "run-B",
                "run-C",
                "run-D");
            AssertInOrder(
                html,
                "Sigma / Structure / Main &amp; Core @ R01",
                "Sigma / Structure / Main &amp; Core @ R02",
                "Sigma / Structure / Main &amp; Core @ R03",
                "Sigma / Structure / Main &amp; Core @ R04");
        }

        [Fact]
        public void Render_PathsAppearInCanonicalOrderAndSelectedMatchesFollowPathOrder()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            Assert.Contains("Continuity path 1", html, StringComparison.Ordinal);
            AssertInOrder(
                html,
                "Path clash in run A",
                "Path clash in run B",
                "Path clash in run C",
                "Path clash in run D");
            AssertInOrder(
                html,
                "Transition 1 / Entry 1",
                "Transition 2 / Entry 1",
                "Transition 3 / Entry 1");
        }

        [Fact]
        public void Render_StandaloneSelectedMatchesAppearAndAreNotPromotedToPath()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            string standaloneSection = ExtractSection(html, "standalone-selected-matches-section");
            Assert.Contains("Standalone selected match", standaloneSection, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Standalone clash in run A", standaloneSection, StringComparison.Ordinal);
            Assert.Contains("Ambiguous clash primary A", standaloneSection, StringComparison.Ordinal);
            Assert.Contains("does not participate in any continuity link or path", standaloneSection, StringComparison.Ordinal);
            Assert.DoesNotContain("Continuity path: 1", standaloneSection, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_StandaloneNotes_AreNestedInsideCorrespondingListItems()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());
            string standaloneSection = ExtractSection(html, "standalone-selected-matches-section");

            Assert.DoesNotContain("</li>\n<p class=\"standalone-note\">", standaloneSection, StringComparison.Ordinal);
            AssertStandaloneNoteIsInsideListItem(standaloneSection, "Standalone clash in run A");
            AssertStandaloneNoteIsInsideListItem(standaloneSection, "Ambiguous clash primary A");
        }

        [Fact]
        public void Render_NonPathLifecycleEntriesAppearInReceivedOrder()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());
            string nonPathSection = ExtractSection(html, "non-path-lifecycle-section");

            AssertInOrder(
                nonPathSection,
                "Transition 1 / Entry 2",
                "Transition 1 / Entry 3",
                "Transition 1 / Entry 4",
                "Transition 1 / Entry 5",
                "Transition 2 / Entry 2",
                "Transition 2 / Entry 3",
                "Transition 3 / Entry 2");
        }

        [Fact]
        public void Render_TransitionsAndLifecycleEntriesAppearInReceivedOrder()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());
            string transitionsSection = ExtractSection(html, "transition-sections");

            AssertInOrder(
                transitionsSection,
                "Transition 1 of 3",
                "Transition 1 / Entry 1",
                "Transition 1 / Entry 5",
                "Transition 2 of 3",
                "Transition 2 / Entry 1",
                "Transition 2 / Entry 3",
                "Transition 3 of 3",
                "Transition 3 / Entry 1",
                "Transition 3 / Entry 2");
        }

        [Fact]
        public void Render_StatusesAppearInExpectedTransitions()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());
            string firstTransition = ExtractTransition(html, "Transition 1 of 3");
            string secondTransition = ExtractTransition(html, "Transition 2 of 3");
            string thirdTransition = ExtractTransition(html, "Transition 3 of 3");

            Assert.Contains("<dt>StillOpen</dt><dd>2</dd>", firstTransition, StringComparison.Ordinal);
            Assert.Contains("<dt>Unverifiable</dt><dd>2</dd>", firstTransition, StringComparison.Ordinal);
            Assert.Contains("<dt>Resolved</dt><dd>1</dd>", firstTransition, StringComparison.Ordinal);
            Assert.Contains("<dt>Resolved</dt><dd>2</dd>", secondTransition, StringComparison.Ordinal);
            Assert.Contains("<dt>New</dt><dd>1</dd>", thirdTransition, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_ConfidenceLifecycleEvidenceAndMatchEvidenceAppearWithoutReclassification()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            Assert.Contains("<dt>Confidence</dt><dd>High</dd>", html, StringComparison.Ordinal);
            Assert.Contains("SelectedMatch / SupportsClassification", html, StringComparison.Ordinal);
            Assert.Contains("CandidateAmbiguity / BlocksClassification", html, StringComparison.Ordinal);
            Assert.Contains("SourceClashGuid / Supports", html, StringComparison.Ordinal);
            Assert.Contains("ElementIdentifierPair / Supports", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_DynamicContentIsHtmlEncoded()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            Assert.DoesNotContain("run-A <alpha>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<Main>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Pipe <Path>", html, StringComparison.Ordinal);
            Assert.Contains("run-A &lt;alpha&gt;", html, StringComparison.Ordinal);
            Assert.Contains("Coordination &amp; Clearance &lt;Main&gt;", html, StringComparison.Ordinal);
            Assert.Contains("Pipe &lt;Path&gt; &amp; Valve", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_DoesNotRenderSuppressedPathsHashesOrProperties()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            Assert.DoesNotContain("SECRET-SOURCE-PATH-SENTINEL", html, StringComparison.Ordinal);
            Assert.DoesNotContain("SECRET-CONTENT-HASH-SENTINEL", html, StringComparison.Ordinal);
            Assert.DoesNotContain("SECRET-PROPERTIES-SENTINEL", html, StringComparison.Ordinal);
            Assert.Contains("Sigma_Main_R01.nwc", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_SourceGuidIsLabeledEvidenceOnly()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            Assert.Contains("Source GUID (evidence only)", html, StringComparison.Ordinal);
            Assert.Contains("Source clash GUID is evidence only.", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_DoesNotInventForbiddenIdentifiersScoresOrAggregateConfidence()
        {
            string html = new HtmlLongitudinalClashReportRenderer().Render(CreateGoldenPresentation());

            Assert.DoesNotContain("Status: Reopened", html, StringComparison.Ordinal);
            Assert.DoesNotContain("persistent clash ID", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("path ID", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fingerprint", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("score", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("aggregate confidence", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Render_TwoRunScenario_HasZeroContinuityPathsAndStandaloneSelectedMatches()
        {
            var presentation = CreateTwoRunPresentation();
            string html = new HtmlLongitudinalClashReportRenderer().Render(presentation);

            Assert.Equal(2, presentation.RunCount);
            Assert.Equal(1, presentation.AdjacentComparisonCount);
            Assert.Equal(0, presentation.ContinuityLinkCount);
            Assert.Equal(0, presentation.ContinuityPathCount);
            Assert.True(presentation.StandaloneSelectedMatchCount > 0);
            Assert.Contains("<h3>Continuity paths</h3><p>0</p>", html, StringComparison.Ordinal);
            Assert.Contains("Zero continuity paths.", html, StringComparison.Ordinal);
            Assert.Contains("Standalone selected matches", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_EmptyCollections_ShowExplicitMessagesAndSections()
        {
            var presentation = CreateEmptyTwoRunPresentation();
            string html = new HtmlLongitudinalClashReportRenderer().Render(presentation);

            Assert.Empty(presentation.PathPresentations);
            Assert.Empty(presentation.StandaloneSelectedMatches);
            Assert.Empty(presentation.NonPathLifecycleEntries);
            Assert.Contains("continuity-paths-section", html, StringComparison.Ordinal);
            Assert.Contains("standalone-selected-matches-section", html, StringComparison.Ordinal);
            Assert.Contains("non-path-lifecycle-section", html, StringComparison.Ordinal);
            Assert.Contains("Zero continuity paths.", html, StringComparison.Ordinal);
            Assert.Contains("No standalone selected matches.", html, StringComparison.Ordinal);
            Assert.Contains("No lifecycle entries outside continuity paths.", html, StringComparison.Ordinal);
        }

        private static ClashRunSequencePresentationResult CreateGoldenPresentation()
        {
            var revisionsA = MakeRevisions("R01");
            var revisionsB = MakeRevisions("R02");
            var revisionsC = MakeRevisions("R03");
            var revisionsD = MakeRevisions("R04");

            var runA = MakeRun(
                "run-A <alpha>",
                RunACreatedAt,
                revisionsA,
                MakeOccurrence("Path clash in run A", revisionsA.Structure, revisionsA.Hvac, "PATH-A", "PATH-B", "guid-path", "Pipe <Path> & Valve"),
                MakeOccurrence("Standalone clash in run A", revisionsA.Structure, revisionsA.Hvac, "STAND-A", "STAND-B", "guid-standalone", "Standalone duct"),
                MakeOccurrence("Resolved only clash in run A", revisionsA.Structure, revisionsA.Hvac, "RES-A", "RES-B", "guid-resolved", "Resolved pipe"),
                MakeOccurrence("Ambiguous clash primary A", revisionsA.Structure, revisionsA.Hvac, "AMB-A", "AMB-B", "guid-ambiguous", "Ambiguous primary"),
                MakeOccurrence("Ambiguous clash secondary A", revisionsA.Structure, revisionsA.Hvac, "AMB-A", "AMB-B", "guid-ambiguous", "Ambiguous secondary"));

            var runB = MakeRun(
                "run-B",
                RunBCreatedAt,
                revisionsB,
                MakeOccurrence("Path clash in run B", revisionsB.Structure, revisionsB.Hvac, "PATH-A", "PATH-B", "guid-path", "Pipe <Path> & Valve"),
                MakeOccurrence("Standalone clash in run B", revisionsB.Structure, revisionsB.Hvac, "STAND-A", "STAND-B", "guid-standalone", "Standalone duct"),
                MakeOccurrence("Ambiguous clash in run B", revisionsB.Structure, revisionsB.Hvac, "AMB-A", "AMB-B", "guid-ambiguous", "Ambiguous current"));

            var runC = MakeRun(
                "run-C",
                RunCCreatedAt,
                revisionsC,
                MakeOccurrence("Path clash in run C", revisionsC.Structure, revisionsC.Hvac, "PATH-A", "PATH-B", "guid-path", "Pipe <Path> & Valve"));

            var runD = MakeRun(
                "run-D",
                RunDCreatedAt,
                revisionsD,
                MakeOccurrence("Path clash in run D", revisionsD.Structure, revisionsD.Hvac, "PATH-A", "PATH-B", "guid-path", "Pipe <Path> & Valve"),
                MakeOccurrence("New only clash in run D", revisionsD.Structure, revisionsD.Hvac, "NEW-A", "NEW-B", "guid-new", "New diffuser"));

            return BuildPresentation(runA, runB, runC, runD);
        }

        private static ClashRunSequencePresentationResult CreateTwoRunPresentation()
        {
            var revisionsA = MakeRevisions("R11");
            var revisionsB = MakeRevisions("R12");
            var runA = MakeRun(
                "two-run-A",
                RunACreatedAt,
                revisionsA,
                MakeOccurrence("Two run selected A", revisionsA.Structure, revisionsA.Hvac, "TWO-A", "TWO-B", "guid-two", "Two run clash"));
            var runB = MakeRun(
                "two-run-B",
                RunBCreatedAt,
                revisionsB,
                MakeOccurrence("Two run selected B", revisionsB.Structure, revisionsB.Hvac, "TWO-A", "TWO-B", "guid-two", "Two run clash"));

            return BuildPresentation(runA, runB);
        }

        private static ClashRunSequencePresentationResult CreateEmptyTwoRunPresentation()
        {
            var revisionsA = MakeRevisions("R21");
            var revisionsB = MakeRevisions("R22");
            var runA = MakeRun("empty-run-A", RunACreatedAt, revisionsA);
            var runB = MakeRun("empty-run-B", RunBCreatedAt, revisionsB);

            return BuildPresentation(runA, runB);
        }

        private static ClashRunSequencePresentationResult BuildPresentation(params CoordinationRun[] runs)
        {
            IClashMatcher matcher = new ConservativeClashMatcher();
            IClashRunComparer runComparer = new DeterministicClashRunComparer(matcher);
            IClashLifecycleClassifier lifecycleClassifier = new ConservativeClashLifecycleClassifier();
            IClashRunSequenceComparer sequenceComparer =
                new DeterministicAdjacentClashRunSequenceComparer(runComparer, lifecycleClassifier);
            IClashRunSequenceContinuityProjector continuityProjector =
                new DeterministicSelectedMatchContinuityProjector();
            IClashRunSequenceContinuityPathAssembler continuityPathAssembler =
                new DeterministicSelectedMatchContinuityPathAssembler();
            IClashRunSequenceAnalyzer analyzer =
                new DeterministicClashRunSequenceAnalyzer(sequenceComparer, continuityProjector, continuityPathAssembler);
            IClashRunSequencePresentationProjector projector =
                new DeterministicClashRunSequencePresentationProjector();

            return projector.Project(analyzer.Analyze(runs));
        }

        private static ModelRevisionPair MakeRevisions(string revision)
        {
            var structure = new ModelRevision(
                new ModelIdentity("Sigma", "Structure", "Main & Core"),
                revision,
                "Sigma_Main_" + revision + ".nwc",
                "SECRET-SOURCE-PATH-SENTINEL-" + revision,
                "SECRET-CONTENT-HASH-SENTINEL-" + revision,
                null);
            var hvac = new ModelRevision(
                new ModelIdentity("Alfa", "HVAC", "Coordination"),
                revision,
                "Alfa_HVAC_" + revision + ".nwc",
                "SECRET-SOURCE-PATH-SENTINEL-HVAC-" + revision,
                "SECRET-CONTENT-HASH-SENTINEL-HVAC-" + revision,
                null);

            return new ModelRevisionPair(structure, hvac);
        }

        private static CoordinationRun MakeRun(
            string runId,
            DateTimeOffset createdAt,
            ModelRevisionPair revisions,
            params ClashOccurrence[] occurrences)
        {
            var executedTests = new[]
            {
                new ExecutedClashTest(ClashTestName, revisions.Structure.Identity, revisions.Hvac.Identity),
            };

            return new CoordinationRun(
                new RunManifest(runId, createdAt, new[] { revisions.Structure, revisions.Hvac }, executedTests),
                occurrences);
        }

        private static ClashOccurrence MakeOccurrence(
            string clashName,
            ModelRevision modelA,
            ModelRevision modelB,
            string elementIdA,
            string elementIdB,
            string guid,
            string elementName) =>
            new ClashOccurrence(
                ClashTestName,
                new ClashResult(
                    clashName,
                    ClashStatus.Active,
                    0.125,
                    null,
                    new ClashPoint(1.25, 2.5, 3.75),
                    new ClashObject(
                        elementIdA,
                        elementName,
                        "Level <01>",
                        modelA.SourceFileName,
                        new[] { "Hidden", "Path" },
                        new Dictionary<string, string> { ["Hidden"] = "SECRET-PROPERTIES-SENTINEL" }),
                    new ClashObject(
                        elementIdB,
                        "Duct & Sleeve",
                        "Level <01>",
                        modelB.SourceFileName,
                        null,
                        new Dictionary<string, string> { ["Hidden"] = "SECRET-PROPERTIES-SENTINEL-B" }),
                    guid),
                modelA,
                modelB);

        private static void AssertInOrder(string text, params string[] expectedFragments)
        {
            int previousIndex = -1;
            foreach (string fragment in expectedFragments)
            {
                int currentIndex = text.IndexOf(fragment, previousIndex + 1, StringComparison.Ordinal);
                Assert.True(currentIndex > previousIndex, $"Expected fragment '{fragment}' after index {previousIndex}.");
                previousIndex = currentIndex;
            }
        }

        private static void AssertStandaloneNoteIsInsideListItem(string standaloneSection, string marker)
        {
            int markerIndex = standaloneSection.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(markerIndex >= 0, $"Expected marker '{marker}' in standalone section.");

            int itemStart = standaloneSection.LastIndexOf("<li class=\"standalone-selected-match ", markerIndex, StringComparison.Ordinal);
            Assert.True(itemStart >= 0, $"Expected marker '{marker}' inside a standalone selected match item.");

            int nextItemStart = standaloneSection.IndexOf("<li class=\"standalone-selected-match ", itemStart + 1, StringComparison.Ordinal);
            int sectionListEnd = standaloneSection.LastIndexOf("</ol>\n</section>", StringComparison.Ordinal);
            Assert.True(sectionListEnd > itemStart, $"Expected standalone selected matches list to close after marker '{marker}'.");

            int itemLimit = nextItemStart >= 0 ? nextItemStart : sectionListEnd;
            int itemEnd = standaloneSection.LastIndexOf("</li>", itemLimit - 1, StringComparison.Ordinal);
            Assert.True(itemEnd > markerIndex, $"Expected closing list item after marker '{marker}'.");

            int noteIndex = standaloneSection.IndexOf("<p class=\"standalone-note\">", markerIndex, StringComparison.Ordinal);
            Assert.True(noteIndex > markerIndex && noteIndex < itemEnd, $"Expected standalone note before the closing list item for marker '{marker}'.");
        }

        private static string ExtractSection(string html, string cssClass)
        {
            string marker = "<section class=\"" + cssClass + "\">";
            int startIndex = html.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(startIndex >= 0);

            string[] topLevelMarkers =
            {
                "<section class=\"longitudinal-summary-section\">",
                "<section class=\"interpretation-warning\">",
                "<section class=\"run-sequence-section\">",
                "<section class=\"continuity-paths-section\">",
                "<section class=\"standalone-selected-matches-section\">",
                "<section class=\"non-path-lifecycle-section\">",
                "<section class=\"transition-sections\">",
                "<footer class=\"longitudinal-classification-note\">",
            };

            int endIndex = html.Length;
            foreach (string topLevelMarker in topLevelMarkers)
            {
                int candidate = html.IndexOf(topLevelMarker, startIndex + marker.Length, StringComparison.Ordinal);
                if (candidate >= 0 && candidate < endIndex)
                {
                    endIndex = candidate;
                }
            }

            return html.Substring(startIndex, endIndex - startIndex);
        }

        private static string ExtractTransition(string html, string transitionLabel)
        {
            int labelIndex = html.IndexOf(transitionLabel, StringComparison.Ordinal);
            Assert.True(labelIndex >= 0);

            int articleStart = html.LastIndexOf("<article class=\"transition-section\">", labelIndex, StringComparison.Ordinal);
            Assert.True(articleStart >= 0);

            int nextArticle = html.IndexOf("<article class=\"transition-section\">", labelIndex + transitionLabel.Length, StringComparison.Ordinal);
            return nextArticle >= 0
                ? html.Substring(articleStart, nextArticle - articleStart)
                : html.Substring(articleStart);
        }

        private sealed class ModelRevisionPair
        {
            public ModelRevisionPair(ModelRevision structure, ModelRevision hvac)
            {
                Structure = structure;
                Hvac = hvac;
            }

            public ModelRevision Structure { get; }
            public ModelRevision Hvac { get; }
        }
    }
}
