using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Unit tests for CoordinationRun: an immutable snapshot of a RunManifest plus the ClashOccurrences observed in it.</summary>
    public class CoordinationRunTests
    {
        private static readonly DateTimeOffset SampleCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(1));

        private static ModelRevision MakeRevision(
            string company, string discipline, string modelName, string revision, string? sourceFileName = null) =>
            new ModelRevision(
                new ModelIdentity(company, discipline, modelName), revision, sourceFileName ?? $"{modelName}_{revision}.nwc",
                null, null, null);

        private static ClashObject MakeObject(string id) => new ClashObject(id, null, null, null, null, null);

        private static ClashResult MakeClash(string name = "Clash1", string guid = "g1") =>
            new ClashResult(name, ClashStatus.New, null, null, null, MakeObject("a"), MakeObject("b"), guid);

        private static ClashOccurrence MakeOccurrence(ModelRevision modelA, ModelRevision modelB, string clashTestName = "Test 1") =>
            new ClashOccurrence(clashTestName, MakeClash(), modelA, modelB);

        private static ExecutedClashTest TestFor(string name, ModelRevision modelA, ModelRevision modelB) =>
            new ExecutedClashTest(name, modelA.Identity, modelB.Identity);

        private static RunManifest MakeManifest(string runId, ModelRevision[] models, params ExecutedClashTest[] executedClashTests) =>
            new RunManifest(runId, SampleCreatedAt, models, executedClashTests);

        [Fact]
        public void Constructor_RejectsNullManifest()
        {
            Assert.Throws<ArgumentNullException>(() => new CoordinationRun(null!, Array.Empty<ClashOccurrence>()));
        }

        [Fact]
        public void Constructor_RejectsNullOccurrences()
        {
            var manifest = MakeManifest("run-1", new[] { MakeRevision("Sigma", "Structure", "Main", "R04") });

            Assert.Throws<ArgumentNullException>(() => new CoordinationRun(manifest, null!));
        }

        [Fact]
        public void Constructor_AcceptsEmptyOccurrenceList()
        {
            var manifest = MakeManifest("run-1", new[] { MakeRevision("Sigma", "Structure", "Main", "R04") });

            var run = new CoordinationRun(manifest, Array.Empty<ClashOccurrence>());

            Assert.Empty(run.Occurrences);
        }

        [Fact]
        public void Constructor_RejectsNullItemInOccurrences()
        {
            var structureR04 = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { structureR04 }, TestFor("Test 1", structureR04, structureR04));
            var occurrences = new ClashOccurrence?[] { MakeOccurrence(structureR04, structureR04), null };

            Assert.Throws<ArgumentException>(() => new CoordinationRun(manifest, occurrences!));
        }

        [Fact]
        public void Constructor_DefensivelyCopiesOccurrencesList()
        {
            var structureR04 = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { structureR04 }, TestFor("Test 1", structureR04, structureR04));
            var source = new List<ClashOccurrence> { MakeOccurrence(structureR04, structureR04) };

            var run = new CoordinationRun(manifest, source);
            source.Clear();

            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_PreservesDeclaredOrderOfOccurrences()
        {
            var structureR04 = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest(
                "run-1", new[] { structureR04 },
                TestFor("Test A", structureR04, structureR04), TestFor("Test B", structureR04, structureR04));
            var first = MakeOccurrence(structureR04, structureR04, "Test A");
            var second = MakeOccurrence(structureR04, structureR04, "Test B");

            var run = new CoordinationRun(manifest, new[] { second, first });

            Assert.Same(second, run.Occurrences[0]);
            Assert.Same(first, run.Occurrences[1]);
        }

        [Fact]
        public void Constructor_PreservesManifestReference()
        {
            var structureR04 = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { structureR04 });

            var run = new CoordinationRun(manifest, Array.Empty<ClashOccurrence>());

            Assert.Same(manifest, run.Manifest);
        }

        [Fact]
        public void Constructor_AcceptsOccurrence_WhenModelAAndModelBBelongToManifest()
        {
            var structureR04 = MakeRevision("Sigma", "Structure", "Main", "R04");
            var hvacR07 = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { structureR04, hvacR07 }, TestFor("Test 1", structureR04, hvacR07));

            var run = new CoordinationRun(manifest, new[] { MakeOccurrence(structureR04, hvacR07) });

            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_AcceptsEquivalentModelRevision_CreatedAsAnotherInstance()
        {
            var declared = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { declared }, TestFor("Test 1", declared, declared));

            // Same values, different instance.
            var equivalent = MakeRevision("Sigma", "Structure", "Main", "R04");

            var run = new CoordinationRun(manifest, new[] { MakeOccurrence(equivalent, equivalent) });

            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_RejectsModelA_NotDeclaredInManifest()
        {
            var declared = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { declared });
            var undeclared = MakeRevision("Beta", "Architecture", "Main", "R01");

            var ex = Assert.Throws<ArgumentException>(
                () => new CoordinationRun(manifest, new[] { MakeOccurrence(undeclared, declared) }));

            Assert.Contains("index 0", ex.Message);
            Assert.Contains("ModelA", ex.Message);
        }

        [Fact]
        public void Constructor_RejectsModelB_NotDeclaredInManifest()
        {
            var declared = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { declared });
            var undeclared = MakeRevision("Beta", "Architecture", "Main", "R01");

            var ex = Assert.Throws<ArgumentException>(
                () => new CoordinationRun(manifest, new[] { MakeOccurrence(declared, undeclared) }));

            Assert.Contains("index 0", ex.Message);
            Assert.Contains("ModelB", ex.Message);
        }

        [Fact]
        public void Constructor_RejectsSameModelIdentity_WithDifferentRevision_OnSideA()
        {
            var manifestRevision = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-2026-07-10", new[] { manifestRevision });
            var staleRevision = MakeRevision("Sigma", "Structure", "Main", "R03");

            var ex = Assert.Throws<ArgumentException>(
                () => new CoordinationRun(manifest, new[] { MakeOccurrence(staleRevision, manifestRevision) }));

            Assert.Contains("ModelA", ex.Message);
            Assert.Contains("R03", ex.Message);
            Assert.Contains("run-2026-07-10", ex.Message);
        }

        [Fact]
        public void Constructor_RejectsSameModelIdentity_WithDifferentRevision_OnSideB()
        {
            var manifestRevision = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { manifestRevision });
            var staleRevision = MakeRevision("Sigma", "Structure", "Main", "R03");

            var ex = Assert.Throws<ArgumentException>(
                () => new CoordinationRun(manifest, new[] { MakeOccurrence(manifestRevision, staleRevision) }));

            Assert.Contains("ModelB", ex.Message);
            Assert.Contains("R03", ex.Message);
        }

        [Fact]
        public void Constructor_AcceptsSelfClash_WithSameModelRevisionOnBothSides()
        {
            var declared = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { declared }, TestFor("Test 1", declared, declared));

            var run = new CoordinationRun(manifest, new[] { MakeOccurrence(declared, declared) });

            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_AcceptsManifestModel_WithNoOccurrence()
        {
            var declared = MakeRevision("Sigma", "Structure", "Main", "R04");
            var unused = MakeRevision("Beta", "Architecture", "Main", "R01");
            var manifest = MakeManifest("run-1", new[] { declared, unused }, TestFor("Test 1", declared, declared));

            var run = new CoordinationRun(manifest, new[] { MakeOccurrence(declared, declared) });

            Assert.Equal(2, run.Models.Count);
            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_DoesNotDeduplicateRepeatedOccurrences()
        {
            var declared = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { declared }, TestFor("Test 1", declared, declared));
            var occurrence = MakeOccurrence(declared, declared);

            var run = new CoordinationRun(manifest, new[] { occurrence, occurrence, occurrence });

            Assert.Equal(3, run.Occurrences.Count);
        }

        [Fact]
        public void ToString_IncludesRunId()
        {
            var declared = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("coordination-2026-07-10-0900", new[] { declared });

            var run = new CoordinationRun(manifest, Array.Empty<ClashOccurrence>());

            Assert.Contains("coordination-2026-07-10-0900", run.ToString());
        }

        [Fact]
        public void ToString_IncludesModelCount()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa });

            var run = new CoordinationRun(manifest, Array.Empty<ClashOccurrence>());

            Assert.Contains("2 models", run.ToString());
        }

        [Fact]
        public void ToString_IncludesOccurrenceCount()
        {
            var declared = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { declared }, TestFor("Test 1", declared, declared));
            var occurrence = MakeOccurrence(declared, declared);

            var run = new CoordinationRun(manifest, new[] { occurrence, occurrence });

            Assert.Contains("2 occurrences", run.ToString());
        }

        // ===================== Executed clash test coverage =====================

        [Fact]
        public void Constructor_AcceptsOccurrence_WhenTestDeclaredDirect()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa }, TestFor("HVAC vs Structure", sigma, alfa));

            var run = new CoordinationRun(manifest, new[] { MakeOccurrence(sigma, alfa, "HVAC vs Structure") });

            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_AcceptsOccurrence_WhenTestDeclaredInverted()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa }, TestFor("HVAC vs Structure", alfa, sigma));

            var run = new CoordinationRun(manifest, new[] { MakeOccurrence(sigma, alfa, "HVAC vs Structure") });

            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_AcceptsOccurrence_WhenTestNameDiffersOnlyByCase()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa }, TestFor("hvac VS structure", sigma, alfa));

            var run = new CoordinationRun(manifest, new[] { MakeOccurrence(sigma, alfa, "HVAC vs Structure") });

            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_AcceptsSelfClash_WhenTestDeclared()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var manifest = MakeManifest("run-1", new[] { sigma }, TestFor("Self Clash", sigma, sigma));

            var run = new CoordinationRun(manifest, new[] { MakeOccurrence(sigma, sigma, "Self Clash") });

            Assert.Single(run.Occurrences);
        }

        [Fact]
        public void Constructor_RejectsOccurrence_WhenTestNotDeclared()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa });

            var ex = Assert.Throws<ArgumentException>(
                () => new CoordinationRun(manifest, new[] { MakeOccurrence(sigma, alfa, "HVAC vs Structure") }));

            Assert.Contains("index 0", ex.Message);
            Assert.Contains("HVAC vs Structure", ex.Message);
            Assert.Contains("run-1", ex.Message);
        }

        [Fact]
        public void Constructor_RejectsOccurrence_WhenSameNameDeclaredForDifferentModelPair()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var beta = MakeRevision("Beta", "Architecture", "Main", "R10");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa, beta }, TestFor("Coordination", sigma, beta));

            Assert.Throws<ArgumentException>(
                () => new CoordinationRun(manifest, new[] { MakeOccurrence(sigma, alfa, "Coordination") }));
        }

        [Fact]
        public void Constructor_RejectsOccurrence_WhenDeclaredNameDiffersByPunctuation()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa }, TestFor("HVAC-Structure", sigma, alfa));

            Assert.Throws<ArgumentException>(
                () => new CoordinationRun(manifest, new[] { MakeOccurrence(sigma, alfa, "HVAC vs Structure") }));
        }

        [Fact]
        public void Constructor_RejectsOccurrence_WhenDeclaredNameDiffersByNumber()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa }, TestFor("Test 01", sigma, alfa));

            Assert.Throws<ArgumentException>(
                () => new CoordinationRun(manifest, new[] { MakeOccurrence(sigma, alfa, "Test 1") }));
        }

        [Fact]
        public void Constructor_AcceptsDeclaredTest_WithNoMatchingOccurrence()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa }, TestFor("HVAC vs Structure", sigma, alfa));

            var run = new CoordinationRun(manifest, Array.Empty<ClashOccurrence>());

            Assert.Empty(run.Occurrences);
            Assert.Single(run.ExecutedClashTests);
        }

        [Fact]
        public void Constructor_AcceptsZeroOccurrences_WithExecutedTestsDeclared()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest(
                "run-1", new[] { sigma, alfa },
                TestFor("HVAC vs Structure", sigma, alfa), TestFor("Self Clash", sigma, sigma));

            var run = new CoordinationRun(manifest, Array.Empty<ClashOccurrence>());

            Assert.Empty(run.Occurrences);
            Assert.Equal(2, run.ExecutedClashTests.Count);
        }

        [Fact]
        public void ExecutedClashTests_DelegatesToManifestList()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
            var alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
            var manifest = MakeManifest("run-1", new[] { sigma, alfa }, TestFor("HVAC vs Structure", sigma, alfa));

            var run = new CoordinationRun(manifest, Array.Empty<ClashOccurrence>());

            Assert.Same(manifest.ExecutedClashTests, run.ExecutedClashTests);
        }
    }
}
