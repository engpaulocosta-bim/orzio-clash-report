using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable single clash between two elements, as reported by the source (maps to one &lt;clashresult&gt;).</summary>
    public sealed class ClashResult
    {
        public string? Name { get; }
        public ClashStatus Status { get; }
        public double? Distance { get; }
        public string? GridLocation { get; }
        public ClashPoint? Point { get; }
        public ClashObject ElementA { get; }
        public ClashObject ElementB { get; }
        public string? Guid { get; }

        public ClashResult(
            string? name,
            ClashStatus status,
            double? distance,
            string? gridLocation,
            ClashPoint? point,
            ClashObject elementA,
            ClashObject elementB,
            string? guid)
        {
            Name = name;
            Status = status;
            Distance = distance;
            GridLocation = gridLocation;
            Point = point;
            ElementA = elementA ?? throw new ArgumentNullException(nameof(elementA));
            ElementB = elementB ?? throw new ArgumentNullException(nameof(elementB));
            Guid = guid;
        }
    }
}
