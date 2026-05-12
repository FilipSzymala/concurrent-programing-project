namespace Data.Models
{
    internal sealed class BallEntity : IBallData
    {
        private readonly object _sync = new object();
        private double _x;
        private double _y;
        private Vector2D _velocity;

        public BallEntity(int id, double x, double y, int diameter, double mass,
                          Vector2D velocity, byte r, byte g, byte b)
        {
            Id = id;
            _x = x;
            _y = y;
            Diameter = diameter;
            Mass = mass;
            _velocity = velocity;
            R = r;
            G = g;
            B = b;
        }

        public int Id { get; }
        public int Diameter { get; }
        public double Mass { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public double X
        {
            get { lock (_sync) { return _x; } }
        }

        public double Y
        {
            get { lock (_sync) { return _y; } }
        }

        public Vector2D Velocity
        {
            get { lock (_sync) { return _velocity; } }
        }

        public BallSnapshot Snapshot
        {
            get
            {
                lock (_sync)
                {
                    return new BallSnapshot(_x, _y, _velocity);
                }
            }
        }

        public void ApplyChange(double newX, double newY, Vector2D newVelocity)
        {
            lock (_sync)
            {
                _x = newX;
                _y = newY;
                _velocity = newVelocity;
            }
        }

        public void SetVelocity(Vector2D newVelocity)
        {
            lock (_sync)
            {
                _velocity = newVelocity;
            }
        }

        public void Step(double dtMs)
        {
            const double nominalMs = 16.0;
            lock (_sync)
            {
                double scale = dtMs / nominalMs;
                _x += _velocity.X * scale;
                _y += _velocity.Y * scale;
            }
        }
    }
}
