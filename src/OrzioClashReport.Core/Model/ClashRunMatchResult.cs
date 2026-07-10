using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, auditable result of matching a previous and a current <see cref="CoordinationRun"/>: every
    /// candidate relationship found, the one-to-one subset selected from them, the candidates that were not
    /// selected, and the occurrence slots from each run that ended up with no selected match. This is matching,
    /// not lifecycle -- an occurrence in <see cref="UnmatchedPrevious"/> or <see cref="UnmatchedCurrent"/> may
    /// still have one or more entries in <see cref="AlternativeCandidates"/>; it simply lost the one-to-one
    /// selection to a competing candidate. <see cref="UnmatchedPrevious"/>/<see cref="UnmatchedCurrent"/> must
    /// never be read as "new" or "resolved".
    /// </summary>
    public sealed class ClashRunMatchResult
    {
        public CoordinationRun PreviousRun { get; }
        public CoordinationRun CurrentRun { get; }

        public IReadOnlyList<ClashRunMatchCandidate> Candidates { get; }
        public IReadOnlyList<ClashRunMatchCandidate> SelectedMatches { get; }
        public IReadOnlyList<ClashRunMatchCandidate> AlternativeCandidates { get; }

        public IReadOnlyList<ClashOccurrence> UnmatchedPrevious { get; }
        public IReadOnlyList<ClashOccurrence> UnmatchedCurrent { get; }

        internal ClashRunMatchResult(
            CoordinationRun previousRun,
            CoordinationRun currentRun,
            IReadOnlyList<ClashRunMatchCandidate> candidates,
            IReadOnlyList<ClashRunMatchCandidate> selectedMatches,
            IReadOnlyList<ClashRunMatchCandidate> alternativeCandidates,
            IReadOnlyList<ClashOccurrence> unmatchedPrevious,
            IReadOnlyList<ClashOccurrence> unmatchedCurrent)
        {
            PreviousRun = previousRun ?? throw new ArgumentNullException(nameof(previousRun));
            CurrentRun = currentRun ?? throw new ArgumentNullException(nameof(currentRun));

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (selectedMatches == null)
            {
                throw new ArgumentNullException(nameof(selectedMatches));
            }

            if (alternativeCandidates == null)
            {
                throw new ArgumentNullException(nameof(alternativeCandidates));
            }

            if (unmatchedPrevious == null)
            {
                throw new ArgumentNullException(nameof(unmatchedPrevious));
            }

            if (unmatchedCurrent == null)
            {
                throw new ArgumentNullException(nameof(unmatchedCurrent));
            }

            var candidatesCopy = CopyNonNull(candidates, nameof(candidates));
            var selectedCopy = CopyNonNull(selectedMatches, nameof(selectedMatches));
            var alternativeCopy = CopyNonNull(alternativeCandidates, nameof(alternativeCandidates));
            var unmatchedPreviousCopy = CopyNonNull(unmatchedPrevious, nameof(unmatchedPrevious));
            var unmatchedCurrentCopy = CopyNonNull(unmatchedCurrent, nameof(unmatchedCurrent));

            ValidateUniqueReferences(candidatesCopy, nameof(candidates), "Candidates cannot contain the same candidate reference more than once.");
            ValidateUniqueReferences(selectedCopy, nameof(selectedMatches), "Selected matches cannot contain the same candidate reference more than once.");
            ValidateUniqueReferences(alternativeCopy, nameof(alternativeCandidates), "Alternative candidates cannot contain the same candidate reference more than once.");

            foreach (var candidate in candidatesCopy)
            {
                ValidateCandidateAgainstRuns(candidate, previousRun, currentRun, nameof(candidates));
            }

            foreach (var selected in selectedCopy)
            {
                if (!ContainsByReference(candidatesCopy, selected))
                {
                    throw new ArgumentException(
                        "Every selected match must exist in Candidates by the same reference.", nameof(selectedMatches));
                }

                if (ContainsByReference(alternativeCopy, selected))
                {
                    throw new ArgumentException(
                        "A candidate cannot be both a selected match and an alternative candidate.", nameof(selectedMatches));
                }
            }

            foreach (var alternative in alternativeCopy)
            {
                if (!ContainsByReference(candidatesCopy, alternative))
                {
                    throw new ArgumentException(
                        "Every alternative candidate must exist in Candidates by the same reference.", nameof(alternativeCandidates));
                }
            }

            foreach (var candidate in candidatesCopy)
            {
                bool isSelected = ContainsByReference(selectedCopy, candidate);
                bool isAlternative = ContainsByReference(alternativeCopy, candidate);

                if (isSelected == isAlternative)
                {
                    throw new ArgumentException(
                        "Every candidate must appear in exactly one of SelectedMatches or AlternativeCandidates.",
                        nameof(candidates));
                }
            }

            var usedPreviousIndexes = new HashSet<int>();
            var usedCurrentIndexes = new HashSet<int>();
            foreach (var selected in selectedCopy)
            {
                if (!usedPreviousIndexes.Add(selected.PreviousIndex))
                {
                    throw new ArgumentException(
                        $"Previous index {selected.PreviousIndex} is selected in more than one match.", nameof(selectedMatches));
                }

                if (!usedCurrentIndexes.Add(selected.CurrentIndex))
                {
                    throw new ArgumentException(
                        $"Current index {selected.CurrentIndex} is selected in more than one match.", nameof(selectedMatches));
                }
            }

            ValidateUnmatchedSequence(
                previousRun.Occurrences,
                usedPreviousIndexes,
                unmatchedPreviousCopy,
                nameof(unmatchedPrevious),
                "previous");

            ValidateUnmatchedSequence(
                currentRun.Occurrences,
                usedCurrentIndexes,
                unmatchedCurrentCopy,
                nameof(unmatchedCurrent),
                "current");

            Candidates = candidatesCopy.AsReadOnly();
            SelectedMatches = selectedCopy.AsReadOnly();
            AlternativeCandidates = alternativeCopy.AsReadOnly();
            UnmatchedPrevious = unmatchedPreviousCopy.AsReadOnly();
            UnmatchedCurrent = unmatchedCurrentCopy.AsReadOnly();
        }

        private static List<T> CopyNonNull<T>(IReadOnlyList<T> source, string paramName) where T : class
        {
            var copy = new List<T>(source);
            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] == null)
                {
                    throw new ArgumentException($"Item at index {i} cannot be null.", paramName);
                }
            }

            return copy;
        }

        private static void ValidateUniqueReferences<T>(IReadOnlyList<T> list, string paramName, string message) where T : class
        {
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    if (ReferenceEquals(list[i], list[j]))
                    {
                        throw new ArgumentException(message, paramName);
                    }
                }
            }
        }

        private static bool ContainsByReference(IReadOnlyList<ClashRunMatchCandidate> list, ClashRunMatchCandidate item)
        {
            foreach (var candidate in list)
            {
                if (ReferenceEquals(candidate, item))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateUnmatchedSequence(
            IReadOnlyList<ClashOccurrence> runOccurrences,
            HashSet<int> selectedIndexes,
            IReadOnlyList<ClashOccurrence> actualUnmatched,
            string paramName,
            string runLabel)
        {
            var expectedUnmatched = new List<ClashOccurrence>();
            for (int index = 0; index < runOccurrences.Count; index++)
            {
                if (!selectedIndexes.Contains(index))
                {
                    expectedUnmatched.Add(runOccurrences[index]);
                }
            }

            if (actualUnmatched.Count != expectedUnmatched.Count)
            {
                throw new ArgumentException(
                    $"Unmatched{Capitalize(runLabel)} must contain exactly the unselected {runLabel} occurrences in slot order.",
                    paramName);
            }

            for (int index = 0; index < expectedUnmatched.Count; index++)
            {
                if (!ReferenceEquals(actualUnmatched[index], expectedUnmatched[index]))
                {
                    throw new ArgumentException(
                        $"Unmatched{Capitalize(runLabel)} must contain exactly the unselected {runLabel} occurrences in slot order.",
                        paramName);
                }
            }
        }

        private static string Capitalize(string value) =>
            string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value.Substring(1);

        private static void ValidateCandidateAgainstRuns(
            ClashRunMatchCandidate candidate, CoordinationRun previousRun, CoordinationRun currentRun, string paramName)
        {
            if (candidate.PreviousIndex >= previousRun.Occurrences.Count)
            {
                throw new ArgumentException(
                    $"Candidate previous index {candidate.PreviousIndex} is out of range for the previous run's "
                    + $"{previousRun.Occurrences.Count} occurrences.",
                    paramName);
            }

            if (candidate.CurrentIndex >= currentRun.Occurrences.Count)
            {
                throw new ArgumentException(
                    $"Candidate current index {candidate.CurrentIndex} is out of range for the current run's "
                    + $"{currentRun.Occurrences.Count} occurrences.",
                    paramName);
            }

            if (!ReferenceEquals(candidate.Assessment.PreviousOccurrence, previousRun.Occurrences[candidate.PreviousIndex]))
            {
                throw new ArgumentException(
                    $"Candidate at previous index {candidate.PreviousIndex} does not reference the exact occurrence "
                    + "at that slot in the previous run.",
                    paramName);
            }

            if (!ReferenceEquals(candidate.Assessment.CurrentOccurrence, currentRun.Occurrences[candidate.CurrentIndex]))
            {
                throw new ArgumentException(
                    $"Candidate at current index {candidate.CurrentIndex} does not reference the exact occurrence "
                    + "at that slot in the current run.",
                    paramName);
            }
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "run-1 -> run-2: 12 candidates, 5 selected, 7 alternatives, 2 unmatched previous, 3 unmatched current". Not a persistence key.</summary>
        public override string ToString() =>
            $"{PreviousRun.RunId} -> {CurrentRun.RunId}: {Candidates.Count} candidates, {SelectedMatches.Count} selected, "
            + $"{AlternativeCandidates.Count} alternatives, {UnmatchedPrevious.Count} unmatched previous, "
            + $"{UnmatchedCurrent.Count} unmatched current";
    }
}
