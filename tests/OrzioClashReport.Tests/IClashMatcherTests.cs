using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Locks the public shape of IClashMatcher (a pairwise Core port) and proves it is usable both by an
    /// implementation that returns a candidate assessment and by one that refuses to. Contains no real
    /// matching algorithm: there is none to test at this step.
    /// </summary>
    public class IClashMatcherTests
    {
        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static ClashObject MakeObject(string id) => new ClashObject(id, null, null, null, null, null);

        private static ClashResult MakeClash(string name = "Clash1", string guid = "g1") =>
            new ClashResult(name, ClashStatus.New, null, null, null, MakeObject("a"), MakeObject("b"), guid);

        private static ClashOccurrence MakeOccurrence(string clashTestName = "Test 1") =>
            new ClashOccurrence(
                clashTestName, MakeClash(),
                MakeRevision("Sigma", "Structure", "Main", "R04"),
                MakeRevision("Alfa", "HVAC", "Main", "R07"));

        // --- 11.1 Public shape of the interface, via reflection ---

        [Fact]
        public void IClashMatcher_IsPublicInterface()
        {
            var type = typeof(IClashMatcher);

            Assert.True(type.IsInterface);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void IClashMatcher_DeclaresExactlyOnePublicMethod()
        {
            var methods = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.Single(methods);
        }

        [Fact]
        public void IClashMatcher_MethodIsNamedAssess()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal("Assess", method.Name);
        }

        [Fact]
        public void IClashMatcher_ReturnTypeIsClashMatchAssessment()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal(typeof(ClashMatchAssessment), method.ReturnType);
        }

        [Fact]
        public void IClashMatcher_HasExactlyTwoParameters()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal(2, method.GetParameters().Length);
        }

        [Fact]
        public void IClashMatcher_FirstParameterIsNamedPreviousOccurrence()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal("previousOccurrence", method.GetParameters()[0].Name);
        }

        [Fact]
        public void IClashMatcher_SecondParameterIsNamedCurrentOccurrence()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.Equal("currentOccurrence", method.GetParameters()[1].Name);
        }

        [Fact]
        public void IClashMatcher_BothParametersAreClashOccurrence()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();
            var parameters = method.GetParameters();

            Assert.Equal(typeof(ClashOccurrence), parameters[0].ParameterType);
            Assert.Equal(typeof(ClashOccurrence), parameters[1].ParameterType);
        }

        [Fact]
        public void IClashMatcher_DoesNotAcceptCoordinationRun()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.DoesNotContain(method.GetParameters(), p => p.ParameterType == typeof(CoordinationRun));
        }

        [Fact]
        public void IClashMatcher_DoesNotAcceptACollection()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.DoesNotContain(method.GetParameters(), p => typeof(System.Collections.IEnumerable).IsAssignableFrom(p.ParameterType));
        }

        [Fact]
        public void IClashMatcher_DoesNotAcceptCancellationToken()
        {
            var method = typeof(IClashMatcher).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single();

            Assert.DoesNotContain(method.GetParameters(), p => p.ParameterType == typeof(CancellationToken));
        }

        // --- 11.2 Fake that returns an assessment ---

        /// <summary>Test-only fake: records the arguments it received and returns an assessment built from the exact same references. No real matching logic.</summary>
        private sealed class ReturningMatcher : IClashMatcher
        {
            public ClashOccurrence? ReceivedPrevious { get; private set; }
            public ClashOccurrence? ReceivedCurrent { get; private set; }
            public ClashMatchConfidence ConfidenceToReturn { get; set; } = ClashMatchConfidence.Low;
            public MatchEvidence[] EvidenceToReturn { get; set; } =
                { new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "same clash test name", null, null) };

            public ClashMatchAssessment? Assess(ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence)
            {
                if (previousOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(previousOccurrence));
                }

                if (currentOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(currentOccurrence));
                }

                ReceivedPrevious = previousOccurrence;
                ReceivedCurrent = currentOccurrence;

                return new ClashMatchAssessment(previousOccurrence, currentOccurrence, ConfidenceToReturn, EvidenceToReturn);
            }
        }

        [Fact]
        public void ReturningMatcher_ReceivesPreviousAndCurrentInCorrectOrder()
        {
            var matcher = new ReturningMatcher();
            var previous = MakeOccurrence("Previous Test");
            var current = MakeOccurrence("Current Test");

            matcher.Assess(previous, current);

            Assert.Same(previous, matcher.ReceivedPrevious);
            Assert.Same(current, matcher.ReceivedCurrent);
        }

        [Fact]
        public void ReturningMatcher_ResultPreservesPreviousOccurrenceReference()
        {
            var matcher = new ReturningMatcher();
            var previous = MakeOccurrence();
            var current = MakeOccurrence();

            var result = matcher.Assess(previous, current);

            Assert.Same(previous, result!.PreviousOccurrence);
        }

        [Fact]
        public void ReturningMatcher_ResultPreservesCurrentOccurrenceReference()
        {
            var matcher = new ReturningMatcher();
            var previous = MakeOccurrence();
            var current = MakeOccurrence();

            var result = matcher.Assess(previous, current);

            Assert.Same(current, result!.CurrentOccurrence);
        }

        [Fact]
        public void ReturningMatcher_ResultCanBeLow()
        {
            var matcher = new ReturningMatcher { ConfidenceToReturn = ClashMatchConfidence.Low };

            var result = matcher.Assess(MakeOccurrence(), MakeOccurrence());

            Assert.NotNull(result);
            Assert.Equal(ClashMatchConfidence.Low, result!.Confidence);
        }

        [Fact]
        public void ReturningMatcher_ResultCanContainSupportsContradictsAndUnavailableEvidence()
        {
            var matcher = new ReturningMatcher
            {
                EvidenceToReturn = new[]
                {
                    new MatchEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports, "guids equal", null, null),
                    new MatchEvidence(MatchEvidenceKind.SpatialPosition, MatchEvidenceVerdict.Contradicts, "far apart", null, null),
                    new MatchEvidence(MatchEvidenceKind.ElementMetadata, MatchEvidenceVerdict.Unavailable, "no metadata", null, null),
                }
            };

            var result = matcher.Assess(MakeOccurrence(), MakeOccurrence());

            Assert.NotNull(result);
            Assert.Equal(3, result!.Evidence.Count);
            Assert.Equal(MatchEvidenceVerdict.Supports, result.Evidence[0].Verdict);
            Assert.Equal(MatchEvidenceVerdict.Contradicts, result.Evidence[1].Verdict);
            Assert.Equal(MatchEvidenceVerdict.Unavailable, result.Evidence[2].Verdict);
        }

        [Fact]
        public void ReturningMatcher_RejectsNullPreviousOccurrence()
        {
            var matcher = new ReturningMatcher();

            Assert.Throws<ArgumentNullException>(() => matcher.Assess(null!, MakeOccurrence()));
        }

        [Fact]
        public void ReturningMatcher_RejectsNullCurrentOccurrence()
        {
            var matcher = new ReturningMatcher();

            Assert.Throws<ArgumentNullException>(() => matcher.Assess(MakeOccurrence(), null!));
        }

        // --- 11.3 Fake that rejects a candidate ---

        /// <summary>Test-only fake: always refuses to produce a candidate assessment. No real matching logic.</summary>
        private sealed class RejectingMatcher : IClashMatcher
        {
            public ClashMatchAssessment? Assess(ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence)
            {
                if (previousOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(previousOccurrence));
                }

                if (currentOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(currentOccurrence));
                }

                return null;
            }
        }

        [Fact]
        public void IClashMatcher_CanReturnNull()
        {
            IClashMatcher matcher = new RejectingMatcher();

            var result = matcher.Assess(MakeOccurrence(), MakeOccurrence());

            Assert.Null(result);
        }

        [Fact]
        public void RejectingMatcher_NullResultIsAcceptedWithoutAnArtificialAssessment()
        {
            IClashMatcher matcher = new RejectingMatcher();

            var result = matcher.Assess(MakeOccurrence(), MakeOccurrence());

            Assert.Null(result);
        }

        [Fact]
        public void RejectingMatcher_RejectsNullPreviousOccurrence()
        {
            IClashMatcher matcher = new RejectingMatcher();

            Assert.Throws<ArgumentNullException>(() => matcher.Assess(null!, MakeOccurrence()));
        }

        [Fact]
        public void RejectingMatcher_RejectsNullCurrentOccurrence()
        {
            IClashMatcher matcher = new RejectingMatcher();

            Assert.Throws<ArgumentNullException>(() => matcher.Assess(MakeOccurrence(), null!));
        }
    }
}
