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
                "sample.nwd", null, new[] { new ClashBatch("Test 1", 0.001, new[] { clash1, clash2 }) });

            var groups = new List<ClashGroup>
            {
                new ClashGroup("Architecture", "AVAC", "L1", new[] { clash1 }),
                new ClashGroup("AVAC", "Structure", "L2", new[] { clash2 })
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
