using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// A <see cref="ClashMatchAssessment"/> tied to the exact slot indices of the two occurrences it compares
    /// within a previous and a current <see cref="CoordinationRun"/>. Indices are necessary because a
    /// <see cref="CoordinationRun"/> permits duplicate occurrences, even the same reference at more than one
    /// position, so a candidate must identify *which* slot it came from, not just *which* occurrence.
    /// </summary>
    public sealed class ClashRunMatchCandidate
    {
        public int PreviousIndex { get; }
        public int CurrentIndex { get; }
        public ClashMatchAssessment Assessment { get; }

        internal ClashRunMatchCandidate(int previousIndex, int currentIndex, ClashMatchAssessment assessment)
        {
            if (previousIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(previousIndex), previousIndex, "Previous index cannot be negative.");
            }

            if (currentIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentIndex), currentIndex, "Current index cannot be negative.");
            }

            PreviousIndex = previousIndex;
            CurrentIndex = currentIndex;
            Assessment = assessment ?? throw new ArgumentNullException(nameof(assessment));
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "previous[2] x current[5]: High". Not a persistence key.</summary>
        public override string ToString() => $"previous[{PreviousIndex}] x current[{CurrentIndex}]: {Assessment.Confidence}";
    }
}
