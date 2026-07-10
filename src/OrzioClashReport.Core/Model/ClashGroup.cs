using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable bucket of clash results sharing the same clash test, discipline pair, and level.</summary>
    public sealed class ClashGroup
    {
        public string? ClashTestName { get; }
        public string Key { get; }
        public string DisciplineA { get; }
        public string DisciplineB { get; }
        public string? Level { get; }
        public IReadOnlyList<ClashResult> Members { get; }
        public ClashResult RepresentativeClash { get; }

        public ClashGroup(
            string? clashTestName, string disciplineA, string disciplineB, string? level, IReadOnlyList<ClashResult> members)
        {
            ClashTestName = clashTestName;
            DisciplineA = disciplineA ?? throw new ArgumentNullException(nameof(disciplineA));
            DisciplineB = disciplineB ?? throw new ArgumentNullException(nameof(disciplineB));
            Level = level;

            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            Members = new List<ClashResult>(members).AsReadOnly();

            if (Members.Count == 0)
            {
                throw new ArgumentException("A clash group must have at least one member.", nameof(members));
            }

            RepresentativeClash = Members[0];
            Key = $"{ClashTestName ?? "(unnamed test)"}|{DisciplineA}|{DisciplineB}|{Level ?? "(none)"}";
        }
    }
}
