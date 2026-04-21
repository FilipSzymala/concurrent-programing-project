namespace Data.Models
{
    internal class BallEntity : IBallData
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int Diameter { get; set; }
        public Vector2D Velocity { get; set; }
        public double VelocityX => Velocity.X;
        public double VelocityY => Velocity.Y;
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }
}