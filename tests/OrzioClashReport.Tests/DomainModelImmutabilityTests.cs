using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Proves domain model constructors defensively copy caller-supplied collections, so mutating the original after construction cannot change internal state.</summary>
    public class DomainModelImmutabilityTests
    {
        [Fact]
        public void ClashObject_PathHierarchy_IsUnaffectedByMutatingSourceListAfterConstruction()
        {
            var source = new List<string> { "File", "Model.nwd" };
            var clashObject = new ClashObject("id-1", null, null, null, source, null);

            source.Add("Injected");

            Assert.Equal(new[] { "File", "Model.nwd" }, clashObject.PathHierarchy);
        }

        [Fact]
        public void ClashObject_Properties_IsUnaffectedByMutatingSourceDictionaryAfterConstruction()
        {
            var source = new Dictionary<string, string> { ["Item Layer"] = "L1" };
            var clashObject = new ClashObject("id-1", null, null, null, null, source);

            source["Item Layer"] = "Injected";
            source["Extra"] = "Injected";

            Assert.Equal("L1", clashObject.Properties["Item Layer"]);
            Assert.False(clashObject.Properties.ContainsKey("Extra"));
        }

        [Fact]
        public void ClashBatch_Clashes_IsUnaffectedByMutatingSourceListAfterConstruction()
        {
            var elementA = new ClashObject("a", null, null, null, null, null);
            var elementB = new ClashObject("b", null, null, null, null, null);
            var clash = new ClashResult("Clash1", ClashStatus.New, null, null, null, elementA, elementB, "g1");

            var source = new List<ClashResult> { clash };
            var batch = new ClashBatch("Test", null, source);

            source.Clear();

            Assert.Single(batch.Clashes);
        }

        [Fact]
        public void ClashReportDocument_Batches_IsUnaffectedByMutatingSourceListAfterConstruction()
        {
            var batch = new ClashBatch("Test", null, null);
            var source = new List<ClashBatch> { batch };
            var document = new ClashReportDocument("doc", null, source);

            source.Clear();

            Assert.Single(document.Batches);
        }

        [Fact]
        public void ClashGroup_Members_IsUnaffectedByMutatingSourceListAfterConstruction()
        {
            var elementA = new ClashObject("a", null, "L1", null, null, null);
            var elementB = new ClashObject("b", null, "L1", null, null, null);
            var clash = new ClashResult("Clash1", ClashStatus.New, null, null, null, elementA, elementB, "g1");

            var source = new List<ClashResult> { clash };
            var group = new ClashGroup("Test", "Architecture", "AVAC", "L1", source);

            source.Clear();

            Assert.Single(group.Members);
        }

        [Fact]
        public void GroupedClashReport_Groups_IsUnaffectedByMutatingSourceListAfterConstruction()
        {
            var elementA = new ClashObject("a", null, "L1", null, null, null);
            var elementB = new ClashObject("b", null, "L1", null, null, null);
            var clash = new ClashResult("Clash1", ClashStatus.New, null, null, null, elementA, elementB, "g1");
            var group = new ClashGroup("Test", "Architecture", "AVAC", "L1", new[] { clash });

            var document = new ClashReportDocument("doc", null, null);
            var source = new List<ClashGroup> { group };
            var report = new GroupedClashReport(document, source, rawCount: 1);

            source.Clear();

            Assert.Single(report.Groups);
        }
    }
}
