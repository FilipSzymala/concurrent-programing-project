namespace Data
{
    public readonly struct BallSnapshot
    {
        public double X { get; }
        public double Y { get; }
        public Vector2D Velocity { get; }

        public BallSnapshot(double x, double y, Vector2D velocity)
        {
            X = x;
            Y = y;
            Velocity = velocity;
        }
    }
}
