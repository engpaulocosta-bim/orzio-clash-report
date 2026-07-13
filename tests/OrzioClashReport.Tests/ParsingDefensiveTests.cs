using System;
using System.IO;
using System.Linq;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Input.NavisworksXml;

namespace OrzioClashReport.Tests
{
    /// <summary>Proves the XML parser fails loudly on malformed input and applies the documented defensive-parsing rules for status and clash-object counts.</summary>
    public class ParsingDefensiveTests : IDisposable
    {
        private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), $"orzioclash-test-{Guid.NewGuid():N}.xml");

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        private ClashReportDocument ReadXml(string content, out RecordingAppLog log)
        {
            File.WriteAllText(_tempFilePath, content);
            log = new RecordingAppLog();
            var source = new NavisworksXmlClashSource(_tempFilePath, log);
            return source.Read();
        }

        private static string ClashObject(string guid) => $@"
              <clashobject>
                <objectattribute>
                  <name>GUID</name>
                  <value>{guid}</value>
                </objectattribute>
                <layer>L1</layer>
              </clashobject>";

        private const string ClashObjectWithoutGuid = @"
              <clashobject>
                <layer>L1</layer>
              </clashobject>";

        /// <summary>Builds a clashobject with optional "Item Source File"/"Item Source File Name" smarttags; a null argument omits that smarttag entirely, distinct from an empty/whitespace value.</summary>
        private static string ClashObjectWithSourceSmartTags(string guid, string? itemSourceFile, string? itemSourceFileName)
        {
            string itemSourceFileTag = itemSourceFile == null ? string.Empty : $@"
                  <smarttag>
                    <name>Item Source File</name>
                    <value>{itemSourceFile}</value>
                  </smarttag>";

            string itemSourceFileNameTag = itemSourceFileName == null ? string.Empty : $@"
                  <smarttag>
                    <name>Item Source File Name</name>
                    <value>{itemSourceFileName}</value>
                  </smarttag>";

            return $@"
              <clashobject>
                <objectattribute>
                  <name>GUID</name>
                  <value>{guid}</value>
                </objectattribute>
                <layer>L1</layer>
                <smarttags>{itemSourceFileTag}{itemSourceFileNameTag}
                </smarttags>
              </clashobject>";
        }

        private string BuildExchange(string clashResultBody, string clashResultAttributes = "guid=\"g1\"") => $@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<exchange filename=""test.nwd"">
  <batchtest name=""Report"">
    <clashtests>
      <clashtest name=""Test"" tolerance=""0.001"">
        <clashresults>
          <clashresult name=""Clash1"" {clashResultAttributes}>
            {clashResultBody}
          </clashresult>
        </clashresults>
      </clashtest>
    </clashtests>
  </batchtest>
</exchange>";

        [Fact]
        public void Read_MalformedXml_FailsWithClearMessage()
        {
            File.WriteAllText(_tempFilePath, "<exchange><unclosed></exchange>");
            var source = new NavisworksXmlClashSource(_tempFilePath, new RecordingAppLog());

            var ex = Assert.Throws<InvalidOperationException>(() => source.Read());
            Assert.Contains(_tempFilePath, ex.Message);
        }

        [Fact]
        public void Read_RootIsNotExchange_FailsWithClearMessage()
        {
            File.WriteAllText(_tempFilePath, "<?xml version=\"1.0\"?><notexchange></notexchange>");
            var source = new NavisworksXmlClashSource(_tempFilePath, new RecordingAppLog());

            var ex = Assert.Throws<InvalidOperationException>(() => source.Read());
            Assert.Contains("exchange", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_ClashWithFewerThanTwoClashObjects_FailsClearly()
        {
            string xml = BuildExchange($@"
            <resultstatus>New</resultstatus>
            <clashobjects>
              {ClashObject("guid-a")}
            </clashobjects>");
            File.WriteAllText(_tempFilePath, xml);
            var source = new NavisworksXmlClashSource(_tempFilePath, new RecordingAppLog());

            var ex = Assert.Throws<InvalidOperationException>(() => source.Read());
            Assert.Contains("Clash1", ex.Message);
        }

        [Fact]
        public void Read_ClashWithMoreThanTwoClashObjects_UsesFirstTwoAndWarns()
        {
            string xml = BuildExchange($@"
            <resultstatus>New</resultstatus>
            <clashobjects>
              {ClashObject("guid-a")}
              {ClashObject("guid-b")}
              {ClashObject("guid-c")}
            </clashobjects>");
            var document = ReadXml(xml, out var log);

            var clash = document.Batches[0].Clashes[0];
            Assert.Equal("guid-a", clash.ElementA.ElementId);
            Assert.Equal("guid-b", clash.ElementB.ElementId);
            Assert.Contains(log.Warnings, w => w.Contains("3 clash objects"));
        }

        [Fact]
        public void Read_UnknownStatus_MapsToUnknownAndWarns()
        {
            string xml = BuildExchange($@"
            <resultstatus>Bogus</resultstatus>
            <clashobjects>
              {ClashObject("guid-a")}
              {ClashObject("guid-b")}
            </clashobjects>");
            var document = ReadXml(xml, out var log);

            var clash = document.Batches[0].Clashes[0];
            Assert.Equal(ClashStatus.Unknown, clash.Status);
            Assert.Contains(log.Warnings, w => w.Contains("Bogus"));
        }

        [Fact]
        public void Read_MissingStatus_MapsToUnknownWithoutCrash()
        {
            string xml = BuildExchange($@"
            <clashobjects>
              {ClashObject("guid-a")}
              {ClashObject("guid-b")}
            </clashobjects>");
            var document = ReadXml(xml, out var log);

            var clash = document.Batches[0].Clashes[0];
            Assert.Equal(ClashStatus.Unknown, clash.Status);
        }

        [Fact]
        public void Read_ClashObjectWithoutGuid_FailsClearly()
        {
            string xml = BuildExchange($@"
            <resultstatus>New</resultstatus>
            <clashobjects>
              {ClashObjectWithoutGuid}
              {ClashObject("guid-b")}
            </clashobjects>");
            File.WriteAllText(_tempFilePath, xml);
            var source = new NavisworksXmlClashSource(_tempFilePath, new RecordingAppLog());

            var ex = Assert.Throws<InvalidOperationException>(() => source.Read());
            Assert.Contains("GUID", ex.Message);
        }

        // --- SourceModel precedence: "Item Source File" wins, then "Item Source File Name", then null ---

        [Fact]
        public void Read_SourceModel_ItemSourceFileWinsWhenBothPresent()
        {
            string xml = BuildExchange($@"
            <resultstatus>New</resultstatus>
            <clashobjects>
              {ClashObjectWithSourceSmartTags("guid-a", itemSourceFile: "Model_A.rvt", itemSourceFileName: @"C:\Models\Model_A.rvt")}
              {ClashObject("guid-b")}
            </clashobjects>");
            var document = ReadXml(xml, out _);

            var clash = document.Batches[0].Clashes[0];
            Assert.Equal("Model_A.rvt", clash.ElementA.SourceModel);
        }

        [Fact]
        public void Read_SourceModel_FallsBackToItemSourceFileNameWhenItemSourceFileAbsent()
        {
            string xml = BuildExchange($@"
            <resultstatus>New</resultstatus>
            <clashobjects>
              {ClashObjectWithSourceSmartTags("guid-a", itemSourceFile: null, itemSourceFileName: @"C:\Models\Model_A.rvt")}
              {ClashObject("guid-b")}
            </clashobjects>");
            var document = ReadXml(xml, out _);

            var clash = document.Batches[0].Clashes[0];
            Assert.Equal(@"C:\Models\Model_A.rvt", clash.ElementA.SourceModel);
        }

        [Fact]
        public void Read_SourceModel_FallsBackToItemSourceFileNameWhenItemSourceFileIsWhitespace()
        {
            string xml = BuildExchange($@"
            <resultstatus>New</resultstatus>
            <clashobjects>
              {ClashObjectWithSourceSmartTags("guid-a", itemSourceFile: "   ", itemSourceFileName: @"C:\Models\Model_A.rvt")}
              {ClashObject("guid-b")}
            </clashobjects>");
            var document = ReadXml(xml, out _);

            var clash = document.Batches[0].Clashes[0];
            Assert.Equal(@"C:\Models\Model_A.rvt", clash.ElementA.SourceModel);
        }

        [Fact]
        public void Read_SourceModel_IsNullWhenBothSmartTagsAbsent()
        {
            string xml = BuildExchange($@"
            <resultstatus>New</resultstatus>
            <clashobjects>
              {ClashObjectWithSourceSmartTags("guid-a", itemSourceFile: null, itemSourceFileName: null)}
              {ClashObject("guid-b")}
            </clashobjects>");
            var document = ReadXml(xml, out _);

            var clash = document.Batches[0].Clashes[0];
            Assert.Null(clash.ElementA.SourceModel);
        }

        [Fact]
        public void Read_SourceModel_IsNullWhenBothSmartTagsAreWhitespace()
        {
            string xml = BuildExchange($@"
            <resultstatus>New</resultstatus>
            <clashobjects>
              {ClashObjectWithSourceSmartTags("guid-a", itemSourceFile: "   ", itemSourceFileName: "   ")}
              {ClashObject("guid-b")}
            </clashobjects>");
            var document = ReadXml(xml, out _);

            var clash = document.Batches[0].Clashes[0];
            Assert.Null(clash.ElementA.SourceModel);
        }
    }
}
