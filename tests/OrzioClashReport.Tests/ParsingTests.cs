using System;
using System.IO;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Input.NavisworksXml;

namespace OrzioClashReport.Tests
{
    public class ParsingTests
    {
        private static string SampleFilePath =>
            Path.Combine(AppContext.BaseDirectory, "samples", "sample-clash.xml");

        private static ClashReportDocument ReadSample()
        {
            var log = new RecordingAppLog();
            var source = new NavisworksXmlClashSource(SampleFilePath, log);
            return source.Read();
        }

        [Fact]
        public void Read_SetsSourceNameFromExchangeFilename()
        {
            var document = ReadSample();

            Assert.Equal("Project_A_HVAC_PD_R01_Federated_Model.nwd", document.SourceName);
        }

        [Fact]
        public void Read_ParsesOneBatchWithFiveClashes()
        {
            var document = ReadSample();

            var batch = Assert.Single(document.Batches);
            Assert.Equal("Teste 01", batch.Name);
            Assert.Equal(0.001, batch.Tolerance);
            Assert.Equal(5, batch.Clashes.Count);
        }

        [Fact]
        public void Read_ParsesFirstClashFields()
        {
            var document = ReadSample();
            var clash = document.Batches[0].Clashes[0];

            Assert.Equal("Clash1", clash.Name);
            Assert.Equal("e37d5d4a-2bd1-2ea4-0694-6f2b9d7479ed", clash.Guid);
            Assert.Equal(ClashStatus.New, clash.Status);
            Assert.Equal(-0.007, clash.Distance);

            Assert.NotNull(clash.Point);
            Assert.Equal(2810.456, clash.Point!.Value.X);
            Assert.Equal(1869.841, clash.Point!.Value.Y);
            Assert.Equal(-3.426, clash.Point!.Value.Z);
        }

        [Fact]
        public void Read_ParsesClashObjectIdentityAndLevel()
        {
            var document = ReadSample();
            var clash = document.Batches[0].Clashes[0];

            Assert.Equal("48b67dfa-ecf5-541d-fa9d-88e56fc0aa4e", clash.ElementA.ElementId);
            Assert.Equal("Nivel +0", clash.ElementA.Level);
            Assert.Equal("a06455ac-491b-9ab2-40d9-e2930ea5eb33", clash.ElementB.ElementId);
        }

        [Fact]
        public void Read_LeavesPathHierarchyEmptyWhenPathlinkIsAbsent()
        {
            var document = ReadSample();
            var clash = document.Batches[0].Clashes[0];

            Assert.Empty(clash.ElementA.PathHierarchy);
            Assert.Empty(clash.ElementB.PathHierarchy);
        }

        [Fact]
        public void Read_CarriesRawSmarttagsAsProperties()
        {
            var document = ReadSample();
            var elementA = document.Batches[0].Clashes[0].ElementA;

            Assert.Equal("Project_A_HVAC_PD_R00.rvt", elementA.Properties["Item Source File"]);
        }

        [Fact]
        public void Read_ParsesSourceModelFromItemSourceFile()
        {
            var document = ReadSample();
            var elementA = document.Batches[0].Clashes[0].ElementA;

            Assert.Equal("Project_A_HVAC_PD_R00.rvt", elementA.SourceModel);
            Assert.Equal("Project_A_HVAC_PD_R00.rvt", elementA.Properties["Item Source File"]);
        }
    }
}
