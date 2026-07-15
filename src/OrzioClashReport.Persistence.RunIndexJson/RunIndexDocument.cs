using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Persistence.RunIndexJson
{
    /// <summary>Represents an ordered collection of persisted run-snapshot path references.</summary>
    public sealed class RunIndexDocument
    {
        private readonly ReadOnlyCollection<string> snapshotPaths;

        public RunIndexDocument(IReadOnlyList<string> snapshotPaths)
        {
            if (snapshotPaths == null)
            {
                throw new ArgumentNullException(nameof(snapshotPaths));
            }

            var copy = new string[snapshotPaths.Count];
            for (int i = 0; i < snapshotPaths.Count; i++)
            {
                string? value = snapshotPaths[i];
                if (value == null)
                {
                    throw new ArgumentException("Snapshot path entries cannot be null.", nameof(snapshotPaths));
                }

                if (value.Trim().Length == 0)
                {
                    throw new ArgumentException("Snapshot path entries cannot be empty or whitespace.", nameof(snapshotPaths));
                }

                RunIndexPathReferenceValidator.EnsureCanonicalReferenceForConstructor(value, nameof(snapshotPaths));
                copy[i] = value;
            }

            this.snapshotPaths = Array.AsReadOnly(copy);
        }

        public IReadOnlyList<string> SnapshotPaths => snapshotPaths;
    }
}
