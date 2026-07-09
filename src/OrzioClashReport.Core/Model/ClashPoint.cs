namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable 3D coordinate where a clash was detected, in model units.</summary>
    public readonly struct ClashPoint
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public ClashPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
