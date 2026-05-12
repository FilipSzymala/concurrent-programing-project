namespace Data
{
    public interface IBallData
    {
        int Id { get; }
        int Diameter { get; }
        double Mass { get; }
        byte R { get; }
        byte G { get; }
        byte B { get; }

        double X { get; }
        double Y { get; }
        Vector2D Velocity { get; }

        BallSnapshot Snapshot { get; }

        void ApplyChange(double newX, double newY, Vector2D newVelocity);
        void SetVelocity(Vector2D newVelocity);
    }
}
