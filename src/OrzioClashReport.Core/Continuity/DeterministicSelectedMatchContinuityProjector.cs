using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Continuity
{
    /// <summary>
    /// A deterministic <see cref="IClashRunSequenceContinuityProjector"/>: for each boundary between two
    /// consecutive pairwise comparisons in a <see cref="ClashRunSequenceComparisonResult"/>, walks the shared
    /// run's occurrence slots and creates a <see cref="SelectedMatchContinuityLink"/> wherever a selected match
    /// enters a slot and a selected match leaves the exact same slot. Considers only
    /// <see cref="ClashRunMatchResult.SelectedMatches"/> -- never <see cref="ClashRunMatchResult.Candidates"/>,
    /// <see cref="ClashRunMatchResult.AlternativeCandidates"/>, or lifecycle status -- and never compares a run
    /// against anything other than its immediately adjacent boundary partner.
    /// </summary>
    public sealed class DeterministicSelectedMatchContinuityProjector : IClashRunSequenceContinuityProjector
    {
        public ClashRunSequenceContinuityResult Project(ClashRunSequenceComparisonResult sequenceComparison)
        {
            if (sequenceComparison == null)
            {
                throw new ArgumentNullException(nameof(sequenceComparison));
            }

            var links = new List<SelectedMatchContinuityLink>();

            for (int boundary = 0; boundary < sequenceComparison.Comparisons.Count - 1; boundary++)
            {
                var incomingSelectedMatches = sequenceComparison.Comparisons[boundary].MatchResult.SelectedMatches;
                var outgoingSelectedMatches = sequenceComparison.Comparisons[boundary + 1].MatchResult.SelectedMatches;
                var sharedRun = sequenceComparison.Runs[boundary + 1];

                var incomingBySlot = new Dictionary<int, ClashRunMatchCandidate>();
                foreach (var candidate in incomingSelectedMatches)
                {
                    incomingBySlot[candidate.CurrentIndex] = candidate;
                }

                var outgoingBySlot = new Dictionary<int, ClashRunMatchCandidate>();
                foreach (var candidate in outgoingSelectedMatches)
                {
                    outgoingBySlot[candidate.PreviousIndex] = candidate;
                }

                for (int slot = 0; slot < sharedRun.Occurrences.Count; slot++)
                {
                    bool hasIncoming = incomingBySlot.TryGetValue(slot, out var incomingSelected);
                    bool hasOutgoing = outgoingBySlot.TryGetValue(slot, out var outgoingSelected);

                    if (hasIncoming && hasOutgoing)
                    {
                        links.Add(new SelectedMatchContinuityLink(
                            boundary,
                            slot,
                            sharedRun,
                            sharedRun.Occurrences[slot],
                            incomingSelected!,
                            outgoingSelected!));
                    }
                }
            }

            return new ClashRunSequenceContinuityResult(sequenceComparison, links);
        }
    }
}
