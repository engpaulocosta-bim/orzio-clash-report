using System;
using System.IO;
using System.Linq;
using OrzioClashReport.Core.Grouping;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Input.NavisworksXml;
using OrzioClashReport.Output.Html;

namespace OrzioClashReport.Tests
{
    public class MissingElementGuidTests
    {
        [Fact]
        public void GroupAndRender_KeepsPartialPairsAndAuditsOnlyIdentifiedDuplicates()
        {
            string partial = Object(null, "Structure") + Object("b", "Services");
            string unidentified = Object(null, "Structure") + Object(null, "Services");
            string identified = Object("a", "Structure") + Object("b", "Services");
            string results = Result("Partial 1", partial) + Result("Partial 2", partial)
                + Result("Identified 1", identified) + Result("Identified 2", identified)
                + Result("Unidentified 1", unidentified) + Result("Unidentified 2", unidentified);
            var report = new RuleBasedGrouper(new PathHierarchyDisciplineResolver()).Group(ReadExport(results));

            var group = Assert.Single(report.Groups);
            Assert.Equal("Test", group.ClashTestName);
            Assert.Equal("Services", group.DisciplineA);
            Assert.Equal("Structure", group.DisciplineB);
            Assert.Equal("L1", group.Level);
            Assert.Equal(
                new[] { "Identified 1", "Partial 1", "Partial 2", "Unidentified 1", "Unidentified 2" },
                group.Members.Select(clash => clash.Name).OrderBy(name => name, StringComparer.Ordinal));
            var missingElement = Assert.Single(group.Members, clash => clash.Name == "Partial 1").ElementA;
            Assert.Equal("structure.nwc", missingElement.SourceModel);
            Assert.Equal("Pipe", missingElement.Properties["Item Name"]);
            Assert.Equal("Identified 2", Assert.Single(report.Collapses).CollapsedClash.Name);

            string html = new HtmlReportRenderer().Render(report);
            Assert.Contains("6 raw clashes = 5 retained + 1 collapsed duplicates", html, StringComparison.Ordinal);
            Assert.Contains("6 elements without GUID across 4 clashes", html, StringComparison.Ordinal);
            Assert.Contains("excluded from automatic deduplication", html, StringComparison.Ordinal);
            Assert.Contains("<td>GUID unavailable</td><td>b</td>", html, StringComparison.Ordinal);
            Assert.Contains("<td>GUID unavailable</td><td>GUID unavailable</td>", html, StringComparison.Ordinal);
        }

        private static ClashReportDocument ReadExport(string results)
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, $"""
                    <exchange><batchtest><clashtests><clashtest name="Test" tolerance="0.001">
                    <clashresults>{results}</clashresults>
                    </clashtest></clashtests></batchtest></exchange>
                    """);
                return new NavisworksXmlClashSource(path, new RecordingAppLog()).Read();
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string Result(string name, string elements) => $"""
            <clashresult name="{name}" status="new">
            <clashpoint><pos3f x="1" y="2" z="3" /></clashpoint>
            <clashobjects>{elements}</clashobjects>
            </clashresult>
            """;

        private static string Object(string? guid, string discipline)
        {
            string attribute = guid == null
                ? string.Empty
                : $"<objectattribute><name>GUID</name><value>{guid}</value></objectattribute>";
            return $"""
                <clashobject>{attribute}<layer>L1</layer>
                <pathlink><node>Project</node><node>Federation</node><node>{discipline}</node></pathlink>
                <smarttags>
                <smarttag><name>Item Name</name><value>Pipe</value></smarttag>
                <smarttag><name>Item Source File</name><value>structure.nwc</value></smarttag>
                </smarttags></clashobject>
                """;
        }
    }
}
