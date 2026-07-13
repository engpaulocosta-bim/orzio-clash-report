using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Lifecycle
{
    /// <summary>
    /// A conservative <see cref="IClashLifecycleClassifier"/>: classifies every slot of an existing
    /// <see cref="ClashRunMatchResult"/> without ever rerunning matching. A selected match becomes
    /// <see cref="ClashLifecycleStatus.StillOpen"/> only when its confidence is
    /// <see cref="ClashMatchConfidence.Medium"/> or <see cref="ClashMatchConfidence.High"/> and no alternative
    /// candidate shares its previous or current index; otherwise it becomes
    /// <see cref="ClashLifecycleStatus.Unverifiable"/>. An unmatched previous occurrence becomes
    /// <see cref="ClashLifecycleStatus.Resolved"/> only when it has no competing alternative and both of its
    /// revision-free <see cref="ModelIdentity"/> sides plus its clash test are observed in the current run;
    /// an unmatched current occurrence becomes <see cref="ClashLifecycleStatus.New"/> under the symmetric
    /// condition against the previous run. Anything else is <see cref="ClashLifecycleStatus.Unverifiable"/>.
    ///
    /// <para>
    /// A clash test is considered "observed" in a run only when that run's
    /// <see cref="RunManifest.ExecutedClashTests"/> explicitly declares a test with the same name (ordinal,
    /// case-insensitive) and the same revision-free model pair, direct or A/B-swapped -- never by scanning
    /// that run's occurrences. This is what lets a run prove it executed a test and got zero results: the
    /// declaration alone is sufficient evidence, so an unmatched clash can be classified
    /// <see cref="ClashLifecycleStatus.Resolved"/> or <see cref="ClashLifecycleStatus.New"/> even when the
    /// other run has no occurrence at all for that test and model pair.
    /// </para>
    /// </summary>
    public sealed class ConservativeClashLifecycleClassifier : IClashLifecycleClassifier
    {
        public ClashLifecycleResult Classify(ClashRunMatchResult matchResult)
        {
            if (matchResult == null)
            {
                throw new ArgumentNullException(nameof(matchResult));
            }

            var entries = new List<ClashLifecycleEntry>();

            foreach (var selected in matchResult.SelectedMatches)
            {
                entries.Add(ClassifySelected(matchResult, selected));
            }

            var usedPreviousIndexes = new HashSet<int>();
            var usedCurrentIndexes = new HashSet<int>();
            foreach (var selected in matchResult.SelectedMatches)
            {
                usedPreviousIndexes.Add(selected.PreviousIndex);
                usedCurrentIndexes.Add(selected.CurrentIndex);
            }

            for (int previousIndex = 0; previousIndex < matchResult.PreviousRun.Occurrences.Count; previousIndex++)
            {
                if (!usedPreviousIndexes.Contains(previousIndex))
                {
                    entries.Add(ClassifyUnmatchedPrevious(matchResult, previousIndex));
                }
            }

            for (int currentIndex = 0; currentIndex < matchResult.CurrentRun.Occurrences.Count; currentIndex++)
            {
                if (!usedCurrentIndexes.Contains(currentIndex))
                {
                    entries.Add(ClassifyUnmatchedCurrent(matchResult, currentIndex));
                }
            }

            return new ClashLifecycleResult(matchResult, entries);
        }

        private static ClashLifecycleEntry ClassifySelected(ClashRunMatchResult matchResult, ClashRunMatchCandidate selected)
        {
            var evidence = new List<ClashLifecycleEvidence>
            {
                new ClashLifecycleEvidence(
                    ClashLifecycleEvidenceKind.SelectedMatch,
                    ClashLifecycleEvidenceVerdict.SupportsClassification,
                    "This previous/current pair was selected as a one-to-one match."),
            };

            var confidence = selected.Assessment.Confidence;
            bool confidenceSupports = confidence != ClashMatchConfidence.Low;
            evidence.Add(new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.MatchConfidence,
                confidenceSupports ? ClashLifecycleEvidenceVerdict.SupportsClassification : ClashLifecycleEvidenceVerdict.BlocksClassification,
                confidenceSupports
                    ? $"Match confidence is {confidence}. This is not treated as exact, only as sufficient to proceed."
                    : $"Match confidence is {confidence}, too weak to auto-classify."));

            int competingAlternatives = matchResult.AlternativeCandidates.Count(
                a => a.PreviousIndex == selected.PreviousIndex || a.CurrentIndex == selected.CurrentIndex);
            bool hasAmbiguity = competingAlternatives > 0;
            evidence.Add(new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.CandidateAmbiguity,
                hasAmbiguity ? ClashLifecycleEvidenceVerdict.BlocksClassification : ClashLifecycleEvidenceVerdict.SupportsClassification,
                hasAmbiguity
                    ? $"{competingAlternatives} alternative candidate(s) share this match's previous or current index."
                    : "No alternative candidate shares this match's previous or current index."));

            var status = HasBlocker(evidence) ? ClashLifecycleStatus.Unverifiable : ClashLifecycleStatus.StillOpen;

            return new ClashLifecycleEntry(
                status,
                selected.PreviousIndex,
                selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence,
                selected.Assessment.CurrentOccurrence,
                selected,
                evidence);
        }

        private static ClashLifecycleEntry ClassifyUnmatchedPrevious(ClashRunMatchResult matchResult, int previousIndex)
        {
            var occurrence = matchResult.PreviousRun.Occurrences[previousIndex];
            var evidence = BuildUnmatchedEvidence(
                occurrence,
                otherRun: matchResult.CurrentRun,
                competingAlternatives: matchResult.AlternativeCandidates.Count(a => a.PreviousIndex == previousIndex),
                slotLabel: "previous slot");

            var status = HasBlocker(evidence) ? ClashLifecycleStatus.Unverifiable : ClashLifecycleStatus.Resolved;

            return new ClashLifecycleEntry(status, previousIndex, null, occurrence, null, null, evidence);
        }

        private static ClashLifecycleEntry ClassifyUnmatchedCurrent(ClashRunMatchResult matchResult, int currentIndex)
        {
            var occurrence = matchResult.CurrentRun.Occurrences[currentIndex];
            var evidence = BuildUnmatchedEvidence(
                occurrence,
                otherRun: matchResult.PreviousRun,
                competingAlternatives: matchResult.AlternativeCandidates.Count(a => a.CurrentIndex == currentIndex),
                slotLabel: "current slot");

            var status = HasBlocker(evidence) ? ClashLifecycleStatus.Unverifiable : ClashLifecycleStatus.New;

            return new ClashLifecycleEntry(status, null, currentIndex, null, occurrence, null, evidence);
        }

        /// <summary>Builds the five evidence items shared by unmatched-previous and unmatched-current classification, checked against the occurrence's own run's opposite (<paramref name="otherRun"/>).</summary>
        private static List<ClashLifecycleEvidence> BuildUnmatchedEvidence(
            ClashOccurrence occurrence, CoordinationRun otherRun, int competingAlternatives, string slotLabel)
        {
            var evidence = new List<ClashLifecycleEvidence>();

            bool hasAmbiguity = competingAlternatives > 0;
            evidence.Add(new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.CandidateAmbiguity,
                hasAmbiguity ? ClashLifecycleEvidenceVerdict.BlocksClassification : ClashLifecycleEvidenceVerdict.SupportsClassification,
                hasAmbiguity
                    ? $"{competingAlternatives} alternative candidate(s) reference this {slotLabel}."
                    : $"No alternative candidate references this {slotLabel}."));

            evidence.Add(BuildModelIdentityPresenceEvidence("ModelA", occurrence.ModelA.Identity, otherRun));
            evidence.Add(BuildModelIdentityPresenceEvidence("ModelB", occurrence.ModelB.Identity, otherRun));

            bool testDeclared = IsClashTestDeclaredExecuted(occurrence, otherRun);
            evidence.Add(new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.ClashTestPresence,
                testDeclared ? ClashLifecycleEvidenceVerdict.SupportsClassification : ClashLifecycleEvidenceVerdict.BlocksClassification,
                testDeclared
                    ? $"Clash test '{occurrence.ClashTestName}' is explicitly declared as executed for this "
                      + "revision-free model pair in the other run's manifest."
                    : $"The manifest does not declare clash test '{occurrence.ClashTestName}' as executed for "
                      + "this model pair in the other run."));

            evidence.Add(new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.SelectedMatch,
                ClashLifecycleEvidenceVerdict.SupportsClassification,
                $"No selected match consumes this {slotLabel}."));

            return evidence;
        }

        private static ClashLifecycleEvidence BuildModelIdentityPresenceEvidence(string sideLabel, ModelIdentity identity, CoordinationRun otherRun)
        {
            bool present = otherRun.Models.Any(model => model.Identity.Equals(identity));

            return new ClashLifecycleEvidence(
                ClashLifecycleEvidenceKind.ModelIdentityPresence,
                present ? ClashLifecycleEvidenceVerdict.SupportsClassification : ClashLifecycleEvidenceVerdict.BlocksClassification,
                present
                    ? $"{sideLabel} identity '{identity}' is declared in the other run's manifest, regardless of revision."
                    : $"{sideLabel} identity '{identity}' is not declared in the other run's manifest.");
        }

        private static bool IsClashTestDeclaredExecuted(ClashOccurrence occurrence, CoordinationRun run) =>
            run.ExecutedClashTests.Any(test =>
                string.Equals(test.Name, occurrence.ClashTestName, StringComparison.OrdinalIgnoreCase)
                && ((test.ModelA.Equals(occurrence.ModelA.Identity) && test.ModelB.Equals(occurrence.ModelB.Identity))
                    || (test.ModelA.Equals(occurrence.ModelB.Identity) && test.ModelB.Equals(occurrence.ModelA.Identity))));

        private static bool HasBlocker(IEnumerable<ClashLifecycleEvidence> evidence) =>
            evidence.Any(e => e.Verdict == ClashLifecycleEvidenceVerdict.BlocksClassification);
    }
}
