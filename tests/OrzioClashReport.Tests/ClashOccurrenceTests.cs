using System;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Unit tests for ClashOccurrence: a single clash observed in a clash test, linked to the exact ModelRevision on each side.</summary>
    public class ClashOccurrenceTests
    {
        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static ClashObject MakeObject(string id) => new ClashObject(id, null, null, null, null, null);

        private static ClashResult MakeClash(
            string? name, string? guid, ClashObject? elementA = null, ClashObject? elementB = null, ClashPoint? point = null, double? distance = null) =>
            new ClashResult(
                name, ClashStatus.New, distance, null, point,
                elementA ?? MakeObject("a"), elementB ?? MakeObject("b"), guid);

        private static readonly ModelRevision StructureR04 = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04");
        private static readonly ModelRevision HvacR07 = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07");

        [Fact]
        public void Constructor_RejectsNullClashTestName()
        {
            var clash = MakeClash("Clash1", "g1");

            Assert.Throws<ArgumentNullException>(() => new ClashOccurrence(null!, clash, StructureR04, HvacR07));
        }

        [Fact]
        public void Constructor_RejectsEmptyClashTestName()
        {
            var clash = MakeClash("Clash1", "g1");

            Assert.Throws<ArgumentException>(() => new ClashOccurrence("", clash, StructureR04, HvacR07));
        }

        [Fact]
        public void Constructor_RejectsWhitespaceClashTestName()
        {
            var clash = MakeClash("Clash1", "g1");

            Assert.Throws<ArgumentException>(() => new ClashOccurrence("   ", clash, StructureR04, HvacR07));
        }

        [Fact]
        public void Constructor_TrimsClashTestName()
        {
            var clash = MakeClash("Clash1", "g1");

            var occurrence = new ClashOccurrence("  Test 1  ", clash, StructureR04, HvacR07);

            Assert.Equal("Test 1", occurrence.ClashTestName);
        }

        [Fact]
        public void Constructor_RejectsNullClash()
        {
            Assert.Throws<ArgumentNullException>(() => new ClashOccurrence("Test 1", null!, StructureR04, HvacR07));
        }

        [Fact]
        public void Constructor_RejectsNullModelA()
        {
            var clash = MakeClash("Clash1", "g1");

            Assert.Throws<ArgumentNullException>(() => new ClashOccurrence("Test 1", clash, null!, HvacR07));
        }

        [Fact]
        public void Constructor_RejectsNullModelB()
        {
            var clash = MakeClash("Clash1", "g1");

            Assert.Throws<ArgumentNullException>(() => new ClashOccurrence("Test 1", clash, StructureR04, null!));
        }

        [Fact]
        public void Constructor_PreservesClashReference()
        {
            var clash = MakeClash("Clash1", "g1");

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Same(clash, occurrence.Clash);
        }

        [Fact]
        public void Constructor_PreservesModelAReference()
        {
            var clash = MakeClash("Clash1", "g1");

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Same(StructureR04, occurrence.ModelA);
        }

        [Fact]
        public void Constructor_PreservesModelBReference()
        {
            var clash = MakeClash("Clash1", "g1");

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Same(HvacR07, occurrence.ModelB);
        }

        [Fact]
        public void Constructor_PreservesAToBCorrespondenceAndOrder()
        {
            var elementA = MakeObject("elem-a");
            var elementB = MakeObject("elem-b");
            var clash = MakeClash("Clash1", "g1", elementA, elementB);

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Same(elementA, occurrence.Clash.ElementA);
            Assert.Same(elementB, occurrence.Clash.ElementB);
            Assert.Same(StructureR04, occurrence.ModelA);
            Assert.Same(HvacR07, occurrence.ModelB);
        }

        [Fact]
        public void Constructor_AcceptsModelAAndModelBBeingEqual_SelfClash()
        {
            var clash = MakeClash("Clash1", "g1");

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, StructureR04);

            Assert.Same(StructureR04, occurrence.ModelA);
            Assert.Same(StructureR04, occurrence.ModelB);
        }

        [Fact]
        public void Constructor_AcceptsNullClashGuid()
        {
            var clash = MakeClash("Clash1", null);

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Null(occurrence.Clash.Guid);
        }

        [Fact]
        public void Constructor_AcceptsNullClashName()
        {
            var clash = MakeClash(null, "g1");

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Null(occurrence.Clash.Name);
        }

        [Fact]
        public void Constructor_AcceptsNullPoint()
        {
            var clash = MakeClash("Clash1", "g1", point: null);

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Null(occurrence.Clash.Point);
        }

        [Fact]
        public void Constructor_AcceptsNullDistance()
        {
            var clash = MakeClash("Clash1", "g1", distance: null);

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Null(occurrence.Clash.Distance);
        }

        [Fact]
        public void ToString_IncludesClashTestName()
        {
            var clash = MakeClash("Clash1", "g1");

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            Assert.Contains("Test 1", occurrence.ToString());
        }

        [Fact]
        public void ToString_IncludesModelARevisionAndModelBRevision()
        {
            var clash = MakeClash("Clash1", "g1");

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            string text = occurrence.ToString();
            Assert.Contains(StructureR04.ToString(), text);
            Assert.Contains(HvacR07.ToString(), text);
        }

        [Fact]
        public void ToString_HasReadableFallback_WhenNameAndGuidAreNull()
        {
            var clash = MakeClash(null, null);

            var occurrence = new ClashOccurrence("Test 1", clash, StructureR04, HvacR07);

            string text = occurrence.ToString();
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.Contains("Test 1", text);
        }
    }
}
