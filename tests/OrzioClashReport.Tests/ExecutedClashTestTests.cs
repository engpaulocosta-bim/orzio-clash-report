using System;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Unit tests for ExecutedClashTest: the Core's immutable declaration that a clash test was executed for a revision-free model pair.</summary>
    public class ExecutedClashTestTests
    {
        private static ModelIdentity MakeIdentity(string company, string discipline, string modelName) =>
            new ModelIdentity(company, discipline, modelName);

        private static readonly ModelIdentity Structure = MakeIdentity("Sigma", "Structure", "Sigma_Structure");
        private static readonly ModelIdentity Hvac = MakeIdentity("Alfa", "HVAC", "Alfa_HVAC");

        [Fact]
        public void Constructor_RejectsNullName()
        {
            Assert.Throws<ArgumentNullException>(() => new ExecutedClashTest(null!, Structure, Hvac));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RejectsEmptyOrWhitespaceName(string name)
        {
            Assert.Throws<ArgumentException>(() => new ExecutedClashTest(name, Structure, Hvac));
        }

        [Fact]
        public void Constructor_TrimsName()
        {
            var test = new ExecutedClashTest("  HVAC vs Structure  ", Structure, Hvac);

            Assert.Equal("HVAC vs Structure", test.Name);
        }

        [Fact]
        public void Constructor_RejectsNullModelA()
        {
            Assert.Throws<ArgumentNullException>(() => new ExecutedClashTest("Test 1", null!, Hvac));
        }

        [Fact]
        public void Constructor_RejectsNullModelB()
        {
            Assert.Throws<ArgumentNullException>(() => new ExecutedClashTest("Test 1", Structure, null!));
        }

        [Fact]
        public void Constructor_PreservesModelAAndModelBReferences()
        {
            var test = new ExecutedClashTest("Test 1", Structure, Hvac);

            Assert.Same(Structure, test.ModelA);
            Assert.Same(Hvac, test.ModelB);
        }

        [Fact]
        public void Constructor_PreservesDeclaredAbOrder_DoesNotNormalizeOrSwap()
        {
            var direct = new ExecutedClashTest("Test 1", Structure, Hvac);
            var swapped = new ExecutedClashTest("Test 1", Hvac, Structure);

            Assert.Same(Structure, direct.ModelA);
            Assert.Same(Hvac, direct.ModelB);
            Assert.Same(Hvac, swapped.ModelA);
            Assert.Same(Structure, swapped.ModelB);
        }

        [Fact]
        public void Constructor_AcceptsSelfClash()
        {
            var test = new ExecutedClashTest("Self Clash", Structure, Structure);

            Assert.Same(Structure, test.ModelA);
            Assert.Same(Structure, test.ModelB);
        }

        [Fact]
        public void ToString_IncludesNameAndBothModels()
        {
            var test = new ExecutedClashTest("HVAC vs Structure", Structure, Hvac);

            string text = test.ToString();

            Assert.Contains("HVAC vs Structure", text);
            Assert.Contains(Structure.ToString(), text);
            Assert.Contains(Hvac.ToString(), text);
        }

        [Fact]
        public void Type_DoesNotExposeRevision()
        {
            var properties = typeof(ExecutedClashTest).GetProperties();

            Assert.DoesNotContain(properties, p => p.Name.Contains("Revision", StringComparison.OrdinalIgnoreCase));
        }
    }
}
