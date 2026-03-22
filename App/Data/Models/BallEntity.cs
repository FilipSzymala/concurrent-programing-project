namespace Data.Models
{
    public class BallEntity
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int Diameter { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        
        
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        
    }
}