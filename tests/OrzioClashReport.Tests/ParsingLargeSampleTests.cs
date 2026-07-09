using System;
using System.IO;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Input.NavisworksXml;

namespace OrzioClashReport.Tests
{
    /// <summary>Parses the larger, multi-discipline fixture to prove the parser scales and captures pathlink data.</summary>
    public class ParsingLargeSampleTests
    {
        private static string SampleFilePath =>
            Path.Combine(AppContext.BaseDirectory, "samples", "sample-clash2.xml");

        private static ClashReportDocument ReadSample()
        {
            var log = new RecordingAppLog();
            var source = new NavisworksXmlClashSource(SampleFilePath, log);
            return source.Read();
        }

        [Fact]
        public void Read_ParsesAllClashesFromLargeExport()
        {
            var document = ReadSample();

            var batch = Assert.Single(document.Batches);
            Assert.Equal("Test 1", batch.Name);
            Assert.Equal(1458, batch.Clashes.Count);
        }

        [Fact]
        public void Read_ParsesPathHierarchyWhenPathlinkIsPresent()
        {
            var document = ReadSample();
            var clash = document.Batches[0].Clashes[0];

            Assert.Equal(
                new[]
                {
                    "File",
                    "CRISAL_001.26_EXE_ESP_AVAC_PD_R01 _MODELO FEDERADO.nwd",
                    "CRISAL_001.26_EXE_ESP_AVAC_PD_R01_ARCHITETURE.nwd",
                    "Nivel +0",
                    "Walls",
                    "Basic Wall",
                    "Generic - 150mm",
                    "Basic Wall",
                    "12      BETÃO"
                },
                clash.ElementA.PathHierarchy);

            Assert.Equal(
                new[]
                {
                    "File",
                    "CRISAL_001.26_EXE_ESP_AVAC_PD_R01 _MODELO FEDERADO.nwd",
                    "CRISAL_001.26_EXE_ESP_AVAC_PD_R01_CEILLING.nwd",
                    "Nivel +0",
                    "Ceilings",
                    "Compound Ceiling",
                    "600 x 600mm Grid",
                    "Compound Ceiling",
                    "Ceiling Tile 600 x 600"
                },
                clash.ElementB.PathHierarchy);
        }

        [Fact]
        public void Read_ContainsClashesAcrossTwoDistinctSourceModels()
        {
            var document = ReadSample();
            var clash = document.Batches[0].Clashes[0];

            string? sourceA = clash.ElementA.Properties.TryGetValue("Item Source File Name", out var a) ? a : null;
            string? sourceB = clash.ElementB.Properties.TryGetValue("Item Source File Name", out var b) ? b : null;

            Assert.NotNull(sourceA);
            Assert.NotNull(sourceB);
            Assert.NotEqual(sourceA, sourceB);
        }
    }
}
