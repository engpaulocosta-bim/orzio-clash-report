using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using OrzioClashReport.Launcher.Contracts.Results;

namespace OrzioClashReport.Launcher.Application.Operations
{
    /// <summary>
    /// An explicitly ordered list of files. Order is a declaration by the user and is the only order
    /// that exists: this type never sorts by date, name, revision, or anything else, and moving an
    /// entry is the only way its position changes.
    /// </summary>
    /// <remarks>
    /// A repeated entry is kept exactly as declared and reported as a warning. Silently removing it
    /// would change the sequence the user asked for, and repeating a run is a legitimate declaration
    /// the engine already accepts.
    /// </remarks>
    public sealed class OrderedFileList
    {
        private readonly List<string> _paths = new List<string>();

        public IReadOnlyList<string> Paths => new ReadOnlyCollection<string>(_paths);

        public int Count => _paths.Count;

        /// <summary>Appends at the end. Never inserts by comparison, and never rejects a repeat.</summary>
        public void Add(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be empty.", nameof(path));
            }

            _paths.Add(path);
        }

        public void AddRange(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            foreach (string path in paths)
            {
                Add(path);
            }
        }

        public void RemoveAt(int index)
        {
            RequireIndex(index);
            _paths.RemoveAt(index);
        }

        public void Clear() => _paths.Clear();

        /// <summary>Moves one entry one position earlier. This is the only way order changes.</summary>
        public bool MoveUp(int index)
        {
            RequireIndex(index);

            if (index == 0)
            {
                return false;
            }

            (_paths[index - 1], _paths[index]) = (_paths[index], _paths[index - 1]);
            return true;
        }

        /// <summary>Moves one entry one position later. This is the only way order changes.</summary>
        public bool MoveDown(int index)
        {
            RequireIndex(index);

            if (index == _paths.Count - 1)
            {
                return false;
            }

            (_paths[index + 1], _paths[index]) = (_paths[index], _paths[index + 1]);
            return true;
        }

        /// <summary>
        /// Warnings about the declared sequence. They never change it: a duplicate is reported and
        /// kept, because the engine treats a repeated reference as a valid declaration.
        /// </summary>
        public IReadOnlyList<LauncherWarning> Warnings()
        {
            var warnings = new List<LauncherWarning>();
            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _paths.Count; i++)
            {
                if (seen.TryGetValue(_paths[i], out int first))
                {
                    warnings.Add(new LauncherWarning(
                        LauncherWarningKind.DuplicateOrderedInput,
                        $"A posição {i + 1} repete a posição {first + 1} ({Path.GetFileName(_paths[i])}). "
                        + "A repetição é mantida exatamente como declarada."));
                }
                else
                {
                    seen[_paths[i]] = i;
                }
            }

            return warnings;
        }

        private void RequireIndex(int index)
        {
            if (index < 0 || index >= _paths.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is outside the list.");
            }
        }
    }
}
