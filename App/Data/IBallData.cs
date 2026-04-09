namespace Data
{
    public interface IBallData
    {
        int Id { get; }
        double X { get; }
        double Y { get; }
        int Diameter { get; }
        Vector2D Velocity { get; }
        byte R { get; }
        byte G { get; }
        byte B { get; }
    }
}