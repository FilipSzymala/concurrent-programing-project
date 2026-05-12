using System;
using Data;

namespace Logic.Physics
{
    public static class CollisionResolver
    {
        public static bool ResolveWalls(IBallData ball, int boardWidth, int boardHeight)
        {
            if (ball == null) throw new ArgumentNullException(nameof(ball));

            BallSnapshot snap = ball.Snapshot;
            double maxX = boardWidth - ball.Diameter;
            double maxY = boardHeight - ball.Diameter;

            Vector2D velocity = snap.Velocity;
            bool bounced = false;

            if (snap.X < 0 && velocity.X < 0)
            {
                velocity = velocity.WithX(-velocity.X);
                bounced = true;
            }
            else if (snap.X > maxX && velocity.X > 0)
            {
                velocity = velocity.WithX(-velocity.X);
                bounced = true;
            }

            if (snap.Y < 0 && velocity.Y < 0)
            {
                velocity = velocity.WithY(-velocity.Y);
                bounced = true;
            }
            else if (snap.Y > maxY && velocity.Y > 0)
            {
                velocity = velocity.WithY(-velocity.Y);
                bounced = true;
            }

            if (bounced)
            {
                ball.SetVelocity(velocity);
                return true;
            }

            return false;
        }

        public static bool ResolveBallCollision(IBallData a, IBallData b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));

            BallSnapshot sa = a.Snapshot;
            BallSnapshot sb = b.Snapshot;

            double ra = a.Diameter / 2.0;
            double rb = b.Diameter / 2.0;

            double cax = sa.X + ra;
            double cay = sa.Y + ra;
            double cbx = sb.X + rb;
            double cby = sb.Y + rb;

            double dx = cax - cbx;
            double dy = cay - cby;
            double dist2 = dx * dx + dy * dy;
            double minDist = ra + rb;

            if (dist2 >= minDist * minDist) return false;
            if (dist2 < 1e-9) return false;

            double dist = Math.Sqrt(dist2);
            double nx = dx / dist;
            double ny = dy / dist;

            double rvx = sa.Velocity.X - sb.Velocity.X;
            double rvy = sa.Velocity.Y - sb.Velocity.Y;
            double vRelN = rvx * nx + rvy * ny;

            double m1 = a.Mass;
            double m2 = b.Mass;
            double totalMass = m1 + m2;

            double overlap = minDist - dist;
            double sepA = overlap * (m2 / totalMass);
            double sepB = overlap * (m1 / totalMass);
            double newAX = sa.X + nx * sepA;
            double newAY = sa.Y + ny * sepA;
            double newBX = sb.X - nx * sepB;
            double newBY = sb.Y - ny * sepB;

            Vector2D newVA = sa.Velocity;
            Vector2D newVB = sb.Velocity;

            if (vRelN < 0)
            {
                double impulse = vRelN / totalMass;
                newVA = new Vector2D(
                    sa.Velocity.X - 2 * m2 * impulse * nx,
                    sa.Velocity.Y - 2 * m2 * impulse * ny);
                newVB = new Vector2D(
                    sb.Velocity.X + 2 * m1 * impulse * nx,
                    sb.Velocity.Y + 2 * m1 * impulse * ny);
            }

            a.ApplyChange(newAX, newAY, newVA);
            b.ApplyChange(newBX, newBY, newVB);
            return true;
        }
    }
}
