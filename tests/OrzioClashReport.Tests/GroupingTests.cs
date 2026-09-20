using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Grouping;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    public class GroupingTests
    {
        private static ClashObject MakeObject(string id, string? level) =>
            new ClashObject(id, null, level, null, null, null);

        private static ClashResult MakeClash(
            string name, string guid, ClashObject elementA, ClashObject elementB, ClashPoint? point = null) =>
            new ClashResult(name, ClashStatus.New, null, null, point, elementA, elementB, guid);

        private static RuleBasedGrouper MakeGrouper(IReadOnlyDictionary<string, string> disciplineByElementId) =>
            new RuleBasedGrouper(new FakeDisciplineResolver(disciplineByElementId));

        [Fact]
        public void Group_BucketsClashesWithSameDisciplinePairAndLevel()
        {
            var avac1 = MakeObject("avac-1", "L1");
            var arch1 = MakeObject("arch-1", "L1");
            var avac2 = MakeObject("avac-2", "L1");
            var arch2 = MakeObject("arch-2", "L1");

            var clash1 = MakeClash("Clash1", "g1", avac1, arch1);
            var clash2 = MakeClash("Clash2", "g2", avac2, arch2);

            var disciplines = new Dictionary<string, string>
            {
                ["avac-1"] = "AVAC", ["arch-1"] = "Architecture",
                ["avac-2"] = "AVAC", ["arch-2"] = "Architecture"
            };

            var document = new ClashReportDocument(
                "doc", null, new[] { new ClashBatch("Test", null, new[] { clash1, clash2 }) });

            var report = MakeGrouper(disciplines).Group(document);

            Assert.Equal(2, report.RawCount);
            var group = Assert.Single(report.Groups);
            Assert.Equal(2, group.Members.Count);
        }

        [Fact]
        public void Group_TreatsDisciplinePairAsOrderIndependent()
        {
            var avac = MakeObject("avac-1", "L1");
            var arch = MakeObject("arch-1", "L1");

            // Same two disciplines, but elementA/elementB swapped between the two clashes.
            var clash1 = MakeClash("Clash1", "g1", avac, arch);
            var clash2 = MakeClash("Clash2", "g2", arch, avac);

            var disciplines = new Dictionary<string, string> { ["avac-1"] = "AVAC", ["arch-1"] = "Architecture" };

            var document = new ClashReportDocument(
                "doc", null, new[] { new ClashBatch("Test", null, new[] { clash1, clash2 }) });

            var report = MakeGrouper(disciplines).Group(document);

            var group = Assert.Single(report.Groups);
            Assert.Equal(2, group.Members.Count);
        }

        [Fact]
        public void Group_SeparatesClashesByLevel()
        {
            var avacL1 = MakeObject("avac-1", "L1");
            var archL1 = MakeObject("arch-1", "L1");
            var avacL2 = MakeObject("avac-2", "L2");
            var archL2 = MakeObject("arch-2", "L2");

            var clashL1 = MakeClash("Clash1", "g1", avacL1, archL1);
            var clashL2 = MakeClash("Clash2", "g2", avacL2, archL2);

            var disciplines = new Dictionary<string, string>
            {
                ["avac-1"] = "AVAC", ["arch-1"] = "Architecture",
                ["avac-2"] = "AVAC", ["arch-2"] = "Architecture"
            };

            var document = new ClashReportDocument(
                "doc", null, new[] { new ClashBatch("Test", null, new[] { clashL1, clashL2 }) });

            var report = MakeGrouper(disciplines).Group(document);

            Assert.Equal(2, report.GroupCount);
        }

        [Fact]
        public void Group_CollapsesSameElementPairWithinTolerance()
        {
            var avac = MakeObject("avac-1", "L1");
            var arch = MakeObject("arch-1", "L1");

            var clash1 = MakeClash("Clash1", "g1", avac, arch, new ClashPoint(1.0, 1.0, 1.0));
            var clash2 = MakeClash("Clash2", "g2", avac, arch, new ClashPoint(1.0000001, 1.0, 1.0));

            var disciplines = new Dictionary<string, string> { ["avac-1"] = "AVAC", ["arch-1"] = "Architecture" };

            var document = new ClashReportDocument(
                "doc", null, new[] { new ClashBatch("Test", 0.001, new[] { clash1, clash2 }) });

            var report = MakeGrouper(disciplines).Group(document);

            Assert.Equal(2, report.RawCount);
            Assert.Equal(1, report.RetainedCount);
            Assert.Equal(1, report.CollapsedCount);
            var group = Assert.Single(report.Groups);
            Assert.Single(group.Members);
            Assert.Same(clash1, group.Members[0]);

            ClashCollapse collapse = Assert.Single(report.Collapses);
            Assert.Equal("Test", collapse.ClashTestName);
            Assert.Same(clash1, collapse.RetainedClash);
            Assert.Same(clash2, collapse.CollapsedClash);
        }

        [Fact]
        public void Group_KeepsSameElementPairSeparateWhenBeyondTolerance()
        {
            var avac = MakeObject("avac-1", "L1");
            var arch = MakeObject("arch-1", "L1");

            var clash1 = MakeClash("Clash1", "g1", avac, arch, new ClashPoint(0.0, 0.0, 0.0));
            var clash2 = MakeClash("Clash2", "g2", avac, arch, new ClashPoint(5.0, 5.0, 5.0));

            var disciplines = new Dictionary<string, string> { ["avac-1"] = "AVAC", ["arch-1"] = "Architecture" };

            var document = new ClashReportDocument(
                "doc", null, new[] { new ClashBatch("Test", 0.001, new[] { clash1, clash2 }) });

            var report = MakeGrouper(disciplines).Group(document);

            Assert.Equal(2, report.RawCount);
            Assert.Equal(2, report.RetainedCount);
            Assert.Equal(0, report.CollapsedCount);
            Assert.Empty(report.Collapses);
            var group = Assert.Single(report.Groups);
            Assert.Equal(2, group.Members.Count);
        }

        [Fact]
        public void Group_PreservesCollapseRecordsInBatchAndSourceOrder()
        {
            var avac1 = MakeObject("avac-1", "L1");
            var arch1 = MakeObject("arch-1", "L1");
            var avac2 = MakeObject("avac-2", "L2");
            var arch2 = MakeObject("arch-2", "L2");

            var retainedA = MakeClash("Retained A", "g1", avac1, arch1, new ClashPoint(1, 1, 1));
            var collapsedA = MakeClash("Collapsed A", "g2", arch1, avac1, new ClashPoint(1, 1, 1.0001));
            var retainedB = MakeClash("Retained B", "g3", avac2, arch2, new ClashPoint(2, 2, 2));
            var collapsedB = MakeClash("Collapsed B", "g4", avac2, arch2, new ClashPoint(2, 2.0001, 2));

            var disciplines = new Dictionary<string, string>
            {
                ["avac-1"] = "AVAC", ["arch-1"] = "Architecture",
                ["avac-2"] = "AVAC", ["arch-2"] = "Architecture"
            };

            var document = new ClashReportDocument(
                "doc",
                null,
                new[]
                {
                    new ClashBatch("Test A", 0.001, new[] { retainedA, collapsedA }),
                    new ClashBatch("Test B", 0.001, new[] { retainedB, collapsedB })
                });

            GroupedClashReport report = MakeGrouper(disciplines).Group(document);

            Assert.Equal(4, report.RawCount);
            Assert.Equal(2, report.RetainedCount);
            Assert.Equal(2, report.CollapsedCount);
            Assert.Equal(new[] { "Test A", "Test B" }, report.Collapses.Select(c => c.ClashTestName));
            Assert.Same(collapsedA, report.Collapses[0].CollapsedClash);
            Assert.Same(collapsedB, report.Collapses[1].CollapsedClash);
        }

        [Fact]
        public void Group_DoesNotCollapseWhenPointIsMissing()
        {
            var avac = MakeObject("avac-1", "L1");
            var arch = MakeObject("arch-1", "L1");

            var clash1 = MakeClash("Clash1", "g1", avac, arch);
            var clash2 = MakeClash("Clash2", "g2", avac, arch);

            var disciplines = new Dictionary<string, string> { ["avac-1"] = "AVAC", ["arch-1"] = "Architecture" };

            var document = new ClashReportDocument(
                "doc", null, new[] { new ClashBatch("Test", 0.001, new[] { clash1, clash2 }) });

            var report = MakeGrouper(disciplines).Group(document);

            var group = Assert.Single(report.Groups);
            Assert.Equal(2, group.Members.Count);
        }

        [Fact]
        public void Group_DoesNotMixClashesFromDifferentClashTestsWithSameDisciplinePairAndLevel()
        {
            var avac1 = MakeObject("avac-1", "L1");
            var arch1 = MakeObject("arch-1", "L1");
            var avac2 = MakeObject("avac-2", "L1");
            var arch2 = MakeObject("arch-2", "L1");

            var clash1 = MakeClash("Clash1", "g1", avac1, arch1);
            var clash2 = MakeClash("Clash2", "g2", avac2, arch2);

            var disciplines = new Dictionary<string, string>
            {
                ["avac-1"] = "AVAC", ["arch-1"] = "Architecture",
                ["avac-2"] = "AVAC", ["arch-2"] = "Architecture"
            };

            var document = new ClashReportDocument(
                "doc",
                null,
                new[]
                {
                    new ClashBatch("Test A", null, new[] { clash1 }),
                    new ClashBatch("Test B", null, new[] { clash2 })
                });

            var report = MakeGrouper(disciplines).Group(document);

            Assert.Equal(2, report.GroupCount);
            Assert.Equal(new[] { "Test A", "Test B" }, report.Groups.Select(g => g.ClashTestName).OrderBy(n => n));
        }

        [Fact]
        public void Group_SetsClashTestNameOnGroup()
        {
            var avac = MakeObject("avac-1", "L1");
            var arch = MakeObject("arch-1", "L1");
            var clash = MakeClash("Clash1", "g1", avac, arch);

            var disciplines = new Dictionary<string, string> { ["avac-1"] = "AVAC", ["arch-1"] = "Architecture" };

            var document = new ClashReportDocument(
                "doc", null, new[] { new ClashBatch("Teste 01", null, new[] { clash }) });

            var report = MakeGrouper(disciplines).Group(document);

            var group = Assert.Single(report.Groups);
            Assert.Equal("Teste 01", group.ClashTestName);
        }

        [Fact]
        public void Group_SortsGroupsByDisciplineThenLevel()
        {
            var mepL2 = MakeObject("mep-2", "L2");
            var archL2 = MakeObject("arch-2", "L2");
            var mepL1 = MakeObject("mep-1", "L1");
            var archL1 = MakeObject("arch-1", "L1");

            var clashL2 = MakeClash("Clash1", "g1", mepL2, archL2);
            var clashL1 = MakeClash("Clash2", "g2", mepL1, archL1);

            var disciplines = new Dictionary<string, string>
            {
                ["mep-1"] = "MEP", ["arch-1"] = "Architecture",
                ["mep-2"] = "MEP", ["arch-2"] = "Architecture"
            };

            var document = new ClashReportDocument(
                "doc", null, new[] { new ClashBatch("Test", null, new[] { clashL2, clashL1 }) });

            var report = MakeGrouper(disciplines).Group(document);

            Assert.Equal(2, report.Groups.Count);
            Assert.Equal("L1", report.Groups[0].Level);
            Assert.Equal("L2", report.Groups[1].Level);
        }
    }
}
