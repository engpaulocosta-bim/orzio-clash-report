using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Unit tests for ClashLifecycleEvidence: a single auditable signal recorded while classifying one match-result slot's lifecycle.</summary>
    public class ClashLifecycleEvidenceTests
    {
        public static IEnumerable<object[]> AllKinds()
        {
            foreach (ClashLifecycleEvidenceKind kind in Enum.GetValues(typeof(ClashLifecycleEvidenceKind)))
            {
                yield return new object[] { kind };
            }
        }

        public static IEnumerable<object[]> AllVerdicts()
        {
            foreach (ClashLifecycleEvidenceVerdict verdict in Enum.GetValues(typeof(ClashLifecycleEvidenceVerdict)))
            {
                yield return new object[] { verdict };
            }
        }

        [Theory]
        [MemberData(nameof(AllKinds))]
        public void Constructor_AcceptsAllDefinedKinds(ClashLifecycleEvidenceKind kind)
        {
            var evidence = new ClashLifecycleEvidence(kind, ClashLifecycleEvidenceVerdict.SupportsClassification, "description");

            Assert.Equal(kind, evidence.Kind);
        }

        [Theory]
        [MemberData(nameof(AllVerdicts))]
        public void Constructor_AcceptsAllDefinedVerdicts(ClashLifecycleEvidenceVerdict verdict)
        {
            var evidence = new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.SelectedMatch, verdict, "description");

            Assert.Equal(verdict, evidence.Verdict);
        }

        [Fact]
        public void Constructor_RejectsUndefinedKind()
        {
            Assert.Throws<ArgumentException>(
                () => new ClashLifecycleEvidence((ClashLifecycleEvidenceKind)999, ClashLifecycleEvidenceVerdict.SupportsClassification, "description"));
        }

        [Fact]
        public void Constructor_RejectsUndefinedVerdict()
        {
            Assert.Throws<ArgumentException>(
                () => new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.SelectedMatch, (ClashLifecycleEvidenceVerdict)999, "description"));
        }

        [Fact]
        public void Constructor_RejectsNullDescription()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.SelectedMatch, ClashLifecycleEvidenceVerdict.SupportsClassification, null!));
        }

        [Fact]
        public void Constructor_RejectsEmptyDescription()
        {
            Assert.Throws<ArgumentException>(
                () => new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.SelectedMatch, ClashLifecycleEvidenceVerdict.SupportsClassification, ""));
        }

        [Fact]
        public void Constructor_RejectsWhitespaceDescription()
        {
            Assert.Throws<ArgumentException>(
                () => new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.SelectedMatch, ClashLifecycleEvidenceVerdict.SupportsClassification, "   "));
        }

        [Fact]
        public void Constructor_TrimsDescription()
        {
            var evidence = new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.SelectedMatch, ClashLifecycleEvidenceVerdict.SupportsClassification, "  no competing match  ");

            Assert.Equal("no competing match", evidence.Description);
        }

        [Fact]
        public void Constructor_PreservesKind()
        {
            var evidence = new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.ModelIdentityPresence, ClashLifecycleEvidenceVerdict.BlocksClassification, "description");

            Assert.Equal(ClashLifecycleEvidenceKind.ModelIdentityPresence, evidence.Kind);
        }

        [Fact]
        public void Constructor_PreservesVerdict()
        {
            var evidence = new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.ModelIdentityPresence, ClashLifecycleEvidenceVerdict.BlocksClassification, "description");

            Assert.Equal(ClashLifecycleEvidenceVerdict.BlocksClassification, evidence.Verdict);
        }

        [Fact]
        public void ToString_IncludesKind()
        {
            var evidence = new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.MatchConfidence, ClashLifecycleEvidenceVerdict.SupportsClassification, "confidence is High");

            Assert.Contains("MatchConfidence", evidence.ToString());
        }

        [Fact]
        public void ToString_IncludesVerdict()
        {
            var evidence = new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.MatchConfidence, ClashLifecycleEvidenceVerdict.SupportsClassification, "confidence is High");

            Assert.Contains("SupportsClassification", evidence.ToString());
        }

        [Fact]
        public void ToString_IncludesDescription()
        {
            var evidence = new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.MatchConfidence, ClashLifecycleEvidenceVerdict.SupportsClassification, "confidence is High");

            Assert.Contains("confidence is High", evidence.ToString());
        }
    }
}
