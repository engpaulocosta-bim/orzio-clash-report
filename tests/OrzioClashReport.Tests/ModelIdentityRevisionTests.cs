using System;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Unit tests for the revision-free ModelIdentity value object and the ModelRevision that composes it with per-revision metadata.</summary>
    public class ModelIdentityRevisionTests
    {
        private static ModelIdentity MakeIdentity(
            string company = "Sigma", string discipline = "Structure", string modelName = "Sigma_Structure") =>
            new ModelIdentity(company, discipline, modelName);

        // --- ModelIdentity construction ---

        [Fact]
        public void ModelIdentity_Constructor_RejectsNullCompany()
        {
            Assert.Throws<ArgumentNullException>(() => new ModelIdentity(null!, "Structure", "Model"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ModelIdentity_Constructor_RejectsEmptyOrWhitespaceCompany(string company)
        {
            Assert.Throws<ArgumentException>(() => new ModelIdentity(company, "Structure", "Model"));
        }

        [Fact]
        public void ModelIdentity_Constructor_RejectsNullDiscipline()
        {
            Assert.Throws<ArgumentNullException>(() => new ModelIdentity("Sigma", null!, "Model"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ModelIdentity_Constructor_RejectsEmptyOrWhitespaceDiscipline(string discipline)
        {
            Assert.Throws<ArgumentException>(() => new ModelIdentity("Sigma", discipline, "Model"));
        }

        [Fact]
        public void ModelIdentity_Constructor_RejectsNullModelName()
        {
            Assert.Throws<ArgumentNullException>(() => new ModelIdentity("Sigma", "Structure", null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ModelIdentity_Constructor_RejectsEmptyOrWhitespaceModelName(string modelName)
        {
            Assert.Throws<ArgumentException>(() => new ModelIdentity("Sigma", "Structure", modelName));
        }

        [Fact]
        public void ModelIdentity_Constructor_TrimsValues()
        {
            var identity = new ModelIdentity("  Sigma  ", "  Structure  ", "  Main Model  ");

            Assert.Equal("Sigma", identity.Company);
            Assert.Equal("Structure", identity.Discipline);
            Assert.Equal("Main Model", identity.ModelName);
        }

        // --- ModelIdentity equality ---

        [Fact]
        public void ModelIdentity_Equality_IgnoresCaseAndOuterWhitespace()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity("sigma", "structure", "main model");
            var c = new ModelIdentity(" Sigma ", " Structure ", " Main Model ");

            Assert.Equal(a, b);
            Assert.Equal(a, c);
            Assert.True(a == b);
            Assert.True(a == c);
        }

        [Fact]
        public void ModelIdentity_Equality_HashCodeMatchesForEqualObjects()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity("sigma", "structure", "main model");

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void ModelIdentity_Equality_DiffersWhenDisciplineDiffers()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity("Sigma", "Architecture", "Main Model");

            Assert.NotEqual(a, b);
            Assert.True(a != b);
        }

        [Fact]
        public void ModelIdentity_Equality_DiffersWhenCompanyDiffers()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity("Beta", "Structure", "Main Model");

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void ModelIdentity_Equality_DiffersWhenModelNameDiffers()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity("Sigma", "Structure", "Secondary Model");

            Assert.NotEqual(a, b);
        }

        // --- ModelIdentity.StableKey ---

        [Fact]
        public void StableKey_IsEqualForEquivalentIdentities()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity(" sigma ", " STRUCTURE ", " main model ");

            Assert.Equal(a.StableKey, b.StableKey);
        }

        [Fact]
        public void StableKey_ChangesWhenCompanyChanges()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity("Beta", "Structure", "Main Model");

            Assert.NotEqual(a.StableKey, b.StableKey);
        }

        [Fact]
        public void StableKey_ChangesWhenDisciplineChanges()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity("Sigma", "Architecture", "Main Model");

            Assert.NotEqual(a.StableKey, b.StableKey);
        }

        [Fact]
        public void StableKey_ChangesWhenModelNameChanges()
        {
            var a = new ModelIdentity("Sigma", "Structure", "Main Model");
            var b = new ModelIdentity("Sigma", "Structure", "Secondary Model");

            Assert.NotEqual(a.StableKey, b.StableKey);
        }

        [Fact]
        public void StableKey_IsNotAmbiguousWhenAFieldContainsTheDelimiter()
        {
            // Same three raw characters split differently across fields must not collide.
            var a = new ModelIdentity("A|B", "C", "D");
            var b = new ModelIdentity("A", "B|C", "D");

            Assert.NotEqual(a.StableKey, b.StableKey);
        }

        [Fact]
        public void ToString_IsHumanReadable()
        {
            var identity = new ModelIdentity("Sigma", "Structure", "Sigma_Structure");

            Assert.Equal("Sigma / Structure / Sigma_Structure", identity.ToString());
        }

        // --- Identity vs revision separation ---

        [Fact]
        public void Identity_IsSharedAcrossRevisions_AndRevisionNeverEntersStableKey()
        {
            var identity = new ModelIdentity("Sigma", "Structure", "Sigma_Structure");
            var r03 = new ModelRevision(identity, "R03", "Sigma_Structure_R03.nwc", null, null, null);
            var r04 = new ModelRevision(identity, "R04", "Sigma_Structure_R04.nwc", null, null, null);

            Assert.NotEqual(r03.Revision, r04.Revision);
            Assert.Equal(r03.Identity, r04.Identity);
            Assert.Equal(r03.Identity.StableKey, r04.Identity.StableKey);
            Assert.DoesNotContain("R03", r03.Identity.StableKey, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("R04", r04.Identity.StableKey, StringComparison.OrdinalIgnoreCase);
        }

        // --- ModelRevision construction ---

        [Fact]
        public void ModelRevision_Constructor_RejectsNullIdentity()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ModelRevision(null!, "R04", "Model_R04.nwc", null, null, null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ModelRevision_Constructor_RejectsEmptyOrWhitespaceRevision(string revision)
        {
            Assert.Throws<ArgumentException>(
                () => new ModelRevision(MakeIdentity(), revision, "Model_R04.nwc", null, null, null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ModelRevision_Constructor_RejectsEmptyOrWhitespaceSourceFileName(string sourceFileName)
        {
            Assert.Throws<ArgumentException>(
                () => new ModelRevision(MakeIdentity(), "R04", sourceFileName, null, null, null));
        }

        [Fact]
        public void ModelRevision_Constructor_TrimsRequiredStrings()
        {
            var revision = new ModelRevision(MakeIdentity(), "  R04  ", "  Model_R04.nwc  ", null, null, null);

            Assert.Equal("R04", revision.Revision);
            Assert.Equal("Model_R04.nwc", revision.SourceFileName);
        }

        [Fact]
        public void ModelRevision_Constructor_TurnsBlankOptionalStringsIntoNull()
        {
            var revision = new ModelRevision(MakeIdentity(), "R04", "Model_R04.nwc", "   ", "   ", null);

            Assert.Null(revision.SourceFilePath);
            Assert.Null(revision.ContentHash);
        }

        [Fact]
        public void ModelRevision_Constructor_TrimsOptionalStrings()
        {
            var revision = new ModelRevision(
                MakeIdentity(), "R04", "Model_R04.nwc", "  C:\\models\\Model_R04.nwc  ", "  abc123  ", null);

            Assert.Equal("C:\\models\\Model_R04.nwc", revision.SourceFilePath);
            Assert.Equal("abc123", revision.ContentHash);
        }

        // --- ModelRevision equality ---

        [Fact]
        public void ModelRevision_DifferentRevisions_AreNotEqual()
        {
            var identity = MakeIdentity();
            var r03 = new ModelRevision(identity, "R03", "Model_R03.nwc", null, null, null);
            var r04 = new ModelRevision(identity, "R04", "Model_R04.nwc", null, null, null);

            Assert.NotEqual(r03, r04);
        }

        [Fact]
        public void ModelRevision_SameCompleteData_AreEqualWithMatchingHashCode()
        {
            var identity = MakeIdentity();
            var publishedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

            // Revision, SourceFileName, and SourceFilePath differ only by case (ignored);
            // ContentHash is identical (case-sensitive comparison still holds).
            var a = new ModelRevision(identity, "R04", "Model_R04.nwc", "C:\\models\\Model_R04.nwc", "abc123", publishedAt);
            var b = new ModelRevision(identity, "r04", "model_r04.nwc", "c:\\models\\model_r04.nwc", "abc123", publishedAt);

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void ModelRevision_DifferentContentHash_ChangesEquality()
        {
            var identity = MakeIdentity();
            var a = new ModelRevision(identity, "R04", "Model_R04.nwc", null, "abc123", null);
            var b = new ModelRevision(identity, "R04", "Model_R04.nwc", null, "def456", null);

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void ModelRevision_IdenticalContentHash_RemainsEqual()
        {
            var identity = MakeIdentity();
            var a = new ModelRevision(identity, "R04", "Model_R04.nwc", null, "abc123", null);
            var b = new ModelRevision(identity, "R04", "Model_R04.nwc", null, "abc123", null);

            Assert.Equal(a, b);
        }

        [Fact]
        public void ModelRevision_ContentHash_DifferingOnlyByCase_AreNotEqual()
        {
            var identity = MakeIdentity();
            var a = new ModelRevision(identity, "R04", "Model_R04.nwc", null, "abc123", null);
            var b = new ModelRevision(identity, "R04", "Model_R04.nwc", null, "ABC123", null);

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void ModelRevision_NullContentHash_RemainsEqual()
        {
            var identity = MakeIdentity();
            var a = new ModelRevision(identity, "R04", "Model_R04.nwc", null, null, null);
            var b = new ModelRevision(identity, "R04", "Model_R04.nwc", null, null, null);

            Assert.Equal(a, b);
        }

        [Fact]
        public void ModelRevision_DifferentPublishedAt_ChangesEquality()
        {
            var identity = MakeIdentity();
            var a = new ModelRevision(
                identity, "R04", "Model_R04.nwc", null, null, new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero));
            var b = new ModelRevision(
                identity, "R04", "Model_R04.nwc", null, null, new DateTimeOffset(2026, 7, 11, 9, 0, 0, TimeSpan.Zero));

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void ModelRevision_ToString_ShowsIdentityAndRevision()
        {
            var identity = new ModelIdentity("Sigma", "Structure", "Sigma_Structure");
            var revision = new ModelRevision(identity, "R04", "Sigma_Structure_R04.nwc", null, null, null);

            Assert.Equal("Sigma / Structure / Sigma_Structure @ R04", revision.ToString());
        }
    }
}
