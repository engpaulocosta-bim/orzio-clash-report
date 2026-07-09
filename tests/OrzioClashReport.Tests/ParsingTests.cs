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

            Assert.Equal("CRISAL_001.26_EXE_ESP_AVAC_PD_R01 _MODELO FEDERADO.nwd", document.SourceName);
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
            Assert.Equal("25a478d0-9ee6-4faa-a947-dc19d04cfb1b", clash.Guid);
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

            Assert.Equal("e8e7574e-19ad-4796-8341-64386d639236", clash.ElementA.ElementId);
            Assert.Equal("Nivel +0", clash.ElementA.Level);
            Assert.Equal("e8e7574e-19ad-4796-8341-64386d63908b", clash.ElementB.ElementId);
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

            Assert.Equal("CRISAL_001.26_EXE_ESP_AVAC_PD_R00.rvt", elementA.Properties["Item Source File"]);
        }
    }
}
