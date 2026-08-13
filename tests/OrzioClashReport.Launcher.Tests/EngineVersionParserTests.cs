using OrzioClashReport.Launcher.Application.Engine;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class EngineVersionParserTests
    {
        [Theory]
        [InlineData("orzioclash 0.1.0-preview.3", "0.1.0-preview.3")]
        [InlineData("orzioclash 0.1.0-preview.3\n", "0.1.0-preview.3")]
        [InlineData("orzioclash 0.1.0-preview.3\r\n", "0.1.0-preview.3")]
        [InlineData("\norzioclash 1.2.3-rc.10\n", "1.2.3-rc.10")]
        public void ParsesThePublishedVersionLine(string output, string expected)
        {
            Assert.True(EngineVersionParser.TryParse(output, out string version));
            Assert.Equal(expected, version);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("orzioclash")]
        [InlineData("orzioclash 0.1.0")]
        [InlineData("orzioclash  0.1.0-preview.3")]
        [InlineData("OrzioClash 0.1.0-preview.3")]
        [InlineData("orzioclash 0.1.0-preview.3 extra")]
        [InlineData("prefix orzioclash 0.1.0-preview.3")]
        [InlineData("orzioclash 0.1.0-preview_3")]
        public void RejectsAnythingOtherThanThePublishedContract(string? output)
        {
            Assert.False(EngineVersionParser.TryParse(output, out string version));
            Assert.Equal(string.Empty, version);
        }

        [Fact]
        public void RejectsMoreThanOneNonEmptyLine()
        {
            Assert.False(EngineVersionParser.TryParse(
                "orzioclash 0.1.0-preview.3\nsomething else\n", out _));
        }

        [Fact]
        public void VersionComparisonIsOrdinalSoPreviewSuffixesAreNotRoundedOff()
        {
            Assert.True(EngineVersionParser.Matches("0.1.0-preview.3", "0.1.0-preview.3"));
            Assert.False(EngineVersionParser.Matches("0.1.0-preview.3", "0.1.0-preview.4"));
            Assert.False(EngineVersionParser.Matches("0.1.0-PREVIEW.3", "0.1.0-preview.3"));
        }
    }
}
