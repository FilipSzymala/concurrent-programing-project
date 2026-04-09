namespace Logic
{
    public interface IBallStatus
    {
        int Id { get; }
        double X { get; }
        double Y { get; }
        int Diameter { get; }
        byte R { get; }
        byte G { get; }
        byte B { get; }
    }
}