using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable bucket of clash results sharing the same discipline pair and level.</summary>
    public sealed class ClashGroup
    {
        public string Key { get; }
        public string DisciplineA { get; }
        public string DisciplineB { get; }
        public string? Level { get; }
        public IReadOnlyList<ClashResult> Members { get; }
        public ClashResult RepresentativeClash { get; }

        public ClashGroup(string disciplineA, string disciplineB, string? level, IReadOnlyList<ClashResult> members)
        {
            DisciplineA = disciplineA ?? throw new ArgumentNullException(nameof(disciplineA));
            DisciplineB = disciplineB ?? throw new ArgumentNullException(nameof(disciplineB));
            Level = level;
            Members = members ?? throw new ArgumentNullException(nameof(members));

            if (Members.Count == 0)
            {
                throw new ArgumentException("A clash group must have at least one member.", nameof(members));
            }

            RepresentativeClash = Members[0];
            Key = $"{DisciplineA}|{DisciplineB}|{Level ?? "(none)"}";
        }
    }
}
