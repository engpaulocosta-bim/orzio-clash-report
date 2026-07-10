using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Unit tests for MatchEvidence: a single auditable comparison signal between two clash occurrences.</summary>
    public class MatchEvidenceTests
    {
        public static IEnumerable<object[]> AllKinds()
        {
            foreach (MatchEvidenceKind kind in Enum.GetValues(typeof(MatchEvidenceKind)))
            {
                yield return new object[] { kind };
            }
        }

        public static IEnumerable<object[]> AllVerdicts()
        {
            foreach (MatchEvidenceVerdict verdict in Enum.GetValues(typeof(MatchEvidenceVerdict)))
            {
                yield return new object[] { verdict };
            }
        }

        [Theory]
        [MemberData(nameof(AllKinds))]
        public void Constructor_AcceptsAllDefinedMatchEvidenceKinds(MatchEvidenceKind kind)
        {
            var evidence = new MatchEvidence(kind, MatchEvidenceVerdict.Supports, "description", null, null);

            Assert.Equal(kind, evidence.Kind);
        }

        [Theory]
        [MemberData(nameof(AllVerdicts))]
        public void Constructor_AcceptsAllDefinedMatchEvidenceVerdicts(MatchEvidenceVerdict verdict)
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.ClashTestName, verdict, "description", null, null);

            Assert.Equal(verdict, evidence.Verdict);
        }

        [Fact]
        public void Constructor_RejectsUndefinedMatchEvidenceKind()
        {
            Assert.Throws<ArgumentException>(
                () => new MatchEvidence((MatchEvidenceKind)999, MatchEvidenceVerdict.Supports, "description", null, null));
        }

        [Fact]
        public void Constructor_RejectsUndefinedMatchEvidenceVerdict()
        {
            Assert.Throws<ArgumentException>(
                () => new MatchEvidence(MatchEvidenceKind.ClashTestName, (MatchEvidenceVerdict)999, "description", null, null));
        }

        [Fact]
        public void Constructor_RejectsNullDescription()
        {
            Assert.Throws<ArgumentNullException>(
                () => new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, null!, null, null));
        }

        [Fact]
        public void Constructor_RejectsEmptyDescription()
        {
            Assert.Throws<ArgumentException>(
                () => new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "", null, null));
        }

        [Fact]
        public void Constructor_RejectsWhitespaceDescription()
        {
            Assert.Throws<ArgumentException>(
                () => new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "   ", null, null));
        }

        [Fact]
        public void Constructor_TrimsDescription()
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "  same test name  ", null, null);

            Assert.Equal("same test name", evidence.Description);
        }

        [Fact]
        public void Constructor_TrimsPreviousValue()
        {
            var evidence = new MatchEvidence(
                MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "description", "  Test 1  ", null);

            Assert.Equal("Test 1", evidence.PreviousValue);
        }

        [Fact]
        public void Constructor_TrimsCurrentValue()
        {
            var evidence = new MatchEvidence(
                MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "description", null, "  Test 1  ");

            Assert.Equal("Test 1", evidence.CurrentValue);
        }

        [Fact]
        public void Constructor_EmptyPreviousValue_BecomesNull()
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "description", "", null);

            Assert.Null(evidence.PreviousValue);
        }

        [Fact]
        public void Constructor_WhitespaceCurrentValue_BecomesNull()
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "description", null, "   ");

            Assert.Null(evidence.CurrentValue);
        }

        [Fact]
        public void Constructor_PreservesKind()
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.SpatialPosition, MatchEvidenceVerdict.Contradicts, "description", null, null);

            Assert.Equal(MatchEvidenceKind.SpatialPosition, evidence.Kind);
        }

        [Fact]
        public void Constructor_PreservesVerdict()
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.SpatialPosition, MatchEvidenceVerdict.Contradicts, "description", null, null);

            Assert.Equal(MatchEvidenceVerdict.Contradicts, evidence.Verdict);
        }

        [Fact]
        public void Constructor_DoesNotInterpretPreviousValue()
        {
            var evidence = new MatchEvidence(
                MatchEvidenceKind.ElementIdentifierPair, MatchEvidenceVerdict.Contradicts, "description",
                "not-a-real-guid-just-a-string", null);

            Assert.Equal("not-a-real-guid-just-a-string", evidence.PreviousValue);
        }

        [Fact]
        public void Constructor_DoesNotInterpretCurrentValue()
        {
            var evidence = new MatchEvidence(
                MatchEvidenceKind.ElementIdentifierPair, MatchEvidenceVerdict.Contradicts, "description",
                null, "42");

            Assert.Equal("42", evidence.CurrentValue);
        }

        [Fact]
        public void ToString_IncludesKind()
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports, "source GUIDs are equal", null, null);

            Assert.Contains("SourceClashGuid", evidence.ToString());
        }

        [Fact]
        public void ToString_IncludesVerdict()
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports, "source GUIDs are equal", null, null);

            Assert.Contains("Supports", evidence.ToString());
        }

        [Fact]
        public void ToString_IncludesDescription()
        {
            var evidence = new MatchEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports, "source GUIDs are equal", null, null);

            Assert.Contains("source GUIDs are equal", evidence.ToString());
        }

        [Fact]
        public void Constructor_SourceClashGuid_CanHaveUnavailableVerdict()
        {
            var evidence = new MatchEvidence(
                MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Unavailable, "no GUID was reported on either side", null, null);

            Assert.Equal(MatchEvidenceKind.SourceClashGuid, evidence.Kind);
            Assert.Equal(MatchEvidenceVerdict.Unavailable, evidence.Verdict);
        }
    }
}
