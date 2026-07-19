using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, auditable result of assembling an exact <see cref="ClashRunSequenceContinuityResult"/> into
    /// disjoint maximal continuity paths: the continuity result itself, plus the complete, canonically ordered
    /// set of <see cref="SelectedMatchContinuityPath"/>s implied by its links' exact selected-match reference
    /// connectivity. This type validates only that <see cref="Paths"/> is exactly, and in canonical order, the
    /// deterministic complete maximal partition of <see cref="ContinuityResult"/>'s links; it never recomputes
    /// matching, lifecycle, sequence comparison, or continuity projection, and it creates no history, no
    /// multi-run lifecycle, and no persistent or stable clash identity.
    /// </summary>
    public sealed class ClashRunSequenceContinuityPathsResult
    {
        public ClashRunSequenceContinuityResult ContinuityResult { get; }
        public IReadOnlyList<SelectedMatchContinuityPath> Paths { get; }

        internal ClashRunSequenceContinuityPathsResult(
            ClashRunSequenceContinuityResult continuityResult,
            IReadOnlyList<SelectedMatchContinuityPath> paths)
        {
            ContinuityResult = continuityResult ?? throw new ArgumentNullException(nameof(continuityResult));

            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            var pathsCopy = new List<SelectedMatchContinuityPath>(paths);
            for (int i = 0; i < pathsCopy.Count; i++)
            {
                if (pathsCopy[i] == null)
                {
                    throw new ArgumentException($"Path at index {i} cannot be null.", nameof(paths));
                }
            }

            ValidateCompleteMaximalPartition(pathsCopy, continuityResult);

            Paths = pathsCopy.AsReadOnly();
        }

        /// <summary>
        /// Recomputes, from <see cref="ContinuityResult"/>.Links alone, the complete set of maximal paths implied
        /// by exact predecessor/successor connectivity, and requires <paramref name="paths"/> to match it
        /// exactly: same path count, same canonical order (by first-link position in <c>Links</c>), same link
        /// count per path, and the same exact link references in every position. This single structural
        /// comparison is what rejects a missing path, an extra path, a duplicate path, a wrong path order, a
        /// foreign or equivalent-but-distinct link, a missing or extra link inside a path, duplicate link
        /// coverage, a split of one maximal path, a merge of disconnected paths, and a non-maximal path, all at
        /// once. This is structural validation, never rematching or re-assembly through the assembler.
        /// </summary>
        private static void ValidateCompleteMaximalPartition(
            IReadOnlyList<SelectedMatchContinuityPath> paths, ClashRunSequenceContinuityResult continuityResult)
        {
            var links = continuityResult.Links;

            var predecessorOf = new Dictionary<SelectedMatchContinuityLink, SelectedMatchContinuityLink?>();
            var successorOf = new Dictionary<SelectedMatchContinuityLink, SelectedMatchContinuityLink?>();

            foreach (var link in links)
            {
                predecessorOf[link] = FindPredecessor(link, links);
                successorOf[link] = FindSuccessor(link, links);
            }

            var expectedPaths = new List<List<SelectedMatchContinuityLink>>();
            foreach (var link in links)
            {
                if (predecessorOf[link] != null)
                {
                    continue;
                }

                var pathLinks = new List<SelectedMatchContinuityLink> { link };
                var current = link;
                while (successorOf[current] is SelectedMatchContinuityLink successor)
                {
                    pathLinks.Add(successor);
                    current = successor;
                }

                expectedPaths.Add(pathLinks);
            }

            if (paths.Count != expectedPaths.Count)
            {
                throw new ArgumentException(
                    $"Paths must contain exactly {expectedPaths.Count} path(s), the complete maximal partition of "
                    + $"ContinuityResult.Links, but found {paths.Count}.",
                    nameof(paths));
            }

            for (int i = 0; i < expectedPaths.Count; i++)
            {
                var expectedLinks = expectedPaths[i];
                var actualLinks = paths[i].Links;

                if (actualLinks.Count != expectedLinks.Count)
                {
                    throw new ArgumentException(
                        $"Path at index {i} must contain exactly {expectedLinks.Count} link(s) (a maximal path), "
                        + $"but found {actualLinks.Count}.",
                        nameof(paths));
                }

                for (int j = 0; j < expectedLinks.Count; j++)
                {
                    if (!ReferenceEquals(actualLinks[j], expectedLinks[j]))
                    {
                        throw new ArgumentException(
                            $"Path at index {i}, link at index {j} must be the exact canonical link reference from "
                            + "ContinuityResult.Links.",
                            nameof(paths));
                    }
                }
            }
        }

        private static SelectedMatchContinuityLink? FindPredecessor(
            SelectedMatchContinuityLink link, IReadOnlyList<SelectedMatchContinuityLink> links)
        {
            foreach (var candidate in links)
            {
                if (ReferenceEquals(candidate, link))
                {
                    continue;
                }

                if (candidate.OutgoingComparisonIndex == link.IncomingComparisonIndex
                    && ReferenceEquals(candidate.OutgoingSelectedMatch, link.IncomingSelectedMatch))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static SelectedMatchContinuityLink? FindSuccessor(
            SelectedMatchContinuityLink link, IReadOnlyList<SelectedMatchContinuityLink> links)
        {
            foreach (var candidate in links)
            {
                if (ReferenceEquals(candidate, link))
                {
                    continue;
                }

                if (candidate.IncomingComparisonIndex == link.OutgoingComparisonIndex
                    && ReferenceEquals(link.OutgoingSelectedMatch, candidate.IncomingSelectedMatch))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "4 runs, 3 adjacent comparisons, 2 continuity link(s), 1 continuity path(s)". Not a persistence key.</summary>
        public override string ToString() =>
            $"{ContinuityResult.SequenceComparison.Runs.Count} runs, "
            + $"{ContinuityResult.SequenceComparison.Comparisons.Count} adjacent comparisons, "
            + $"{ContinuityResult.Links.Count} continuity link(s), {Paths.Count} continuity path(s)";
    }
}
