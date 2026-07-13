using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Assembly
{
    /// <summary>
    /// The first concrete <see cref="ICoordinationRunAssembler"/>: resolves each side of every clash to a
    /// declared <see cref="ModelRevision"/> using only exact, trimmed, case-insensitive equality between
    /// <see cref="ClashObject.SourceModel"/> and that revision's <see cref="ModelRevision.SourceFileName"/> or
    /// <see cref="ModelRevision.SourceFilePath"/>. No basename extraction, extension stripping, path
    /// normalization, substring/prefix/suffix matching, fuzzy matching, or revision/discipline/company
    /// inference is performed -- the manifest alone is authoritative. A missing <see cref="ClashObject.SourceModel"/>,
    /// zero matching manifest models, or more than one distinct matching manifest model are all assembly
    /// failures; there is no first-match fallback. Batch and clash order are preserved flat
    /// (batch-major, clash-minor), duplicates are preserved as separate occurrences, and A/B sides are never
    /// swapped. <see cref="CoordinationRun"/> remains the final authority for executed-clash-test coverage;
    /// this type does not duplicate that rule.
    /// </summary>
    public sealed class ExactSourceModelCoordinationRunAssembler : ICoordinationRunAssembler
    {
        public CoordinationRun Assemble(ClashReportDocument document, RunManifest manifest)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            var occurrences = new List<ClashOccurrence>();

            for (int batchIndex = 0; batchIndex < document.Batches.Count; batchIndex++)
            {
                var batch = document.Batches[batchIndex];
                string testName = RequireBatchName(batch, batchIndex, manifest.RunId);

                for (int clashIndex = 0; clashIndex < batch.Clashes.Count; clashIndex++)
                {
                    var clash = batch.Clashes[clashIndex];

                    var modelA = ResolveModel(clash.ElementA, manifest, batchIndex, clashIndex, "A", testName);
                    var modelB = ResolveModel(clash.ElementB, manifest, batchIndex, clashIndex, "B", testName);

                    ClashOccurrence occurrence;
                    try
                    {
                        occurrence = new ClashOccurrence(testName, clash, modelA, modelB);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new CoordinationRunAssemblyException(
                            $"Failed to build clash occurrence at batch index {batchIndex}, clash index {clashIndex} "
                            + $"for run '{manifest.RunId}': {ex.Message}",
                            ex);
                    }

                    occurrences.Add(occurrence);
                }
            }

            try
            {
                return new CoordinationRun(manifest, occurrences);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunAssemblyException(
                    $"Failed to assemble coordination run '{manifest.RunId}': {ex.Message}",
                    ex);
            }
        }

        private static string RequireBatchName(ClashBatch batch, int batchIndex, string runId)
        {
            if (string.IsNullOrWhiteSpace(batch.Name))
            {
                throw new CoordinationRunAssemblyException(
                    $"Batch at index {batchIndex} has no clash test name (ClashBatch.Name is null, empty, or "
                    + $"whitespace); cannot assemble run '{runId}'.");
            }

            return batch.Name!;
        }

        private static ModelRevision ResolveModel(
            ClashObject element, RunManifest manifest, int batchIndex, int clashIndex, string side, string testName)
        {
            if (string.IsNullOrWhiteSpace(element.SourceModel))
            {
                throw new CoordinationRunAssemblyException(
                    $"Clash object on side {side} at batch index {batchIndex}, clash index {clashIndex} "
                    + $"(clash test '{testName}', run '{manifest.RunId}') has no SourceModel; cannot resolve a "
                    + "model revision.");
            }

            string token = element.SourceModel!.Trim();

            var candidates = new List<ModelRevision>();
            for (int i = 0; i < manifest.Models.Count; i++)
            {
                var candidate = manifest.Models[i];
                bool matchesFileName = string.Equals(token, candidate.SourceFileName, StringComparison.OrdinalIgnoreCase);
                bool matchesFilePath = candidate.SourceFilePath != null
                    && string.Equals(token, candidate.SourceFilePath, StringComparison.OrdinalIgnoreCase);

                if (matchesFileName || matchesFilePath)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                throw new CoordinationRunAssemblyException(
                    $"SourceModel '{token}' on side {side} at batch index {batchIndex}, clash index {clashIndex} "
                    + $"(clash test '{testName}', run '{manifest.RunId}'): no manifest model matched SourceFileName "
                    + "or SourceFilePath.");
            }

            if (candidates.Count > 1)
            {
                var descriptions = new List<string>(candidates.Count);
                foreach (var candidate in candidates)
                {
                    descriptions.Add(candidate.ToString());
                }

                throw new CoordinationRunAssemblyException(
                    $"SourceModel '{token}' on side {side} at batch index {batchIndex}, clash index {clashIndex} "
                    + $"(clash test '{testName}', run '{manifest.RunId}') matched {candidates.Count} distinct "
                    + $"manifest models, which is ambiguous: {string.Join("; ", descriptions)}.");
            }

            return candidates[0];
        }
    }
}
