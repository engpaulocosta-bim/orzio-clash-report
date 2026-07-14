using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Assembly;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Input.RunManifestJson;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for ExactSourceModelCoordinationRunAssembler: batch-name requirement, exact
    /// SourceFileName/SourceFilePath resolution, rejection of every filename/path heuristic, missing
    /// SourceModel, zero and ambiguous matches, order/reference preservation, executed-clash-test coverage
    /// delegation, empty documents and zero-result tests, duplicate slots, and determinism.
    /// </summary>
    public class ExactSourceModelCoordinationRunAssemblerTests
    {
        private static readonly ExactSourceModelCoordinationRunAssembler Assembler = new ExactSourceModelCoordinationRunAssembler();

        private static readonly DateTimeOffset SampleCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

        private static ModelRevision MakeRevision(
            string company, string discipline, string modelName, string revision, string sourceFileName, string? sourceFilePath = null) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, sourceFileName, sourceFilePath, null, null);

        private static readonly ModelRevision Sigma = MakeRevision("Sigma", "Structure", "Main", "R04", "Sigma_Main_R04.nwc");
        private static readonly ModelRevision Alfa = MakeRevision("Alfa", "HVAC", "Main", "R07", "Alfa_Main_R07.nwc");

        private static ClashObject MakeObject(string elementId, string? sourceModel) =>
            new ClashObject(elementId, null, null, sourceModel, null, null);

        private static ClashResult MakeClash(
            string elementIdA, string? sourceModelA, string elementIdB, string? sourceModelB,
            string name = "Clash1", string? guid = "g1") =>
            new ClashResult(
                name, ClashStatus.New, null, null, null,
                MakeObject(elementIdA, sourceModelA), MakeObject(elementIdB, sourceModelB), guid);

        private static ClashBatch MakeBatch(string? name, params ClashResult[] clashes) =>
            new ClashBatch(name, null, clashes);

        private static ClashReportDocument MakeDocument(params ClashBatch[] batches) =>
            new ClashReportDocument(null, null, batches);

        private static ExecutedClashTest TestFor(string name, ModelRevision modelA, ModelRevision modelB) =>
            new ExecutedClashTest(name, modelA.Identity, modelB.Identity);

        private static RunManifest MakeManifest(string runId, ModelRevision[] models, params ExecutedClashTest[] executedClashTests) =>
            new RunManifest(runId, SampleCreatedAt, models, executedClashTests);

        private static readonly ExecutedClashTest StandardTest = TestFor("Test 1", Sigma, Alfa);

        // ===================== 20.2 Arguments =====================

        [Fact]
        public void Assemble_RejectsNullDocument()
        {
            var manifest = MakeManifest("run-1", new[] { Sigma });

            var ex = Assert.Throws<ArgumentNullException>(() => Assembler.Assemble(null!, manifest));
            Assert.Equal("document", ex.ParamName);
        }

        [Fact]
        public void Assemble_RejectsNullManifest()
        {
            var document = MakeDocument();

            var ex = Assert.Throws<ArgumentNullException>(() => Assembler.Assemble(document, null!));
            Assert.Equal("manifest", ex.ParamName);
        }

        // ===================== 20.3 Batch name =====================
        // ClashBatch.Name has no domain-level invariant (the constructor accepts null/empty/whitespace), so the
        // assembler itself must reject a blank batch name.

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Assemble_RejectsBatchWithBlankName(string? name)
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch(name, clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("index 0", ex.Message);
            Assert.Contains("run-1", ex.Message);
        }

        // ===================== 20.4 Resolution by SourceFileName =====================

        [Fact]
        public void Assemble_ExactSourceFileNameMatch_Resolves()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);

            var occurrence = Assert.Single(run.Occurrences);
            Assert.Same(Sigma, occurrence.ModelA);
            Assert.Same(Alfa, occurrence.ModelB);
        }

        [Fact]
        public void Assemble_SourceFileNameCaseDifference_Resolves()
        {
            var clash = MakeClash("a", Sigma.SourceFileName.ToUpperInvariant(), "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);

            Assert.Same(Sigma, Assert.Single(run.Occurrences).ModelA);
        }

        [Fact]
        public void Assemble_SourceModelWithExternalWhitespace_IsTrimmedBeforeMatching()
        {
            var clash = MakeClash("a", $"  {Sigma.SourceFileName}  ", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);

            Assert.Same(Sigma, Assert.Single(run.Occurrences).ModelA);
        }

        [Fact]
        public void Assemble_ReturnsExactModelRevisionInstanceFromManifest()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);

            var occurrence = Assert.Single(run.Occurrences);
            Assert.Same(manifest.Models[0], occurrence.ModelA);
            Assert.Same(manifest.Models[1], occurrence.ModelB);
        }

        // ===================== 20.5 Resolution by SourceFilePath =====================

        [Fact]
        public void Assemble_ExactSourceFilePathMatch_Resolves()
        {
            var sigmaWithPath = MakeRevision("Sigma", "Structure", "Main", "R04", "Sigma_Main_R04.nwc", @"C:\BIM\Models\Sigma_Main_R04.nwc");
            var clash = MakeClash("a", @"C:\BIM\Models\Sigma_Main_R04.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { sigmaWithPath, Alfa }, TestFor("Test 1", sigmaWithPath, Alfa));

            var run = Assembler.Assemble(document, manifest);

            Assert.Same(sigmaWithPath, Assert.Single(run.Occurrences).ModelA);
        }

        [Fact]
        public void Assemble_SourceFilePathCaseDifference_Resolves()
        {
            var sigmaWithPath = MakeRevision("Sigma", "Structure", "Main", "R04", "Sigma_Main_R04.nwc", @"C:\BIM\Models\Sigma_Main_R04.nwc");
            var clash = MakeClash("a", @"c:\bim\models\sigma_main_r04.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { sigmaWithPath, Alfa }, TestFor("Test 1", sigmaWithPath, Alfa));

            var run = Assembler.Assemble(document, manifest);

            Assert.Same(sigmaWithPath, Assert.Single(run.Occurrences).ModelA);
        }

        [Fact]
        public void Assemble_NullSourceFilePath_DoesNotCreateCandidate()
        {
            // Sigma has no SourceFilePath declared; a token equal to some arbitrary path must not match it.
            var clash = MakeClash("a", @"C:\BIM\Models\Sigma_Main_R04.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("no manifest model matched", ex.Message);
        }

        [Fact]
        public void Assemble_FullPathSourceModel_DoesNotMatchDeclaredBasenameOnly_Fails()
        {
            // Blocks Path.GetFileName-style heuristics: the manifest only declares the basename via
            // SourceFileName (no SourceFilePath), so a full-path SourceModel token must not resolve.
            var clash = MakeClash("a", @"C:\BIM\Models\" + Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("no manifest model matched", ex.Message);
        }

        // ===================== 20.6 No heuristics =====================

        [Fact]
        public void Assemble_FileNameWithoutExtension_DoesNotResolve()
        {
            var clash = MakeClash("a", "Sigma_Main_R04", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        [Fact]
        public void Assemble_FileNameWithDifferentExtension_DoesNotResolve()
        {
            var clash = MakeClash("a", "Sigma_Main_R04.rvt", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        [Fact]
        public void Assemble_SubstringOfDeclaredName_DoesNotResolve()
        {
            var clash = MakeClash("a", "X" + Sigma.SourceFileName + "X", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        [Fact]
        public void Assemble_PrefixOfDeclaredName_DoesNotResolve()
        {
            var clash = MakeClash("a", "Sigma_Main", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        [Fact]
        public void Assemble_PartialSuffixOfDeclaredName_DoesNotResolve()
        {
            var clash = MakeClash("a", "ain_R04.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        [Fact]
        public void Assemble_ReversedSlashPath_DoesNotResolveAutomatically()
        {
            var sigmaWithPath = MakeRevision("Sigma", "Structure", "Main", "R04", "Sigma_Main_R04.nwc", @"C:\BIM\Models\Sigma_Main_R04.nwc");
            var clash = MakeClash("a", "C:/BIM/Models/Sigma_Main_R04.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { sigmaWithPath, Alfa });

            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        [Fact]
        public void Assemble_DifferentRevisionInToken_DoesNotResolve()
        {
            var clash = MakeClash("a", "Sigma_Main_R05.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        [Fact]
        public void Assemble_SimilarModelName_DoesNotResolve()
        {
            var clash = MakeClash("a", "Sigma_Main2_R04.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        // ===================== 20.7 Missing SourceModel =====================

        [Fact]
        public void Assemble_ElementASourceModelNull_Fails()
        {
            var clash = MakeClash("a", null, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("side A", ex.Message);
            Assert.Contains("batch index 0", ex.Message);
            Assert.Contains("clash index 0", ex.Message);
            Assert.Contains("Test 1", ex.Message);
            Assert.Contains("run-1", ex.Message);
        }

        [Fact]
        public void Assemble_ElementASourceModelWhitespace_Fails()
        {
            var clash = MakeClash("a", "   ", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("side A", ex.Message);
        }

        [Fact]
        public void Assemble_ElementBSourceModelNull_Fails()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", null);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("side B", ex.Message);
        }

        [Fact]
        public void Assemble_ElementBSourceModelWhitespace_Fails()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", "   ");
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("side B", ex.Message);
        }

        // ===================== 20.8 No match =====================

        [Fact]
        public void Assemble_UnknownSourceModelInA_Fails()
        {
            var clash = MakeClash("a", "Unknown_Model.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("Unknown_Model.nwc", ex.Message);
            Assert.Contains("side A", ex.Message);
            Assert.Contains("batch index 0", ex.Message);
            Assert.Contains("clash index 0", ex.Message);
            Assert.Contains("Test 1", ex.Message);
            Assert.Contains("run-1", ex.Message);
            Assert.Contains("no manifest model matched SourceFileName or SourceFilePath", ex.Message);
        }

        [Fact]
        public void Assemble_UnknownSourceModelInB_Fails()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", "Unknown_Model.nwc");
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("side B", ex.Message);
        }

        // ===================== 20.9 Ambiguity =====================

        [Fact]
        public void Assemble_TwoModelsWithSameSourceFileName_IsAmbiguous()
        {
            var beta = MakeRevision("Beta", "Architecture", "Main", "R10", Sigma.SourceFileName);
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, beta, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("2 distinct", ex.Message);
            Assert.Contains(Sigma.ToString(), ex.Message);
            Assert.Contains(beta.ToString(), ex.Message);
        }

        [Fact]
        public void Assemble_DoesNotSelectFirstCandidate_OnAmbiguity()
        {
            var beta = MakeRevision("Beta", "Architecture", "Main", "R10", Sigma.SourceFileName);
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, beta, Alfa }, TestFor("Test 1", Sigma, Alfa));

            // No occurrence is produced at all: ambiguity is a hard failure, not a fallback to Sigma (the first candidate).
            Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
        }

        [Fact]
        public void Assemble_SourceFileNameOfOneModel_EqualsSourceFilePathOfAnother_IsAmbiguous()
        {
            var beta = MakeRevision("Beta", "Architecture", "Main", "R10", "Beta_Main_R10.nwc", sourceFilePath: Sigma.SourceFileName);
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, beta, Alfa });

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));
            Assert.Contains("2 distinct", ex.Message);
        }

        [Fact]
        public void Assemble_TokenMatchesBothFileNameAndFilePathOfSameModel_IsSingleCandidate()
        {
            var sigmaSameTokenBothFields = MakeRevision("Sigma", "Structure", "Main", "R04", "Sigma_Main_R04.nwc", sourceFilePath: "Sigma_Main_R04.nwc");
            var clash = MakeClash("a", "Sigma_Main_R04.nwc", "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest(
                "run-1", new[] { sigmaSameTokenBothFields, Alfa }, TestFor("Test 1", sigmaSameTokenBothFields, Alfa));

            var run = Assembler.Assemble(document, manifest);

            Assert.Same(sigmaSameTokenBothFields, Assert.Single(run.Occurrences).ModelA);
        }

        // ===================== 20.10 Order and references =====================

        [Fact]
        public void Assemble_PreservesFlatBatchMajorClashMinorOrder()
        {
            var clash00 = MakeClash("a00", Sigma.SourceFileName, "b00", Alfa.SourceFileName, name: "c00");
            var clash01 = MakeClash("a01", Sigma.SourceFileName, "b01", Alfa.SourceFileName, name: "c01");
            var clash10 = MakeClash("a10", Sigma.SourceFileName, "b10", Alfa.SourceFileName, name: "c10");
            var batch0 = MakeBatch("Test 1", clash00, clash01);
            var batch1 = MakeBatch("Test 2", clash10);
            var document = MakeDocument(batch0, batch1);
            var manifest = MakeManifest(
                "run-1", new[] { Sigma, Alfa }, TestFor("Test 1", Sigma, Alfa), TestFor("Test 2", Sigma, Alfa));

            var run = Assembler.Assemble(document, manifest);

            Assert.Equal(3, run.Occurrences.Count);
            Assert.Same(clash00, run.Occurrences[0].Clash);
            Assert.Same(clash01, run.Occurrences[1].Clash);
            Assert.Same(clash10, run.Occurrences[2].Clash);
        }

        [Fact]
        public void Assemble_OccurrencePreservesExactClashReference()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);

            Assert.Same(clash, Assert.Single(run.Occurrences).Clash);
        }

        [Fact]
        public void Assemble_DoesNotSwapAAndB()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);
            var occurrence = Assert.Single(run.Occurrences);

            Assert.Same(Sigma, occurrence.ModelA);
            Assert.Same(Alfa, occurrence.ModelB);
        }

        [Fact]
        public void Assemble_BatchNameBecomesClashTestName()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("HVAC vs Structure", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, TestFor("HVAC vs Structure", Sigma, Alfa));

            var run = Assembler.Assemble(document, manifest);

            Assert.Equal("HVAC vs Structure", Assert.Single(run.Occurrences).ClashTestName);
        }

        // ===================== 20.11 Manifest coverage remains the final authority =====================

        [Fact]
        public void Assemble_ResolvedOccurrence_WithoutDeclaredExecutedTest_FailsViaCoordinationRun()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var document = MakeDocument(MakeBatch("Test 1", clash));
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }); // no ExecutedClashTests declared

            var ex = Assert.Throws<CoordinationRunAssemblyException>(() => Assembler.Assemble(document, manifest));

            Assert.IsType<ArgumentException>(ex.InnerException);
            Assert.Contains("run-1", ex.Message);
        }

        // ===================== 20.12 Empty document and zero-result tests =====================

        [Fact]
        public void Assemble_EmptyDocument_NoExecutedTests_ProducesEmptyRun()
        {
            var document = MakeDocument();
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa });

            var run = Assembler.Assemble(document, manifest);

            Assert.Empty(run.Occurrences);
            Assert.Same(manifest, run.Manifest);
            Assert.Empty(run.ExecutedClashTests);
        }

        [Fact]
        public void Assemble_EmptyDocument_WithDeclaredExecutedTest_ProducesEmptyRun()
        {
            var document = MakeDocument();
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);

            Assert.Empty(run.Occurrences);
            Assert.Same(manifest, run.Manifest);
            Assert.Single(run.ExecutedClashTests);
        }

        [Fact]
        public void Assemble_BatchWithZeroClashes_IsAccepted()
        {
            var batch = MakeBatch("Test 1");
            var document = MakeDocument(batch);
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);

            Assert.Empty(run.Occurrences);
        }

        // ===================== 20.13 Duplicate slots =====================

        [Fact]
        public void Assemble_SameClashResultReferenceTwiceInBatch_ProducesTwoOccurrences()
        {
            var clash = MakeClash("a", Sigma.SourceFileName, "b", Alfa.SourceFileName);
            var batch = MakeBatch("Test 1", clash, clash);
            var document = MakeDocument(batch);
            var manifest = MakeManifest("run-1", new[] { Sigma, Alfa }, StandardTest);

            var run = Assembler.Assemble(document, manifest);

            Assert.Equal(2, run.Occurrences.Count);
            Assert.Same(clash, run.Occurrences[0].Clash);
            Assert.Same(clash, run.Occurrences[1].Clash);
            Assert.NotSame(run.Occurrences[0], run.Occurrences[1]);
        }

        // ===================== 20.14 Determinism =====================

        [Fact]
        public void Assemble_IsDeterministic_ForTheSameInputs()
        {
            var clash0 = MakeClash("a0", Sigma.SourceFileName, "b0", Alfa.SourceFileName, name: "c0");
            var clash1 = MakeClash("a1", Sigma.SourceFileName, "b1", Alfa.SourceFileName, name: "c1");
            var batch0 = MakeBatch("Test 1", clash0);
            var batch1 = MakeBatch("Test 2", clash1);
            var document = MakeDocument(batch0, batch1);
            var manifest = MakeManifest(
                "run-1", new[] { Sigma, Alfa }, TestFor("Test 1", Sigma, Alfa), TestFor("Test 2", Sigma, Alfa));

            var first = Assembler.Assemble(document, manifest);
            var second = Assembler.Assemble(document, manifest);

            Assert.Equal(first.RunId, second.RunId);
            Assert.Equal(first.Occurrences.Count, second.Occurrences.Count);
            for (int i = 0; i < first.Occurrences.Count; i++)
            {
                Assert.Equal(first.Occurrences[i].ClashTestName, second.Occurrences[i].ClashTestName);
                Assert.Same(first.Occurrences[i].Clash, second.Occurrences[i].Clash);
                Assert.Same(first.Occurrences[i].ModelA, second.Occurrences[i].ModelA);
                Assert.Same(first.Occurrences[i].ModelB, second.Occurrences[i].ModelB);
            }
        }

        // ===================== 21. Real adapter integration =====================
        // NavisworksXmlClashSource + JsonRunManifestSource + ExactSourceModelCoordinationRunAssembler,
        // against samples/sample-clash.xml + samples/sample-clash.run-manifest.json.

        private static string SampleClashXmlPath =>
            System.IO.Path.Combine(AppContext.BaseDirectory, "samples", "sample-clash.xml");

        private static string SampleClashManifestPath =>
            System.IO.Path.Combine(AppContext.BaseDirectory, "samples", "sample-clash.run-manifest.json");

        private static (ClashReportDocument Document, RunManifest Manifest) LoadRealFixtures()
        {
            var xmlSource = new OrzioClashReport.Input.NavisworksXml.NavisworksXmlClashSource(SampleClashXmlPath, new RecordingAppLog());
            var document = xmlSource.Read();
            var manifest = new JsonRunManifestSource().Load(SampleClashManifestPath);
            return (document, manifest);
        }

        [Fact]
        public void Integration_RealFixtures_ManifestReferencePreserved()
        {
            var (document, manifest) = LoadRealFixtures();

            var run = Assembler.Assemble(document, manifest);

            Assert.Same(manifest, run.Manifest);
        }

        [Fact]
        public void Integration_RealFixtures_OccurrenceCountMatchesTotalClashes()
        {
            var (document, manifest) = LoadRealFixtures();
            int totalClashes = document.Batches.Sum(b => b.Clashes.Count);

            var run = Assembler.Assemble(document, manifest);

            Assert.Equal(5, totalClashes);
            Assert.Equal(totalClashes, run.Occurrences.Count);
        }

        [Fact]
        public void Integration_RealFixtures_OrderMatchesDocumentOrder()
        {
            var (document, manifest) = LoadRealFixtures();
            var expectedOrder = document.Batches.SelectMany(b => b.Clashes).ToArray();

            var run = Assembler.Assemble(document, manifest);

            Assert.Equal(expectedOrder.Length, run.Occurrences.Count);
            for (int i = 0; i < expectedOrder.Length; i++)
            {
                Assert.Same(expectedOrder[i], run.Occurrences[i].Clash);
            }
        }

        [Fact]
        public void Integration_RealFixtures_ModelReferencesComeFromManifest()
        {
            var (document, manifest) = LoadRealFixtures();

            var run = Assembler.Assemble(document, manifest);

            foreach (var occurrence in run.Occurrences)
            {
                Assert.Contains(occurrence.ModelA, manifest.Models);
                Assert.Contains(occurrence.ModelB, manifest.Models);
            }
        }

        [Fact]
        public void Integration_RealFixtures_EveryOccurrenceIsCoveredByManifest()
        {
            var (document, manifest) = LoadRealFixtures();

            // CoordinationRun's own constructor already enforces coverage; a successful Assemble() call is
            // itself the proof. This test makes that assertion explicit and checks the resulting run's shape.
            var run = Assembler.Assemble(document, manifest);

            Assert.NotEmpty(run.Occurrences);
            Assert.All(run.Occurrences, o => Assert.Equal("Teste 01", o.ClashTestName));
        }

        [Fact]
        public void Integration_RealFixtures_FirstElementASourceModel_IsProjectAHvacToken()
        {
            // The hotfix's proven fact: "Item Source File" wins over "Item Source File Name" for the first
            // clash object in the real fixture.
            var (document, _) = LoadRealFixtures();
            var firstClash = document.Batches[0].Clashes[0];

            Assert.Equal("Project_A_HVAC_PD_R00.rvt", firstClash.ElementA.SourceModel);
        }

        [Fact]
        public void Integration_RealFixtures_FirstElementAResolves_BySourceFileNameNotSourceFilePath()
        {
            var (document, manifest) = LoadRealFixtures();
            var firstClash = document.Batches[0].Clashes[0];

            var run = Assembler.Assemble(document, manifest);
            var firstOccurrence = run.Occurrences[0];

            Assert.Same(firstClash, firstOccurrence.Clash);
            var resolvedModel = firstOccurrence.ModelA;

            Assert.Equal("Project_A_HVAC_PD_R00.rvt", resolvedModel.SourceFileName);
            // The manifest declares no SourceFilePath for this model: resolution could not have used it.
            Assert.Null(resolvedModel.SourceFilePath);
        }

        [Fact]
        public void Integration_RealFixtures_AllDistinctSourceModelTokens_HaveExactlyOneManifestMatch()
        {
            var (document, manifest) = LoadRealFixtures();
            var distinctTokens = document.Batches
                .SelectMany(b => b.Clashes)
                .SelectMany(c => new[] { c.ElementA.SourceModel, c.ElementB.SourceModel })
                .Where(t => t != null)
                .Select(t => t!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Inspected fact: the fixture has exactly one distinct SourceModel token (a self-clash scenario).
            Assert.Single(distinctTokens);
            Assert.Equal("Project_A_HVAC_PD_R00.rvt", distinctTokens[0]);

            foreach (var token in distinctTokens)
            {
                int matchCount = manifest.Models.Count(m =>
                    string.Equals(token, m.SourceFileName, StringComparison.OrdinalIgnoreCase)
                    || (m.SourceFilePath != null && string.Equals(token, m.SourceFilePath, StringComparison.OrdinalIgnoreCase)));

                Assert.Equal(1, matchCount);
            }
        }

        [Fact]
        public void Integration_RealFixtures_IsSelfClash_BothSidesResolveToSameModelRevision()
        {
            var (document, manifest) = LoadRealFixtures();

            var run = Assembler.Assemble(document, manifest);

            Assert.All(run.Occurrences, o => Assert.Same(o.ModelA, o.ModelB));
        }

        // ===================== Bonus: JsonRunManifestSource + assembler composition =====================

        [Fact]
        public void Assemble_WithManifestParsedByRealJsonAdapter_Resolves()
        {
            const string manifestJson = """
                {
                  "schemaVersion": 2,
                  "runId": "run-json-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" },
                    { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC", "revision": "R07", "sourceFileName": "Alfa_HVAC_R07.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "HVAC vs Structure",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
                    }
                  ]
                }
                """;

            var manifest = new JsonRunManifestSource().Parse(manifestJson);
            var clash = MakeClash("a", "Sigma_Structure_R04.nwc", "b", "Alfa_HVAC_R07.nwc");
            var document = MakeDocument(MakeBatch("HVAC vs Structure", clash));

            var run = Assembler.Assemble(document, manifest);

            var occurrence = Assert.Single(run.Occurrences);
            Assert.Same(manifest.Models[0], occurrence.ModelA);
            Assert.Same(manifest.Models[1], occurrence.ModelB);
        }
    }
}
