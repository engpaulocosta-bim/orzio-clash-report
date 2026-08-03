using System;
using System.Collections.Generic;
using System.IO;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Output.Html;
using Xunit.Abstractions;

namespace OrzioClashReport.Tests
{
    public class RenderingTests
    {
        private readonly ITestOutputHelper _output;

        public RenderingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static GroupedClashReport BuildSampleReport()
        {
            var avac1 = new ClashObject("avac-1", null, "L1", null, null, null);
            var arch1 = new ClashObject("arch-1", null, "L1", null, null, null);
            var avac2 = new ClashObject("avac-2", null, "L2", null, null, null);
            var struct2 = new ClashObject("struct-2", null, "L2", null, null, null);

            var clash1 = new ClashResult(
                "Clash1", ClashStatus.New, -0.007, null, new ClashPoint(1.234, 2.345, -3.456),
                avac1, arch1, "11111111-1111-1111-1111-111111111111");

            var clash2 = new ClashResult(
                "Clash2", ClashStatus.Approved, 0.05, null, null,
                avac2, struct2, "22222222-2222-2222-2222-222222222222");

            var document = new ClashReportDocument(
                "sample.nwd", null, new[] { new ClashBatch("Test 1", 0.001, new[] { clash1, clash2 }) }, "m");

            var groups = new List<ClashGroup>
            {
                new ClashGroup("Test 1", "Architecture", "AVAC", "L1", new[] { clash1 }),
                new ClashGroup("Test 1", "AVAC", "Structure", "L2", new[] { clash2 })
            };

            return new GroupedClashReport(document, groups, rawCount: 2);
        }

        [Fact]
        public void Render_ProducesByteIdenticalOutputOnRepeatedRuns()
        {
            var report = BuildSampleReport();
            var renderer = new HtmlReportRenderer();

            string first = renderer.Render(report);
            string second = renderer.Render(report);

            Assert.Equal(first, second, StringComparer.Ordinal);
            Assert.DoesNotContain("\r", first, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_EncodesDynamicContentInsteadOfEmittingRawHtml()
        {
            var elementA = new ClashObject("a", null, "Level <01>", null, null, null);
            var elementB = new ClashObject("b", null, "Level <01>", null, null, null);

            var clash = new ClashResult(
                "<script>alert('x')</script>", ClashStatus.New, null, null, null,
                elementA, elementB, "33333333-3333-3333-3333-333333333333");

            var document = new ClashReportDocument(
                "<script>alert('x')</script>", null, new[] { new ClashBatch("Test 1", null, new[] { clash }) });

            var group = new ClashGroup(
                "<script>alert('x')</script>", "Model & Structure", "AVAC", "Level <01>", new[] { clash });

            var report = new GroupedClashReport(document, new[] { group }, rawCount: 1);

            string html = new HtmlReportRenderer().Render(report);

            Assert.DoesNotContain("<script>alert('x')</script>", html);
            Assert.Contains("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;", html);
            Assert.Contains("Model &amp; Structure", html);
            Assert.Contains("Level &lt;01&gt;", html);
        }

        [Fact]
        public void Render_HeadsTheDocumentWithTheModelRatherThanTheTool()
        {
            string html = new HtmlReportRenderer().Render(BuildSampleReport());

            // The reader of a forwarded report needs to know which model it describes. The tool that
            // produced it is not that information.
            Assert.Contains("<h1>sample</h1>", html, StringComparison.Ordinal);
            Assert.Contains("<title>sample</title>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<h1>OrzioClashReport</h1>", html, StringComparison.Ordinal);

            // The full file name still appears as provenance.
            Assert.Contains("<dd>sample.nwd</dd>", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_NeverCarriesAFolderChainIntoTheDocument()
        {
            // A source recorded as a full path would otherwise put a client's directory tree into a
            // document that gets forwarded outside the office.
            var document = new ClashReportDocument(
                @"C:\Clients\Acme Confidential\2026\Federated.nwd",
                null,
                new[] { new ClashBatch("Test 1", null, Array.Empty<ClashResult>()) });

            string html = new HtmlReportRenderer().Render(
                new GroupedClashReport(document, Array.Empty<ClashGroup>(), rawCount: 0));

            Assert.DoesNotContain("Acme Confidential", html, StringComparison.Ordinal);
            Assert.DoesNotContain(@"C:\", html, StringComparison.Ordinal);
            Assert.Contains("<h1>Federated</h1>", html, StringComparison.Ordinal);
            Assert.Contains("<dd>Federated.nwd</dd>", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_FallsBackToAGenericTitleWithoutRepeatingItself()
        {
            var document = new ClashReportDocument(
                null, null, new[] { new ClashBatch("Test 1", null, Array.Empty<ClashResult>()) });

            string html = new HtmlReportRenderer().Render(
                new GroupedClashReport(document, Array.Empty<ClashGroup>(), rawCount: 0));

            Assert.Contains("<h1>Coordination clash report</h1>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("class=\"subtitle\"", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_SaysWhichUnitADistanceIsIn()
        {
            string withUnits = new HtmlReportRenderer().Render(BuildSampleReport());

            Assert.Contains("<th>Distance (m)</th>", withUnits, StringComparison.Ordinal);
            Assert.Contains("<th>Point (m)</th>", withUnits, StringComparison.Ordinal);
            Assert.Contains("<dt>Units</dt><dd>m</dd>", withUnits, StringComparison.Ordinal);

            // A source that declared no unit must not have one invented for it.
            var document = new ClashReportDocument(
                "sample.nwd", null, new[] { new ClashBatch("Test 1", null, Array.Empty<ClashResult>()) });
            string withoutUnits = new HtmlReportRenderer().Render(
                new GroupedClashReport(document, Array.Empty<ClashGroup>(), rawCount: 0));

            Assert.DoesNotContain("<dt>Units</dt>", withoutUnits, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_ShowsWhenItWasProducedOnlyWhenItIsTold()
        {
            var moment = new DateTimeOffset(2026, 8, 3, 15, 42, 0, TimeSpan.FromHours(2));

            string stamped = new HtmlReportRenderer(ReportLanguage.English, moment).Render(BuildSampleReport());
            Assert.Contains("<dt>Generated</dt><dd>2026-08-03 13:42 UTC</dd>", stamped, StringComparison.Ordinal);

            // No clock is read for the renderer, so an unstamped report stays byte-stable.
            string unstamped = new HtmlReportRenderer().Render(BuildSampleReport());
            Assert.DoesNotContain("<dt>Generated</dt>", unstamped, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_WritesItsOwnWordingInPortugueseWithoutTranslatingTheRecord()
        {
            string html = new HtmlReportRenderer(ReportLanguage.Portuguese, null).Render(BuildSampleReport());

            Assert.Contains("<html lang=\"pt\">", html, StringComparison.Ordinal);
            Assert.Contains("Relatório de conflitos de coordenação", html, StringComparison.Ordinal);
            Assert.Contains("2 conflitos brutos &rarr; 2 grupos", html, StringComparison.Ordinal);
            Assert.Contains("<th>Distância (m)</th>", html, StringComparison.Ordinal);
            Assert.Contains("<dt>Modelo de origem</dt>", html, StringComparison.Ordinal);

            // What the coordination record actually says is reproduced, never translated: statuses,
            // disciplines, levels, clash test names, and element ids are the source's own words.
            Assert.Contains(">New<", html, StringComparison.Ordinal);
            Assert.Contains(">Approved<", html, StringComparison.Ordinal);
            Assert.Contains("Architecture", html, StringComparison.Ordinal);
            Assert.Contains("Structure", html, StringComparison.Ordinal);
            Assert.Contains("Test 1", html, StringComparison.Ordinal);
            Assert.Contains("avac-1", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_ProducesTheSameBytesInEitherLanguage()
        {
            var report = BuildSampleReport();
            var renderer = new HtmlReportRenderer(ReportLanguage.Portuguese, null);

            Assert.Equal(renderer.Render(report), renderer.Render(report), StringComparer.Ordinal);
            Assert.DoesNotContain("\r", renderer.Render(report), StringComparison.Ordinal);
        }

        [Fact]
        public void Render_NeverPrintsTheExportingMachinesPathsInADisciplineBadge()
        {
            // The discipline resolver reports whatever the export's path hierarchy held, and in a
            // real Navisworks export that is routinely the nested model's absolute path. The report
            // leaves the office; the folder chain must not go with it.
            const string hvac = @"C:\Clients\Acme Confidential\Disciplines\HVAC\Acme_HVAC_R00.rvt";
            const string structure = @"C:\Clients\Acme Confidential\Disciplines\STR\Acme_STR_R00.rvt";

            var elementA = new ClashObject("a", null, "L1", null, null, null);
            var elementB = new ClashObject("b", null, "L1", null, null, null);
            var clash = new ClashResult(
                "Clash1", ClashStatus.New, null, null, null, elementA, elementB,
                "44444444-4444-4444-4444-444444444444");

            var document = new ClashReportDocument(
                "Federated.nwd", null, new[] { new ClashBatch("Test 1", null, new[] { clash }) });
            var group = new ClashGroup("Test 1", hvac, structure, "L1", new[] { clash });

            string html = new HtmlReportRenderer().Render(
                new GroupedClashReport(document, new[] { group }, rawCount: 1));

            Assert.DoesNotContain("Acme Confidential", html, StringComparison.Ordinal);
            Assert.DoesNotContain(@"C:\", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Disciplines", html, StringComparison.Ordinal);

            // The model's own name is what a reader needs, and it is still there.
            Assert.Contains(">Acme_HVAC_R00<", html, StringComparison.Ordinal);
            Assert.Contains(">Acme_STR_R00<", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_LeavesAnOrdinaryDisciplineNameAlone()
        {
            string html = new HtmlReportRenderer().Render(BuildSampleReport());

            Assert.Contains(">Architecture<", html, StringComparison.Ordinal);
            Assert.Contains(">AVAC<", html, StringComparison.Ordinal);
            Assert.Contains(">Structure<", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_WritesItsOwnWordingWithoutTurningAccentsIntoEntities()
        {
            // The document declares UTF-8, so escaping its own Portuguese would only make the file
            // larger and unreadable at source. Content read from the export is still escaped.
            string html = new HtmlReportRenderer(ReportLanguage.Portuguese, null).Render(BuildSampleReport());

            Assert.Contains("Distância", html, StringComparison.Ordinal);
            Assert.Contains("Modelo de origem", html, StringComparison.Ordinal);
            Assert.DoesNotContain("&#237;", html, StringComparison.Ordinal);
            Assert.DoesNotContain("&#226;", html, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(ReportLanguage.English)]
        [InlineData(ReportLanguage.Portuguese)]
        public void TheReportsOwnWordingCannotIntroduceMarkup(ReportLanguage language)
        {
            // Its own wording is written unescaped, so a future edit that put a tag in a label would
            // otherwise become an injection point rather than a typo.
            string html = new HtmlReportRenderer(language, null).Render(BuildSampleReport());

            foreach (string fragment in new[] { "<h1>", "<p class=\"subtitle\">", "<dt>", "<th>" })
            {
                int at = 0;
                while ((at = html.IndexOf(fragment, at, StringComparison.Ordinal)) >= 0)
                {
                    at += fragment.Length;
                    int close = html.IndexOf('<', at);
                    Assert.True(close >= at, "Every element the report writes for itself must be closed.");

                    string inner = html.Substring(at, close - at);
                    Assert.DoesNotContain(">", inner, StringComparison.Ordinal);
                    Assert.DoesNotContain("&#", inner, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void Render_KeepsAGroupTogetherOnAPrintedPage()
        {
            string html = new HtmlReportRenderer().Render(BuildSampleReport());

            Assert.Contains("@media print", html, StringComparison.Ordinal);

            // A group split across a page break is unreadable, and a table whose header appears only
            // on the first page is worse.
            Assert.Contains("break-inside:avoid", html, StringComparison.Ordinal);
            Assert.Contains("thead{display:table-header-group;}", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_MatchesGoldenFile()
        {
            var report = BuildSampleReport();
            var renderer = new HtmlReportRenderer();

            string actual = renderer.Render(report);
            string goldenPath = Path.Combine(AppContext.BaseDirectory, "Golden", "report.golden.html");
            string expected = File.ReadAllText(goldenPath);

            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }
    }
}
