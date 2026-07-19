using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="SelectedMatchContinuityPath"/> invariants, invoking its internal constructor through
    /// reflection (no InternalsVisibleTo, no visibility increase). Valid links are produced through the real
    /// matcher/run-comparer/lifecycle-classifier/sequence-comparer/projector pipeline, never hand-built.
    /// </summary>
    public class SelectedMatchContinuityPathTests
    {
        private static readonly ConstructorInfo PathConstructor =
            typeof(SelectedMatchContinuityPath).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(System.Collections.Generic.IReadOnlyList<SelectedMatchContinuityLink>) },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find SelectedMatchContinuityPath internal constructor.");

        private static SelectedMatchContinuityPath CreatePathViaReflection(
            System.Collections.Generic.IReadOnlyList<SelectedMatchContinuityLink>? links)
        {
            try
            {
                return (SelectedMatchContinuityPath)PathConstructor.Invoke(new object?[] { links });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static readonly DateTimeOffset DefaultCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

        private static readonly ModelRevision Sigma =
            new ModelRevision(new ModelIdentity("Sigma", "Structure", "Main"), "R04", "Sigma_Main_R04.nwc", null, null, null);

        private static readonly ExecutedClashTest Test1ForSigma = new ExecutedClashTest("Test 1", Sigma.Identity, Sigma.Identity);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static ClashOccurrence MakeOccurrence(string tag) =>
            new ClashOccurrence(
                "Test 1",
                new ClashResult("Test 1", ClashStatus.New, null, null, null, MakeObject($"{tag}-a"), MakeObject($"{tag}-b"), null),
                Sigma, Sigma);

        private static RunManifest MakeManifest(string runId, DateTimeOffset createdAt) =>
            new RunManifest(runId, createdAt, new[] { Sigma }, new[] { Test1ForSigma });

        private static CoordinationRun MakeRun(string runId, DateTimeOffset createdAt, params ClashOccurrence[] occurrences) =>
            new CoordinationRun(MakeManifest(runId, createdAt), occurrences);

        private static CoordinationRun MakeRun(string runId, params ClashOccurrence[] occurrences) =>
            MakeRun(runId, DefaultCreatedAt, occurrences);

        private static ClashRunSequenceComparisonResult BuildSequenceComparison(params CoordinationRun[] runs)
        {
            IClashMatcher matcher = new ConservativeClashMatcher();
            IClashRunComparer runComparer = new DeterministicClashRunComparer(matcher);
            IClashLifecycleClassifier classifier = new ConservativeClashLifecycleClassifier();
            IClashRunSequenceComparer sequenceComparer = new DeterministicAdjacentClashRunSequenceComparer(runComparer, classifier);
            return sequenceComparer.Compare(runs);
        }

        private static ClashRunSequenceContinuityResult ProjectContinuity(ClashRunSequenceComparisonResult sequenceComparison) =>
            new DeterministicSelectedMatchContinuityProjector().Project(sequenceComparison);

        /// <summary>3-run chain (A,B,C) sharing one occurrence, yielding exactly one continuity link.</summary>
        private static ClashRunSequenceContinuityResult CreateSingleLinkContinuity()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>4-run chain (A,B,C,D) sharing one occurrence, yielding exactly two connected continuity links.</summary>
        private static ClashRunSequenceContinuityResult CreateTwoLinkChainContinuity()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>5-run chain (A,B,C,D,E) sharing one occurrence, yielding exactly three connected continuity links.</summary>
        private static ClashRunSequenceContinuityResult CreateThreeLinkChainContinuity()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ), MakeRun("run-E", occ));
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>
        /// 4-run fixture (A,B,C,D) producing exactly two links whose boundary indices are consecutive
        /// (Links[0].OutgoingComparisonIndex == Links[1].IncomingComparisonIndex == 1) but whose selected-match
        /// object references differ, isolating exact-reference connectivity from boundary-index adjacency:
        /// A=[u], B=[u,w], C=[w,u], D=[w]. B[0]=u matches C[1]=u (candidate p); B[1]=w matches C[0]=w (candidate
        /// q); p and q are distinct objects, so Links[0]'s outgoing (via p) and Links[1]'s incoming (via q) never
        /// share a reference even though both sit at the same boundary index.
        /// </summary>
        private static ClashRunSequenceContinuityResult CreateSameIndexDisconnectedLinksContinuity()
        {
            var u = "u";
            var w = "w";
            var runA = MakeRun("run-A", MakeOccurrence(u));
            var runB = MakeRun("run-B", MakeOccurrence(u), MakeOccurrence(w));
            var runC = MakeRun("run-C", MakeOccurrence(w), MakeOccurrence(u));
            var runD = MakeRun("run-D", MakeOccurrence(w));
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC, runD);
            return ProjectContinuity(sequenceComparison);
        }

        // ===================== acceptance =====================

        [Fact]
        public void Path_ValidOneLinkPath_IsAccepted()
        {
            var continuity = CreateSingleLinkContinuity();

            var path = CreatePathViaReflection(new[] { continuity.Links[0] });

            Assert.Single(path.Links);
            Assert.Equal(2, path.SelectedMatches.Count);
        }

        [Fact]
        public void Path_ValidTwoLinkPath_IsAccepted()
        {
            var continuity = CreateTwoLinkChainContinuity();
            Assert.Equal(2, continuity.Links.Count);

            var path = CreatePathViaReflection(continuity.Links);

            Assert.Equal(2, path.Links.Count);
            Assert.Equal(3, path.SelectedMatches.Count);
        }

        [Fact]
        public void Path_ValidThreeLinkPath_IsAccepted()
        {
            var continuity = CreateThreeLinkChainContinuity();
            Assert.Equal(3, continuity.Links.Count);

            var path = CreatePathViaReflection(continuity.Links);

            Assert.Equal(3, path.Links.Count);
            Assert.Equal(4, path.SelectedMatches.Count);
        }

        // ===================== rejection: null / empty =====================

        [Fact]
        public void Path_RejectsNullLinks()
        {
            Assert.Throws<ArgumentNullException>(() => CreatePathViaReflection(null));
        }

        [Fact]
        public void Path_RejectsEmptyLinks()
        {
            Assert.Throws<ArgumentException>(() => CreatePathViaReflection(Array.Empty<SelectedMatchContinuityLink>()));
        }

        [Fact]
        public void Path_RejectsNullFirstLink()
        {
            Assert.Throws<ArgumentException>(() =>
                CreatePathViaReflection(new SelectedMatchContinuityLink?[] { null }!));
        }

        [Fact]
        public void Path_RejectsNullLaterLink()
        {
            var continuity = CreateTwoLinkChainContinuity();

            Assert.Throws<ArgumentException>(() =>
                CreatePathViaReflection(new SelectedMatchContinuityLink?[] { continuity.Links[0], null }!));
        }

        // ===================== rejection: boundary progression =====================

        [Fact]
        public void Path_RejectsRepeatedBoundary()
        {
            var continuity = CreateTwoLinkChainContinuity();
            var link = continuity.Links[0];

            Assert.Throws<ArgumentException>(() => CreatePathViaReflection(new[] { link, link }));
        }

        [Fact]
        public void Path_RejectsInvertedBoundary()
        {
            var continuity = CreateTwoLinkChainContinuity();

            // Links[0] is boundary 0, Links[1] is boundary 1 -- reversing the order inverts the progression.
            Assert.Throws<ArgumentException>(() => CreatePathViaReflection(new[] { continuity.Links[1], continuity.Links[0] }));
        }

        [Fact]
        public void Path_RejectsBoundaryGap()
        {
            var continuity = CreateThreeLinkChainContinuity();
            Assert.Equal(3, continuity.Links.Count);

            // Links[0] (boundary 0) and Links[2] (boundary 2) skip boundary 1 entirely.
            Assert.Throws<ArgumentException>(() => CreatePathViaReflection(new[] { continuity.Links[0], continuity.Links[2] }));
        }

        // ===================== rejection: exact reference continuity =====================

        [Fact]
        public void Path_RejectsDifferentCandidateReferences()
        {
            // Boundary indices already agree (Links[0].OutgoingComparisonIndex == Links[1].IncomingComparisonIndex),
            // isolating the exact-reference check: the two links' selected-match objects are still distinct.
            var continuity = CreateSameIndexDisconnectedLinksContinuity();
            Assert.Equal(2, continuity.Links.Count);
            Assert.Equal(continuity.Links[0].OutgoingComparisonIndex, continuity.Links[1].IncomingComparisonIndex);
            Assert.NotSame(continuity.Links[0].OutgoingSelectedMatch, continuity.Links[1].IncomingSelectedMatch);

            Assert.Throws<ArgumentException>(() => CreatePathViaReflection(new[] { continuity.Links[0], continuity.Links[1] }));
        }

        [Fact]
        public void Path_RejectsValueEquivalentDistinctCandidateReference()
        {
            var chain = CreateTwoLinkChainContinuity();

            // Rebuild an independent, value-equivalent sequence comparison over fresh occurrence objects with the
            // exact same tag/shape; its boundary-1 link is structurally value-identical but a distinct object.
            var freshChain = CreateTwoLinkChainContinuity();
            Assert.NotSame(chain.Links[1], freshChain.Links[1]);
            Assert.NotSame(chain.Links[1].IncomingSelectedMatch, freshChain.Links[1].IncomingSelectedMatch);

            Assert.Throws<ArgumentException>(() => CreatePathViaReflection(new[] { chain.Links[0], freshChain.Links[1] }));
        }

        [Fact]
        public void Path_AcceptsExactSharedCandidateReference()
        {
            var chain = CreateTwoLinkChainContinuity();

            Assert.Same(chain.Links[0].OutgoingSelectedMatch, chain.Links[1].IncomingSelectedMatch);

            var path = CreatePathViaReflection(chain.Links);

            Assert.Equal(2, path.Links.Count);
        }

        [Fact]
        public void Path_ExactLinkReferencesArePreserved()
        {
            var chain = CreateTwoLinkChainContinuity();

            var path = CreatePathViaReflection(chain.Links);

            Assert.Same(chain.Links[0], path.Links[0]);
            Assert.Same(chain.Links[1], path.Links[1]);
        }

        // ===================== SelectedMatches derivation =====================

        [Fact]
        public void Path_SelectedMatchesAreDerivedExactly()
        {
            var chain = CreateTwoLinkChainContinuity();

            var path = CreatePathViaReflection(chain.Links);

            Assert.Same(chain.Links[0].IncomingSelectedMatch, path.SelectedMatches[0]);
            Assert.Same(chain.Links[0].OutgoingSelectedMatch, path.SelectedMatches[1]);
            Assert.Same(chain.Links[1].OutgoingSelectedMatch, path.SelectedMatches[2]);
        }

        [Fact]
        public void Path_SelectedMatchesCount_EqualsLinksCountPlusOne()
        {
            var chain = CreateThreeLinkChainContinuity();

            var path = CreatePathViaReflection(chain.Links);

            Assert.Equal(path.Links.Count + 1, path.SelectedMatches.Count);
        }

        // ===================== derived indices =====================

        [Fact]
        public void Path_StartComparisonIndex_IsFirstLinkIncomingIndex()
        {
            var chain = CreateThreeLinkChainContinuity();

            var path = CreatePathViaReflection(chain.Links);

            Assert.Equal(chain.Links[0].IncomingComparisonIndex, path.StartComparisonIndex);
            Assert.Equal(0, path.StartComparisonIndex);
        }

        [Fact]
        public void Path_EndComparisonIndex_IsLastLinkOutgoingIndex()
        {
            var chain = CreateThreeLinkChainContinuity();

            var path = CreatePathViaReflection(chain.Links);

            Assert.Equal(chain.Links[chain.Links.Count - 1].OutgoingComparisonIndex, path.EndComparisonIndex);
            Assert.Equal(3, path.EndComparisonIndex);
        }

        [Fact]
        public void Path_StartRunIndex_EqualsStartComparisonIndex()
        {
            var chain = CreateThreeLinkChainContinuity();

            var path = CreatePathViaReflection(chain.Links);

            Assert.Equal(path.StartComparisonIndex, path.StartRunIndex);
        }

        [Fact]
        public void Path_EndRunIndex_EqualsEndComparisonIndexPlusOne()
        {
            var chain = CreateThreeLinkChainContinuity();

            var path = CreatePathViaReflection(chain.Links);

            Assert.Equal(path.EndComparisonIndex + 1, path.EndRunIndex);
            Assert.Equal(4, path.EndRunIndex);
        }

        // ===================== immutability =====================

        [Fact]
        public void Path_InputLinksMutationDoesNotAffectPath()
        {
            var chain = CreateTwoLinkChainContinuity();
            var mutableLinks = new System.Collections.Generic.List<SelectedMatchContinuityLink> { chain.Links[0], chain.Links[1] };

            var path = CreatePathViaReflection(mutableLinks);
            mutableLinks.Clear();

            Assert.Equal(2, path.Links.Count);
        }

        [Fact]
        public void Path_LinksIsRuntimeReadOnly()
        {
            var chain = CreateSingleLinkContinuity();
            var path = CreatePathViaReflection(new[] { chain.Links[0] });

            Assert.IsNotType<System.Collections.Generic.List<SelectedMatchContinuityLink>>(path.Links);
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<SelectedMatchContinuityLink>)path.Links).Add(chain.Links[0]));
        }

        [Fact]
        public void Path_SelectedMatchesIsRuntimeReadOnly()
        {
            var chain = CreateSingleLinkContinuity();
            var path = CreatePathViaReflection(new[] { chain.Links[0] });

            Assert.IsNotType<System.Collections.Generic.List<ClashRunMatchCandidate>>(path.SelectedMatches);
            Assert.Throws<NotSupportedException>(() =>
                ((System.Collections.Generic.IList<ClashRunMatchCandidate>)path.SelectedMatches).Add(path.SelectedMatches[0]));
        }

        // ===================== no persistent identity =====================

        [Fact]
        public void Path_ExposesNoIdOrTrackOrFingerprintProperties()
        {
            var forbiddenNames = new[]
            {
                "Id", "PathId", "TrackId", "ChainId", "StableId", "PersistentId", "ClashId", "LedgerId", "Fingerprint", "Status",
            };

            var properties = typeof(SelectedMatchContinuityPath).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToList();

            foreach (var forbidden in forbiddenNames)
            {
                Assert.DoesNotContain(forbidden, properties);
            }
        }

        // ===================== ToString =====================

        [Fact]
        public void Path_ToString_ExactExample()
        {
            var chain = CreateTwoLinkChainContinuity();

            var path = CreatePathViaReflection(chain.Links);

            Assert.Equal("comparisons[0..2], 2 continuity link(s), 3 selected match(es)", path.ToString());
        }
    }
}
