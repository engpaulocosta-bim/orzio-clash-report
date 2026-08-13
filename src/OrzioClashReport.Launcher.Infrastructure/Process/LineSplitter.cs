using System;
using System.Text;

namespace OrzioClashReport.Launcher.Infrastructure.Process
{
    /// <summary>
    /// Turns a character stream into progress lines without ever holding an unbounded pending line. An
    /// engine that writes a megabyte with no newline is flushed in bounded pieces instead of buffering.
    /// </summary>
    internal sealed class LineSplitter
    {
        private const int MaximumPendingLength = 4 * 1024;

        private readonly Action<string> _onLine;
        private readonly StringBuilder _pending = new StringBuilder(256);

        public LineSplitter(Action<string> onLine)
        {
            _onLine = onLine ?? throw new ArgumentNullException(nameof(onLine));
        }

        public void Append(char[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                char value = buffer[offset + i];

                if (value == '\n')
                {
                    Flush();
                    continue;
                }

                if (value == '\r')
                {
                    continue;
                }

                _pending.Append(value);

                if (_pending.Length >= MaximumPendingLength)
                {
                    Flush();
                }
            }
        }

        public void Complete()
        {
            if (_pending.Length > 0)
            {
                Flush();
            }
        }

        private void Flush()
        {
            string line = _pending.ToString();
            _pending.Clear();
            _onLine(line);
        }
    }
}
