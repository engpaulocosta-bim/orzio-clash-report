using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Continuity
{
    /// <summary>
    /// A deterministic <see cref="IClashRunSequenceContinuityPathAssembler"/>: walks
    /// <see cref="ClashRunSequenceContinuityResult.Links"/> in canonical order, and for every link with no exact
    /// predecessor, follows its chain of exact successors to assemble one maximal
    /// <see cref="SelectedMatchContinuityPath"/>. Connectivity is decided only by exact selected-match
    /// object-reference equality across consecutive comparison boundaries -- never by value equality, candidate
    /// <c>ToString</c>, source GUID, or non-adjacent reconnection.
    /// </summary>
    public sealed class DeterministicSelectedMatchContinuityPathAssembler : IClashRunSequenceContinuityPathAssembler
    {
        public ClashRunSequenceContinuityPathsResult Assemble(ClashRunSequenceContinuityResult continuityResult)
        {
            if (continuityResult == null)
            {
                throw new ArgumentNullException(nameof(continuityResult));
            }

            var links = continuityResult.Links;
            var paths = new List<SelectedMatchContinuityPath>();

            foreach (var link in links)
            {
                if (HasExactPredecessor(link, links))
                {
                    continue;
                }

                var pathLinks = new List<SelectedMatchContinuityLink> { link };
                var current = link;

                while (TryGetExactSuccessor(current, links, out var successor))
                {
                    pathLinks.Add(successor!);
                    current = successor!;
                }

                paths.Add(new SelectedMatchContinuityPath(pathLinks));
            }

            return new ClashRunSequenceContinuityPathsResult(continuityResult, paths);
        }

        /// <summary>
        /// Determines whether <paramref name="link"/> has an exact predecessor among <paramref name="links"/>:
        /// another link whose <see cref="SelectedMatchContinuityLink.OutgoingComparisonIndex"/> equals
        /// <paramref name="link"/>'s <see cref="SelectedMatchContinuityLink.IncomingComparisonIndex"/> and whose
        /// <see cref="SelectedMatchContinuityLink.OutgoingSelectedMatch"/> is the exact same object reference as
        /// <paramref name="link"/>'s <see cref="SelectedMatchContinuityLink.IncomingSelectedMatch"/>. Under normal
        /// continuity-link construction at most one such predecessor can exist; finding more than one indicates
        /// corrupted or impossible input, and is reported rather than resolved by picking the first match.
        /// </summary>
        private static bool HasExactPredecessor(
            SelectedMatchContinuityLink link, IReadOnlyList<SelectedMatchContinuityLink> links)
        {
            SelectedMatchContinuityLink? found = null;

            foreach (var candidate in links)
            {
                if (ReferenceEquals(candidate, link))
                {
                    continue;
                }

                if (candidate.OutgoingComparisonIndex == link.IncomingComparisonIndex
                    && ReferenceEquals(candidate.OutgoingSelectedMatch, link.IncomingSelectedMatch))
                {
                    if (found != null)
                    {
                        throw new InvalidOperationException(
                            $"Link {link} has more than one exact predecessor, which is impossible under normal "
                            + "continuity-link construction.");
                    }

                    found = candidate;
                }
            }

            return found != null;
        }

        /// <summary>
        /// Determines the exact successor of <paramref name="link"/> among <paramref name="links"/>: another
        /// link whose <see cref="SelectedMatchContinuityLink.IncomingComparisonIndex"/> equals <paramref name="link"/>'s
        /// <see cref="SelectedMatchContinuityLink.OutgoingComparisonIndex"/> and whose
        /// <see cref="SelectedMatchContinuityLink.IncomingSelectedMatch"/> is the exact same object reference as
        /// <paramref name="link"/>'s <see cref="SelectedMatchContinuityLink.OutgoingSelectedMatch"/>. Under normal
        /// continuity-link construction at most one such successor can exist; finding more than one indicates
        /// corrupted or impossible input, and is reported rather than resolved by picking the first match.
        /// </summary>
        private static bool TryGetExactSuccessor(
            SelectedMatchContinuityLink link,
            IReadOnlyList<SelectedMatchContinuityLink> links,
            out SelectedMatchContinuityLink? successor)
        {
            SelectedMatchContinuityLink? found = null;

            foreach (var candidate in links)
            {
                if (ReferenceEquals(candidate, link))
                {
                    continue;
                }

                if (candidate.IncomingComparisonIndex == link.OutgoingComparisonIndex
                    && ReferenceEquals(link.OutgoingSelectedMatch, candidate.IncomingSelectedMatch))
                {
                    if (found != null)
                    {
                        throw new InvalidOperationException(
                            $"Link {link} has more than one exact successor, which is impossible under normal "
                            + "continuity-link construction.");
                    }

                    found = candidate;
                }
            }

            successor = found;
            return found != null;
        }
    }
}
