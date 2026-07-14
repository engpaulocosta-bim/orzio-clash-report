using System;
using System.Collections.Generic;
using System.IO;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Output.Html;

namespace OrzioClashReport.Tests
{
    public class LifecycleRenderingTests
    {
        private static readonly DateTimeOffset PreviousCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset CurrentCreatedAt = new DateTimeOffset(2026, 7, 11, 9, 30, 0, TimeSpan.Zero);

        [Fact]
        public void Render_NullResult_ThrowsArgumentNullException()
        {
            var renderer = new HtmlLifecycleReportRenderer();

            Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));
        }

        [Fact]
        public void Render_ProducesByteIdenticalOutputOnRepeatedRuns()
        {
            var scenario = CreateAllStatusesScenario();
            var renderer = new HtmlLifecycleReportRenderer();

            string first = renderer.Render(scenario.Result);
            string second = renderer.Render(scenario.Result);

            Assert.Equal(first, second, StringComparer.Ordinal);
        }

        [Fact]
        public void Render_AllStatusesSummary_ShowsExactCounts()
        {
            var html = new HtmlLifecycleReportRenderer().Render(CreateAllStatusesScenario().Result);

            Assert.Contains("<article class=\"summary-card status-stillopen\"><h3>StillOpen</h3><p class=\"summary-count\">1</p></article>", html, StringComparison.Ordinal);
            Assert.Contains("<article class=\"summary-card status-new\"><h3>New</h3><p class=\"summary-count\">1</p></article>", html, StringComparison.Ordinal);
            Assert.Contains("<article class=\"summary-card status-resolved\"><h3>Resolved</h3><p class=\"summary-count\">1</p></article>", html, StringComparison.Ordinal);
            Assert.Contains("<article class=\"summary-card status-unverifiable\"><h3>Unverifiable</h3><p class=\"summary-count\">1</p></article>", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_PreservesLifecycleEntryOrder()
        {
            var scenario = CreateAllStatusesScenario();
            var html = new HtmlLifecycleReportRenderer().Render(scenario.Result);

            Assert.Equal(
                new[]
                {
                    ClashLifecycleStatus.StillOpen,
                    ClashLifecycleStatus.Unverifiable,
                    ClashLifecycleStatus.Resolved,
                    ClashLifecycleStatus.New,
                },
                GetStatuses(scenario.Result));

            int stillOpenIndex = html.IndexOf(scenario.StillOpenSlotReference, StringComparison.Ordinal);
            int unverifiableIndex = html.IndexOf(scenario.UnverifiableSlotReference, StringComparison.Ordinal);
            int resolvedIndex = html.IndexOf(scenario.ResolvedSlotReference, StringComparison.Ordinal);
            int newIndex = html.IndexOf(scenario.NewSlotReference, StringComparison.Ordinal);

            Assert.True(stillOpenIndex >= 0);
            Assert.True(unverifiableIndex > stillOpenIndex);
            Assert.True(resolvedIndex > unverifiableIndex);
            Assert.True(newIndex > resolvedIndex);
        }

        [Fact]
        public void Render_SelectedAndUnmatchedEntries_ShowExpectedSections()
        {
            var scenario = CreateAllStatusesScenario();
            var html = new HtmlLifecycleReportRenderer().Render(scenario.Result);

            string selectedBlock = ExtractEntryBlock(html, scenario.StillOpenSlotReference);
            Assert.Contains("Selected match", selectedBlock, StringComparison.Ordinal);
            Assert.Contains("Confidence: High", selectedBlock, StringComparison.Ordinal);
            Assert.Contains("Lifecycle evidence", selectedBlock, StringComparison.Ordinal);
            Assert.Contains("Match evidence", selectedBlock, StringComparison.Ordinal);
            Assert.Contains(scenario.ExpectedMatchEvidenceDescription, selectedBlock, StringComparison.Ordinal);
            Assert.Contains(scenario.ExpectedMatchEvidenceValue, selectedBlock, StringComparison.Ordinal);

            string resolvedBlock = ExtractEntryBlock(html, scenario.ResolvedSlotReference);
            Assert.DoesNotContain("Selected match", resolvedBlock, StringComparison.Ordinal);
            Assert.Contains("Lifecycle evidence", resolvedBlock, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_EncodesDynamicContentInsteadOfEmittingRawHtml()
        {
            var result = CreateEncodedScenario();
            string html = new HtmlLifecycleReportRenderer().Render(result);

            Assert.DoesNotContain("<script>alert('x')</script>", html, StringComparison.Ordinal);
            Assert.Contains("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;", html, StringComparison.Ordinal);
            Assert.Contains("Model &amp; Structure", html, StringComparison.Ordinal);
            Assert.Contains("Level &lt;01&gt;", html, StringComparison.Ordinal);
            Assert.Contains("value &quot;quoted&quot;", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_DoesNotRenderSuppressedData()
        {
            string html = new HtmlLifecycleReportRenderer().Render(CreateSuppressedDataScenario());

            Assert.DoesNotContain("SECRET-NETWORK-PATH-SENTINEL", html, StringComparison.Ordinal);
            Assert.DoesNotContain("SECRET-CONTENT-HASH-SENTINEL", html, StringComparison.Ordinal);
            Assert.DoesNotContain("SECRET-SMARTTAG-SENTINEL", html, StringComparison.Ordinal);
            Assert.Contains("VISIBLE-SOURCE-FILE.nwc", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_MatchesLifecycleGoldenFile()
        {
            var scenario = CreateAllStatusesScenario();
            var renderer = new HtmlLifecycleReportRenderer();

            string actual = renderer.Render(scenario.Result);
            string goldenPath = Path.Combine(AppContext.BaseDirectory, "Golden", "lifecycle-report.golden.html");
            string expected = File.ReadAllText(goldenPath);

            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }

        private static ClashLifecycleStatus[] GetStatuses(ClashLifecycleResult result)
        {
            var statuses = new ClashLifecycleStatus[result.Entries.Count];
            for (int i = 0; i < result.Entries.Count; i++)
            {
                statuses[i] = result.Entries[i].Status;
            }

            return statuses;
        }

        private static string ExtractEntryBlock(string html, string slotReference)
        {
            int slotIndex = html.IndexOf(slotReference, StringComparison.Ordinal);
            Assert.True(slotIndex >= 0);

            int sectionStart = html.LastIndexOf("<section class=\"lifecycle-entry ", slotIndex, StringComparison.Ordinal);
            Assert.True(sectionStart >= 0);

            int nextSection = html.IndexOf("<section class=\"lifecycle-entry ", slotIndex + slotReference.Length, StringComparison.Ordinal);
            return nextSection >= 0
                ? html.Substring(sectionStart, nextSection - sectionStart)
                : html.Substring(sectionStart);
        }

        private static LifecycleScenario CreateAllStatusesScenario()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04", "Sigma_Main_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Coordination", "R07", "Alfa_Coordination_R07.nwc");

            var previousOccurrences = new[]
            {
                MakeOccurrence(
                    "Coordination Test",
                    "Still Open Clash",
                    sigma,
                    alfa,
                    "P0-A",
                    "P0-B",
                    elementNameA: "Beam 01",
                    levelA: "L01",
                    elementNameB: "Duct 01",
                    levelB: "L01",
                    guid: "guid-still-open",
                    distance: 0.125,
                    point: new ClashPoint(1.111, 2.222, 3.333)),
                MakeOccurrence(
                    "Coordination Test",
                    "Resolved Clash",
                    sigma,
                    alfa,
                    "P1-A",
                    "P1-B",
                    elementNameA: "Beam 02",
                    levelA: "L02",
                    elementNameB: "Duct 02",
                    levelB: "L02",
                    guid: "guid-resolved",
                    distance: 0.875,
                    point: new ClashPoint(4.444, 5.555, 6.666)),
                MakeOccurrence(
                    "Coordination Test",
                    "Unverifiable Clash",
                    sigma,
                    alfa,
                    "P2-A",
                    "P2-B",
                    elementNameA: "Beam 03",
                    levelA: "L03",
                    elementNameB: "Duct 03",
                    levelB: "L03",
                    guid: "guid-unverifiable-previous",
                    distance: 1.125,
                    point: new ClashPoint(7.777, 8.888, 9.999)),
            };

            var currentOccurrences = new[]
            {
                MakeOccurrence(
                    "Coordination Test",
                    "Still Open Clash Current",
                    sigma,
                    alfa,
                    "P0-A",
                    "P0-B",
                    elementNameA: "Beam 01",
                    levelA: "L01",
                    elementNameB: "Duct 01",
                    levelB: "L01",
                    guid: "guid-still-open",
                    distance: 0.125,
                    point: new ClashPoint(1.111, 2.222, 3.333)),
                MakeOccurrence(
                    "Coordination Test",
                    "New Clash",
                    sigma,
                    alfa,
                    "C1-A",
                    "C1-B",
                    elementNameA: "Beam 04",
                    levelA: "L04",
                    elementNameB: "Duct 04",
                    levelB: "L04",
                    guid: "guid-new",
                    distance: 2.125,
                    point: new ClashPoint(3.141, 2.718, 1.618)),
                MakeOccurrence(
                    "Coordination Test",
                    "Unverifiable Clash Current",
                    sigma,
                    alfa,
                    "C2-A",
                    "C2-B",
                    elementNameA: "Beam 05",
                    levelA: "L05",
                    elementNameB: "Duct 05",
                    levelB: "L05",
                    guid: "guid-unverifiable-current",
                    distance: 1.625,
                    point: new ClashPoint(9.111, 8.222, 7.333)),
            };

            var executedTests = new[] { new ExecutedClashTest("Coordination Test", sigma.Identity, alfa.Identity) };
            var previousRun = MakeRun("previous-run", PreviousCreatedAt, new[] { sigma, alfa }, executedTests, previousOccurrences);
            var currentRun = MakeRun("current-run", CurrentCreatedAt, new[] { sigma, alfa }, executedTests, currentOccurrences);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(
                previousOccurrences[0],
                currentOccurrences[0],
                MakeAssessment(
                    previousOccurrences[0],
                    currentOccurrences[0],
                    ClashMatchConfidence.High,
                    new MatchEvidence(
                        MatchEvidenceKind.SourceClashGuid,
                        MatchEvidenceVerdict.Supports,
                        "Exact source GUID match",
                        "guid-still-open",
                        "guid-still-open"),
                    new MatchEvidence(
                        MatchEvidenceKind.ElementIdentifierPair,
                        MatchEvidenceVerdict.Supports,
                        "Element identifiers remained aligned",
                        "P0-A/P0-B",
                        "P0-A/P0-B")));
            matcher.RespondWith(
                previousOccurrences[2],
                currentOccurrences[2],
                MakeAssessment(
                    previousOccurrences[2],
                    currentOccurrences[2],
                    ClashMatchConfidence.Low,
                    new MatchEvidence(
                        MatchEvidenceKind.ElementMetadata,
                        MatchEvidenceVerdict.Unavailable,
                        "Insufficient metadata for confident lifecycle correlation",
                        "L03",
                        "L05")));

            var result = new ConservativeClashLifecycleClassifier().Classify(
                new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun));

            return new LifecycleScenario
            {
                Result = result,
                StillOpenSlotReference = "previous[0] &times; current[0]",
                UnverifiableSlotReference = "previous[2] &times; current[2]",
                ResolvedSlotReference = "previous[1] &times; current[-]",
                NewSlotReference = "previous[-] &times; current[1]",
                ExpectedMatchEvidenceDescription = "Exact source GUID match",
                ExpectedMatchEvidenceValue = "P0-A/P0-B",
            };
        }

        private static ClashLifecycleResult CreateEncodedScenario()
        {
            string payload = "<script>alert('x')</script>";
            string quoted = "value \"quoted\"";
            var model = MakeRevision(payload, "Model & Structure", "Level <01>", quoted, "source \"quoted\".nwc");
            var previousOccurrence = MakeOccurrence(
                "Clash <01> & check",
                payload,
                model,
                model,
                quoted,
                "elem-b",
                elementNameA: "Model & Structure",
                levelA: "Level <01>",
                elementNameB: quoted,
                levelB: "Level <01>",
                guid: payload,
                distance: 0.5,
                point: new ClashPoint(1.25, 2.5, 3.75));
            var currentOccurrence = MakeOccurrence(
                "Clash <01> & check",
                payload,
                model,
                model,
                quoted,
                "elem-b",
                elementNameA: "Model & Structure",
                levelA: "Level <01>",
                elementNameB: quoted,
                levelB: "Level <01>",
                guid: payload,
                distance: 0.5,
                point: new ClashPoint(1.25, 2.5, 3.75));
            var executedTests = new[] { new ExecutedClashTest("Clash <01> & check", model.Identity, model.Identity) };
            var previousRun = MakeRun(payload, PreviousCreatedAt, new[] { model }, executedTests, previousOccurrence);
            var currentRun = MakeRun("Model & Structure", CurrentCreatedAt, new[] { model }, executedTests, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(
                previousOccurrence,
                currentOccurrence,
                MakeAssessment(
                    previousOccurrence,
                    currentOccurrence,
                    ClashMatchConfidence.High,
                    new MatchEvidence(
                        MatchEvidenceKind.ElementMetadata,
                        MatchEvidenceVerdict.Supports,
                        quoted,
                        quoted,
                        payload)));

            return new ConservativeClashLifecycleClassifier().Classify(
                new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun));
        }

        private static ClashLifecycleResult CreateSuppressedDataScenario()
        {
            var model = MakeRevision(
                "Sigma",
                "Structure",
                "Main",
                "R04",
                "VISIBLE-SOURCE-FILE.nwc",
                "SECRET-NETWORK-PATH-SENTINEL",
                "SECRET-CONTENT-HASH-SENTINEL");
            var properties = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["SmartTag"] = "SECRET-SMARTTAG-SENTINEL",
            };

            var previousOccurrence = MakeOccurrence(
                "Hidden Data Test",
                "Hidden Clash",
                model,
                model,
                "elem-a",
                "elem-b",
                propertiesA: properties,
                propertiesB: properties,
                guid: "guid-hidden");
            var currentOccurrence = MakeOccurrence(
                "Hidden Data Test",
                "Hidden Clash",
                model,
                model,
                "elem-a",
                "elem-b",
                propertiesA: properties,
                propertiesB: properties,
                guid: "guid-hidden");
            var executedTests = new[] { new ExecutedClashTest("Hidden Data Test", model.Identity, model.Identity) };
            var previousRun = MakeRun("hidden-previous", PreviousCreatedAt, new[] { model }, executedTests, previousOccurrence);
            var currentRun = MakeRun("hidden-current", CurrentCreatedAt, new[] { model }, executedTests, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(
                previousOccurrence,
                currentOccurrence,
                MakeAssessment(
                    previousOccurrence,
                    currentOccurrence,
                    ClashMatchConfidence.High,
                    new MatchEvidence(
                        MatchEvidenceKind.SourceClashGuid,
                        MatchEvidenceVerdict.Supports,
                        "source guid matched",
                        "guid-hidden",
                        "guid-hidden")));

            return new ConservativeClashLifecycleClassifier().Classify(
                new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun));
        }

        private static CoordinationRun MakeRun(
            string runId,
            DateTimeOffset createdAt,
            ModelRevision[] models,
            ExecutedClashTest[] executedClashTests,
            params ClashOccurrence[] occurrences) =>
            new CoordinationRun(new RunManifest(runId, createdAt, models, executedClashTests), occurrences);

        private static ModelRevision MakeRevision(
            string company,
            string discipline,
            string modelName,
            string revision,
            string sourceFileName,
            string? optionalOne = null,
            string? optionalTwo = null) =>
            new ModelRevision(
                new ModelIdentity(company, discipline, modelName),
                revision,
                sourceFileName,
                optionalOne,
                optionalTwo,
                null);

        private static ClashOccurrence MakeOccurrence(
            string clashTestName,
            string clashName,
            ModelRevision modelA,
            ModelRevision modelB,
            string elementIdA,
            string elementIdB,
            string? elementNameA = null,
            string? levelA = null,
            string? elementNameB = null,
            string? levelB = null,
            IReadOnlyDictionary<string, string>? propertiesA = null,
            IReadOnlyDictionary<string, string>? propertiesB = null,
            string? guid = null,
            double? distance = null,
            ClashPoint? point = null) =>
            new ClashOccurrence(
                clashTestName,
                new ClashResult(
                    clashName,
                    ClashStatus.New,
                    distance,
                    null,
                    point,
                    new ClashObject(elementIdA, elementNameA, levelA, modelA.SourceFileName, null, propertiesA),
                    new ClashObject(elementIdB, elementNameB, levelB, modelB.SourceFileName, null, propertiesB),
                    guid),
                modelA,
                modelB);

        private static ClashMatchAssessment MakeAssessment(
            ClashOccurrence previousOccurrence,
            ClashOccurrence currentOccurrence,
            ClashMatchConfidence confidence,
            params MatchEvidence[] evidence) =>
            new ClashMatchAssessment(previousOccurrence, currentOccurrence, confidence, evidence);

        private sealed class ConfigurableClashMatcher : IClashMatcher
        {
            private readonly Dictionary<(ClashOccurrence Previous, ClashOccurrence Current), ClashMatchAssessment> _responses =
                new Dictionary<(ClashOccurrence, ClashOccurrence), ClashMatchAssessment>();

            public void RespondWith(ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence, ClashMatchAssessment assessment) =>
                _responses[(previousOccurrence, currentOccurrence)] = assessment;

            public ClashMatchAssessment? Assess(ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence)
            {
                if (previousOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(previousOccurrence));
                }

                if (currentOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(currentOccurrence));
                }

                return _responses.TryGetValue((previousOccurrence, currentOccurrence), out var assessment)
                    ? assessment
                    : null;
            }
        }

        private sealed class LifecycleScenario
        {
            public required ClashLifecycleResult Result { get; init; }
            public required string StillOpenSlotReference { get; init; }
            public required string UnverifiableSlotReference { get; init; }
            public required string ResolvedSlotReference { get; init; }
            public required string NewSlotReference { get; init; }
            public required string ExpectedMatchEvidenceDescription { get; init; }
            public required string ExpectedMatchEvidenceValue { get; init; }
        }
    }
}
