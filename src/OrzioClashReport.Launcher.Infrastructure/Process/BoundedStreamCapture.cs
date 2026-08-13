using System;
using System.Text;

namespace OrzioClashReport.Launcher.Infrastructure.Process
{
    /// <summary>
    /// Captures at most 64 KB of one engine stream: the first 8 KB and the last 56 KB. An engine that
    /// writes megabytes cannot grow the launcher's memory without bound, and the two ends that matter
    /// (what it started doing, and what it was doing when it stopped) are both preserved.
    /// </summary>
    /// <remarks>
    /// The unit is decoded characters, not raw bytes: the launcher only ever shows this text to a
    /// human, so bounding what is displayed is what matters.
    /// </remarks>
    internal sealed class BoundedStreamCapture
    {
        internal const int HeadCapacity = 8 * 1024;
        internal const int TailCapacity = 56 * 1024;

        private readonly StringBuilder _head = new StringBuilder(1024);
        private readonly char[] _tail = new char[TailCapacity];
        private int _tailStart;
        private int _tailCount;

        public bool Truncated { get; private set; }

        public void Append(char[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            for (int i = 0; i < count; i++)
            {
                Append(buffer[offset + i]);
            }
        }

        public void Append(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            for (int i = 0; i < text.Length; i++)
            {
                Append(text[i]);
            }
        }

        private void Append(char value)
        {
            if (_head.Length < HeadCapacity)
            {
                _head.Append(value);
                return;
            }

            if (_tailCount < TailCapacity)
            {
                _tail[(_tailStart + _tailCount) % TailCapacity] = value;
                _tailCount++;
                return;
            }

            // The tail is full: the oldest retained character is dropped, which is exactly the middle
            // of the stream. That loss is reported through Truncated and never hidden.
            _tail[_tailStart] = value;
            _tailStart = (_tailStart + 1) % TailCapacity;
            Truncated = true;
        }

        public string ToText()
        {
            var builder = new StringBuilder(_head.Length + _tailCount);
            builder.Append(_head);

            for (int i = 0; i < _tailCount; i++)
            {
                builder.Append(_tail[(_tailStart + i) % TailCapacity]);
            }

            return builder.ToString();
        }
    }
}
