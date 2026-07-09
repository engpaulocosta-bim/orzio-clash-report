using System;
using System.IO;
using OrzioClashReport.Core.Grouping;
using OrzioClashReport.Input.NavisworksXml;
using Xunit.Abstractions;

namespace OrzioClashReport.Tests
{
    /// <summary>Sanity-checks RuleBasedGrouper end to end on the real multi-discipline fixture: asserts grouping actually reduces the raw count.</summary>
    public class GroupingRealFixtureTests
    {
        private readonly ITestOutputHelper _output;

        public GroupingRealFixtureTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Group_ReducesRawClashesToFewerGroupsOnRealExport()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "samples", "sample-clash2.xml");
            var source = new NavisworksXmlClashSource(filePath, new RecordingAppLog());
            var document = source.Read();

            var grouper = new RuleBasedGrouper(new PathHierarchyDisciplineResolver());
            var report = grouper.Group(document);

            _output.WriteLine($"{report.RawCount} raw clashes -> {report.GroupCount} groups");
            foreach (var group in report.Groups)
            {
                _output.WriteLine($"  {group.DisciplineA} x {group.DisciplineB} @ {group.Level}: {group.Members.Count}");
            }

            Assert.Equal(1458, report.RawCount);
            Assert.True(
                report.GroupCount < report.RawCount,
                $"Expected grouping to reduce {report.RawCount} raw clashes into fewer groups, got {report.GroupCount}.");
        }
    }
}
