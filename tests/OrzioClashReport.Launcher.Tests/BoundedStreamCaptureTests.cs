using System;
using System.Collections.Generic;
using OrzioClashReport.Launcher.Infrastructure.Process;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class BoundedStreamCaptureTests
    {
        private const int Head = BoundedStreamCapture.HeadCapacity;
        private const int Tail = BoundedStreamCapture.TailCapacity;

        [Fact]
        public void TheBoundsAre8KbOfHeadAnd56KbOfTail()
        {
            Assert.Equal(8 * 1024, Head);
            Assert.Equal(56 * 1024, Tail);
            Assert.Equal(64 * 1024, Head + Tail);
        }

        [Fact]
        public void SmallOutputIsKeptExactlyAndIsNotMarkedTruncated()
        {
            var capture = new BoundedStreamCapture();
            capture.Append("Report written to report.html\n");

            Assert.Equal("Report written to report.html\n", capture.ToText());
            Assert.False(capture.Truncated);
        }

        [Fact]
        public void OutputExactlyAtTheLimitIsKeptWhole()
        {
            var capture = new BoundedStreamCapture();
            capture.Append(new string('a', Head + Tail));

            Assert.Equal(Head + Tail, capture.ToText().Length);
            Assert.False(capture.Truncated);
        }

        [Fact]
        public void HugeOutputKeepsTheFirst8KbAndTheLast56Kb()
        {
            var capture = new BoundedStreamCapture();

            // A recognisable head and tail with a large, discardable middle.
            capture.Append(new string('H', Head));
            capture.Append(new string('M', 5 * 1024 * 1024));
            capture.Append(new string('T', Tail));

            string text = capture.ToText();

            Assert.True(capture.Truncated);
            Assert.Equal(Head + Tail, text.Length);
            Assert.Equal(new string('H', Head), text.Substring(0, Head));
            Assert.Equal(new string('T', Tail), text.Substring(Head));
        }

        [Fact]
        public void MemoryStaysBoundedNoMatterHowMuchIsWritten()
        {
            var capture = new BoundedStreamCapture();

            for (int i = 0; i < 200; i++)
            {
                capture.Append(new string('x', 64 * 1024));
            }

            Assert.Equal(Head + Tail, capture.ToText().Length);
            Assert.True(capture.Truncated);
        }

        [Fact]
        public void TheLineSplitterNormalisesLineEndingsAndDropsTheTerminator()
        {
            var lines = new List<string>();
            var splitter = new LineSplitter(lines.Add);

            Append(splitter, "first\r\nsecond\nthird");
            splitter.Complete();

            Assert.Equal(new[] { "first", "second", "third" }, lines);
        }

        [Fact]
        public void TheLineSplitterFlushesAnUnterminatedLineInsteadOfBufferingItForever()
        {
            var lines = new List<string>();
            var splitter = new LineSplitter(lines.Add);

            Append(splitter, new string('x', 100 * 1024));
            splitter.Complete();

            Assert.True(lines.Count > 1, "A very long unterminated line must be flushed in pieces.");

            foreach (string line in lines)
            {
                Assert.True(line.Length <= 4 * 1024, "No progress line may grow without bound.");
            }
        }

        private static void Append(LineSplitter splitter, string text)
        {
            char[] buffer = text.ToCharArray();
            splitter.Append(buffer, 0, buffer.Length);
        }
    }
}
