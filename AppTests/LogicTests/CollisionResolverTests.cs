using Data;
using Logic.Physics;

namespace LogicTests;

[TestClass]
public sealed class CollisionResolverTests
{
    [TestMethod]
    public void ResolveWalls_FreeFlight_DoesNotChangeState()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: 80, y: 80, velocity: new Vector2D(3, -4));

        bool changed = CollisionResolver.ResolveWalls(ball, 200, 200);

        Assert.IsFalse(changed);
        Assert.AreEqual(80.0, ball.X);
        Assert.AreEqual(80.0, ball.Y);
        Assert.AreEqual(3.0, ball.Velocity.X);
        Assert.AreEqual(-4.0, ball.Velocity.Y);
    }

    [TestMethod]
    public void ResolveWalls_BallPastRightWall_MovingOut_ReversesXVelocity()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: 185, y: 50, velocity: new Vector2D(2, 0));

        CollisionResolver.ResolveWalls(ball, 200, 200);

        Assert.IsLessThan(0, ball.Velocity.X);
    }

    [TestMethod]
    public void ResolveWalls_BallPastRightWall_AlreadyMovingBack_DoesNotFlipAgain()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: 185, y: 50, velocity: new Vector2D(-2, 0));

        bool flipped = CollisionResolver.ResolveWalls(ball, 200, 200);

        Assert.IsFalse(flipped);
        Assert.AreEqual(-2.0, ball.Velocity.X);
    }

    [TestMethod]
    public void ResolveWalls_BallPastLeftWall_MovingOut_ReversesXVelocity()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: -5, y: 50, velocity: new Vector2D(-2, 0));

        CollisionResolver.ResolveWalls(ball, 200, 200);

        Assert.IsGreaterThan(0, ball.Velocity.X);
    }

    [TestMethod]
    public void ResolveWalls_BallPastBottomWall_MovingOut_ReversesYVelocity()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: 50, y: 185, velocity: new Vector2D(0, 2));

        CollisionResolver.ResolveWalls(ball, 200, 200);

        Assert.IsLessThan(0, ball.Velocity.Y);
    }

    [TestMethod]
    public void ResolveWalls_BallPastTopWall_MovingOut_ReversesYVelocity()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: 50, y: -5, velocity: new Vector2D(0, -2));

        CollisionResolver.ResolveWalls(ball, 200, 200);

        Assert.IsGreaterThan(0, ball.Velocity.Y);
    }

    [TestMethod]
    public void ResolveWalls_CornerBounce_BothComponentsReverse()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: 185, y: 185, velocity: new Vector2D(2, 2));

        CollisionResolver.ResolveWalls(ball, 200, 200);

        Assert.IsLessThan(0, ball.Velocity.X);
        Assert.IsLessThan(0, ball.Velocity.Y);
    }

    [TestMethod]
    public void ResolveWalls_PreservesSpeedMagnitude()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: 185, y: 50, velocity: new Vector2D(2, 3));

        double speedBefore = ball.Velocity.Length;
        CollisionResolver.ResolveWalls(ball, 200, 200);
        double speedAfter = ball.Velocity.Length;

        Assert.AreEqual(speedBefore, speedAfter, 1e-9);
    }

    [TestMethod]
    public void ResolveWalls_DoesNotChangePosition()
    {
        var ball = new TestBall(diameter: 20, mass: 1, x: 185, y: 50, velocity: new Vector2D(2, 0));

        CollisionResolver.ResolveWalls(ball, 200, 200);

        Assert.AreEqual(185.0, ball.X);
        Assert.AreEqual(50.0, ball.Y);
    }

    [TestMethod]
    public void ResolveBallCollision_NotOverlapping_ReturnsFalse()
    {
        var a = new TestBall(diameter: 20, mass: 1, x: 0, y: 0, velocity: new Vector2D(1, 0));
        var b = new TestBall(diameter: 20, mass: 1, x: 100, y: 0, velocity: new Vector2D(-1, 0));

        bool collided = CollisionResolver.ResolveBallCollision(a, b);

        Assert.IsFalse(collided);
        Assert.AreEqual(1.0, a.Velocity.X);
        Assert.AreEqual(-1.0, b.Velocity.X);
    }

    [TestMethod]
    public void ResolveBallCollision_EqualMasses_HeadOn_SwapsVelocities()
    {
        var a = new TestBall(diameter: 20, mass: 1, x: 0, y: 0, velocity: new Vector2D(1, 0));
        var b = new TestBall(diameter: 20, mass: 1, x: 18, y: 0, velocity: new Vector2D(-1, 0));

        bool collided = CollisionResolver.ResolveBallCollision(a, b);

        Assert.IsTrue(collided);
        Assert.AreEqual(-1.0, a.Velocity.X, 1e-9);
        Assert.AreEqual(0.0, a.Velocity.Y, 1e-9);
        Assert.AreEqual(1.0, b.Velocity.X, 1e-9);
        Assert.AreEqual(0.0, b.Velocity.Y, 1e-9);
    }

    [TestMethod]
    public void ResolveBallCollision_DifferentMasses_FollowsClosedFormFormula()
    {
        var a = new TestBall(diameter: 20, mass: 1, x: 0, y: 0, velocity: new Vector2D(0, 0));
        var b = new TestBall(diameter: 20, mass: 2, x: 18, y: 0, velocity: new Vector2D(-1, 0));

        CollisionResolver.ResolveBallCollision(a, b);

        Assert.AreEqual(-4.0 / 3.0, a.Velocity.X, 1e-9);
        Assert.AreEqual(-1.0 / 3.0, b.Velocity.X, 1e-9);
    }

    [TestMethod]
    public void ResolveBallCollision_ConservesMomentum()
    {
        var a = new TestBall(diameter: 20, mass: 3, x: 0, y: 0, velocity: new Vector2D(2, 0));
        var b = new TestBall(diameter: 20, mass: 5, x: 18, y: 0, velocity: new Vector2D(-1, 0));

        double pBefore = 3 * 2 + 5 * (-1);
        CollisionResolver.ResolveBallCollision(a, b);
        double pAfter = 3 * a.Velocity.X + 5 * b.Velocity.X;

        Assert.AreEqual(pBefore, pAfter, 1e-9);
    }

    [TestMethod]
    public void ResolveBallCollision_ConservesKineticEnergy()
    {
        var a = new TestBall(diameter: 20, mass: 3, x: 0, y: 0, velocity: new Vector2D(2, 0));
        var b = new TestBall(diameter: 20, mass: 5, x: 18, y: 0, velocity: new Vector2D(-1, 0));

        double kBefore = 0.5 * 3 * (2 * 2) + 0.5 * 5 * (1 * 1);
        CollisionResolver.ResolveBallCollision(a, b);
        double kAfter = 0.5 * 3 * a.Velocity.LengthSquared() + 0.5 * 5 * b.Velocity.LengthSquared();

        Assert.AreEqual(kBefore, kAfter, 1e-9);
    }

    [TestMethod]
    public void ResolveBallCollision_OnlyAppliesImpulseWhenApproaching()
    {
        var a = new TestBall(diameter: 20, mass: 1, x: 0, y: 0, velocity: new Vector2D(-1, 0));
        var b = new TestBall(diameter: 20, mass: 1, x: 18, y: 0, velocity: new Vector2D(1, 0));

        CollisionResolver.ResolveBallCollision(a, b);

        Assert.AreEqual(-1.0, a.Velocity.X, 1e-9);
        Assert.AreEqual(1.0, b.Velocity.X, 1e-9);
    }

    [TestMethod]
    public void ResolveBallCollision_SeparatesOverlappingBalls()
    {
        var a = new TestBall(diameter: 20, mass: 1, x: 0, y: 0, velocity: new Vector2D(1, 0));
        var b = new TestBall(diameter: 20, mass: 1, x: 18, y: 0, velocity: new Vector2D(-1, 0));

        CollisionResolver.ResolveBallCollision(a, b);

        double cax = a.X + a.Diameter / 2.0;
        double cay = a.Y + a.Diameter / 2.0;
        double cbx = b.X + b.Diameter / 2.0;
        double cby = b.Y + b.Diameter / 2.0;
        double dx = cax - cbx;
        double dy = cay - cby;
        double dist = System.Math.Sqrt(dx * dx + dy * dy);

        Assert.AreEqual(20.0, dist, 1e-6);
    }

    [TestMethod]
    public void ResolveBallCollision_Diagonal_PreservesSpeedSumForEqualMassHeadOn()
    {
        var a = new TestBall(diameter: 20, mass: 1, x: 0, y: 0, velocity: new Vector2D(1, 1));
        var b = new TestBall(diameter: 20, mass: 1, x: 12, y: 12, velocity: new Vector2D(-1, -1));

        CollisionResolver.ResolveBallCollision(a, b);

        Assert.AreEqual(-1.0, a.Velocity.X, 1e-9);
        Assert.AreEqual(-1.0, a.Velocity.Y, 1e-9);
        Assert.AreEqual(1.0, b.Velocity.X, 1e-9);
        Assert.AreEqual(1.0, b.Velocity.Y, 1e-9);
    }

    private sealed class TestBall : IBallData
    {
        public TestBall(int diameter, double mass, double x, double y, Vector2D velocity)
        {
            Id = 0;
            Diameter = diameter;
            Mass = mass;
            X = x;
            Y = y;
            Velocity = velocity;
        }

        public int Id { get; }
        public int Diameter { get; }
        public double Mass { get; }
        public byte R => 0;
        public byte G => 0;
        public byte B => 0;
        public double X { get; private set; }
        public double Y { get; private set; }
        public Vector2D Velocity { get; private set; }
        public BallSnapshot Snapshot => new BallSnapshot(X, Y, Velocity);

        public void ApplyChange(double newX, double newY, Vector2D newVelocity)
        {
            X = newX;
            Y = newY;
            Velocity = newVelocity;
        }

        public void SetVelocity(Vector2D newVelocity)
        {
            Velocity = newVelocity;
        }
    }
}

internal static class Vector2DExtensions
{
    public static double LengthSquared(this Vector2D v) => v.X * v.X + v.Y * v.Y;
}
