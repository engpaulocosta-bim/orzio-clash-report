using System;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// One progress notification from a running job: either a state transition, or one captured output
    /// line. Progress is informational only; no domain decision is ever taken by reading engine text.
    /// </summary>
    public sealed class EngineJobProgress
    {
        public EngineJobState State { get; }
        public EngineStreamKind? Stream { get; }
        public string? Line { get; }

        private EngineJobProgress(EngineJobState state, EngineStreamKind? stream, string? line)
        {
            if (!Enum.IsDefined(typeof(EngineJobState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown job state.");
            }

            State = state;
            Stream = stream;
            Line = line;
        }

        public static EngineJobProgress ForState(EngineJobState state) =>
            new EngineJobProgress(state, null, null);

        public static EngineJobProgress ForLine(EngineJobState state, EngineStreamKind stream, string line)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (!Enum.IsDefined(typeof(EngineStreamKind), stream))
            {
                throw new ArgumentOutOfRangeException(nameof(stream), stream, "Unknown stream kind.");
            }

            return new EngineJobProgress(state, stream, line);
        }
    }
}
