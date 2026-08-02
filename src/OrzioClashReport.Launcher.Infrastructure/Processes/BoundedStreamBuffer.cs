using System;
using System.Text;

namespace OrzioClashReport.Launcher.Infrastructure.Processes
{
    /// <summary>
    /// Captures at most 64 KB of one engine stream: the first 8 KB and the last 56 KB, with an
    /// explicit marker where the middle was dropped. An engine that prints without bound must never
    /// be able to grow the launcher's memory without bound.
    /// </summary>
    public sealed class BoundedStreamBuffer
    {
        internal const int HeadCapacity = 8 * 1024;
        internal const int TailCapacity = 56 * 1024;

        private const string TruncationMarker = "\n... [output truncated] ...\n";

        private readonly StringBuilder _head = new StringBuilder(1024);
        private readonly char[] _tail = new char[TailCapacity];
        private int _tailStart;
        private int _tailLength;
        private long _droppedCharacters;

        /// <summary>Whether anything was dropped from the middle of the stream.</summary>
        public bool Truncated => _droppedCharacters > 0;

        /// <summary>Total characters observed, including the ones that were dropped.</summary>
        public long TotalCharacters { get; private set; }

        public void Append(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            for (int i = 0; i < text!.Length; i++)
            {
                Append(text[i]);
            }
        }

        /// <summary>Appends one line plus the newline the reader stripped.</summary>
        public void AppendLine(string? line)
        {
            Append(line);
            Append('\n');
        }

        private void Append(char value)
        {
            TotalCharacters++;

            if (_head.Length < HeadCapacity)
            {
                _head.Append(value);
                return;
            }

            if (_tailLength < TailCapacity)
            {
                _tail[(_tailStart + _tailLength) % TailCapacity] = value;
                _tailLength++;
                return;
            }

            // The tail is full: overwrite its oldest character and count the loss.
            _tail[_tailStart] = value;
            _tailStart = (_tailStart + 1) % TailCapacity;
            _droppedCharacters++;
        }

        public override string ToString()
        {
            if (_tailLength == 0)
            {
                return _head.ToString();
            }

            var builder = new StringBuilder(_head.Length + _tailLength + TruncationMarker.Length);
            builder.Append(_head);

            if (Truncated)
            {
                builder.Append(TruncationMarker);
            }

            for (int i = 0; i < _tailLength; i++)
            {
                builder.Append(_tail[(_tailStart + i) % TailCapacity]);
            }

            return builder.ToString();
        }
    }
}
