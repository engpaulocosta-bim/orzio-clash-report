using OrzioClashReport.Launcher.Infrastructure.Processes;

namespace OrzioClashReport.Launcher.Tests;

public sealed class BoundedStreamBufferTests
{
    private const int Head = BoundedStreamBuffer.HeadCapacity;
    private const int Tail = BoundedStreamBuffer.TailCapacity;

    [Fact]
    public void SmallOutput_IsKeptExactly()
    {
        var buffer = new BoundedStreamBuffer();

        buffer.AppendLine("12 raw clashes -> 4 groups");
        buffer.AppendLine("Report written to report.html");

        Assert.False(buffer.Truncated);
        Assert.Equal("12 raw clashes -> 4 groups\nReport written to report.html\n", buffer.ToString());
    }

    [Fact]
    public void OutputUpToSixtyFourKilobytes_IsKeptWhole()
    {
        var buffer = new BoundedStreamBuffer();
        string content = new string('x', Head + Tail);

        buffer.Append(content);

        Assert.False(buffer.Truncated);
        Assert.Equal(content, buffer.ToString());
    }

    [Fact]
    public void HugeOutput_KeepsTheFirstEightAndLastFiftySixKilobytes()
    {
        var buffer = new BoundedStreamBuffer();
        int total = (Head + Tail) * 4;

        for (int i = 0; i < total; i++)
        {
            buffer.Append(((char)('a' + (i % 26))).ToString());
        }

        Assert.True(buffer.Truncated);
        Assert.Equal(total, buffer.TotalCharacters);

        string captured = buffer.ToString();
        string expectedHead = BuildRange(0, Head);
        string expectedTail = BuildRange(total - Tail, Tail);

        Assert.StartsWith(expectedHead, captured, StringComparison.Ordinal);
        Assert.EndsWith(expectedTail, captured, StringComparison.Ordinal);
        Assert.Contains("[output truncated]", captured, StringComparison.Ordinal);
    }

    [Fact]
    public void CapturedTextNeverGrowsWithoutBound()
    {
        var buffer = new BoundedStreamBuffer();
        const int marker = 64 * 1024;

        for (int i = 0; i < 40; i++)
        {
            buffer.Append(new string('y', marker));
        }

        // Head plus tail plus the truncation marker, and nothing more, however loud the engine is.
        Assert.True(buffer.ToString().Length <= Head + Tail + 64);
        Assert.Equal(40L * marker, buffer.TotalCharacters);
    }

    [Fact]
    public void NullAndEmptyAppends_AreIgnored()
    {
        var buffer = new BoundedStreamBuffer();

        buffer.Append(null);
        buffer.Append(string.Empty);

        Assert.Equal(0, buffer.TotalCharacters);
        Assert.Equal(string.Empty, buffer.ToString());
    }

    private static string BuildRange(int start, int length)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = (char)('a' + ((start + i) % 26));
        }

        return new string(chars);
    }
}
