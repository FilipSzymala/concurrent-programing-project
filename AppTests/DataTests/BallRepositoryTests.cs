using Data;

namespace DataTests;

[TestClass]
public class BallRepositoryTests
{
    [TestMethod]
    [DataRow(800, 600, 15)]
    [DataRow(100, 100, 5)]
    [DataRow(1920, 1080, 50)]
    public void GenerateBalls_ProducesExactlyRequestedCount(int width, int height, int count)
    {
        var api = BallDataApi.CreateApi(width, height);
        api.GenerateBalls(count);

        Assert.HasCount(count, api.Balls);
    }

    [TestMethod]
    [DataRow(500, 500, 10)]
    [DataRow(200, 800, 20)]
    public void GenerateBalls_AllBallsInsideBoardBounds(int width, int height, int count)
    {
        var api = BallDataApi.CreateApi(width, height);
        api.GenerateBalls(count);

        foreach (var ball in api.Balls)
        {
            Assert.IsGreaterThanOrEqualTo(0, ball.X);
            Assert.IsLessThanOrEqualTo(width, ball.X + ball.Diameter);
            Assert.IsGreaterThanOrEqualTo(0, ball.Y);
            Assert.IsLessThanOrEqualTo(height, ball.Y + ball.Diameter);
        }
    }

    [TestMethod]
    public void GenerateBalls_DiametersWithinAllowedRange()
    {
        var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(30);

        foreach (var ball in api.Balls)
        {
            Assert.IsGreaterThanOrEqualTo(35, ball.Diameter);
            Assert.IsLessThanOrEqualTo(70, ball.Diameter);
        }
    }

    [TestMethod]
    public void GenerateBalls_AllBallsHaveNonZeroVelocity()
    {
        var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(20);

        foreach (var ball in api.Balls)
            Assert.IsGreaterThan(0, ball.Velocity.Length);
    }

    [TestMethod]
    public void GenerateBalls_IdsAreSequentialFromZero()
    {
        var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(10);

        for (int i = 0; i < api.Balls.Count; i++)
            Assert.AreEqual(i, api.Balls[i].Id);
    }

    [TestMethod]
    public void GenerateBalls_CalledTwice_ReplacesOldBalls()
    {
        var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(10);
        api.GenerateBalls(3);

        Assert.HasCount(3, api.Balls);
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(i, api.Balls[i].Id);
    }

    [TestMethod]
    public void Constructor_BoardSmallerThanMaxDiameter_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BallDataApi.CreateApi(10, 10));
    }

    [TestMethod]
    public void BoardDimensions_MatchConstructorValues()
    {
        var api = BallDataApi.CreateApi(800, 600);

        Assert.AreEqual(800, api.BoardWidth);
        Assert.AreEqual(600, api.BoardHeight);
    }

    [TestMethod]
    public void UpdatePositions_FreeFlight_MovesAccordingToVelocity()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 80, y: 80, diameter: 20, velocity: new Vector2D(3, -4));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.AreEqual(83.0, ball.X);
        Assert.AreEqual(76.0, ball.Y);
    }

    [TestMethod]
    public void UpdatePositions_FreeFlight_VelocityUnchanged()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 80, y: 80, diameter: 20, velocity: new Vector2D(3, -4));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.AreEqual(3.0, ball.Velocity.X);
        Assert.AreEqual(-4.0, ball.Velocity.Y);
    }

    [TestMethod]
    public void UpdatePositions_BounceOffRightWall_ReversesXVelocity()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 170, y: 50, diameter: 20, velocity: new Vector2D(15, 0));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.IsLessThan(0, ball.Velocity.X);
        Assert.IsLessThanOrEqualTo(200, ball.X + ball.Diameter);
    }

    [TestMethod]
    public void UpdatePositions_BounceOffLeftWall_ReversesXVelocity()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 5, y: 50, diameter: 20, velocity: new Vector2D(-12, 0));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.IsGreaterThan(0, ball.Velocity.X);
        Assert.IsGreaterThanOrEqualTo(0, ball.X);
    }

    [TestMethod]
    public void UpdatePositions_BounceOffBottomWall_ReversesYVelocity()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 175, diameter: 20, velocity: new Vector2D(0, 15));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.IsLessThan(0, ball.Velocity.Y);
        Assert.IsLessThanOrEqualTo(200, ball.Y + ball.Diameter);
    }

    [TestMethod]
    public void UpdatePositions_BounceOffTopWall_ReversesYVelocity()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 5, diameter: 20, velocity: new Vector2D(0, -12));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.IsGreaterThan(0, ball.Velocity.Y);
        Assert.IsGreaterThanOrEqualTo(0, ball.Y);
    }

    [TestMethod]
    public void UpdatePositions_CornerBounce_BothVelocityComponentsReverse()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 175, y: 175, diameter: 20, velocity: new Vector2D(10, 10));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.IsLessThan(0, ball.Velocity.X);
        Assert.IsLessThan(0, ball.Velocity.Y);
        Assert.IsGreaterThanOrEqualTo(0, ball.X);
        Assert.IsLessThanOrEqualTo(200, ball.X + ball.Diameter);
        Assert.IsGreaterThanOrEqualTo(0, ball.Y);
        Assert.IsLessThanOrEqualTo(200, ball.Y + ball.Diameter);
    }

    [TestMethod]
    public void UpdatePositions_ZeroVelocity_BallStaysInPlace()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 100, y: 100, diameter: 20, velocity: new Vector2D(0, 0));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.AreEqual(100.0, ball.X);
        Assert.AreEqual(100.0, ball.Y);
    }

    [TestMethod]
    public void UpdatePositions_HugeVelocity_BallStillInsideBoard()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 100, y: 100, diameter: 20, velocity: new Vector2D(9999, 9999));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.IsGreaterThanOrEqualTo(0, ball.X);
        Assert.IsLessThanOrEqualTo(200, ball.X + ball.Diameter);
        Assert.IsGreaterThanOrEqualTo(0, ball.Y);
        Assert.IsLessThanOrEqualTo(200, ball.Y + ball.Diameter);
    }

    [TestMethod]
    public void UpdatePositions_1000Ticks_BallNeverLeavesBoard()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 170, diameter: 40, velocity: new Vector2D(1.7, 1.9));

        for (int tick = 0; tick < 1000; tick++)
        {
            repo.UpdatePositions();
            var ball = repo.Balls[0];
            Assert.IsGreaterThanOrEqualTo(0, ball.X);
            Assert.IsLessThanOrEqualTo(200, ball.X + ball.Diameter);
            Assert.IsGreaterThanOrEqualTo(0, ball.Y);
            Assert.IsLessThanOrEqualTo(200, ball.Y + ball.Diameter);
        }
    }

    [TestMethod]
    public void UpdatePositions_MultipleBalls_AllStayInsideAfterManyTicks()
    {
        var api = BallDataApi.CreateApi(300, 300);
        api.GenerateBalls(15);

        for (int tick = 0; tick < 500; tick++)
        {
            api.UpdatePositions();
            foreach (var ball in api.Balls)
            {
                Assert.IsGreaterThanOrEqualTo(0, ball.X);
                Assert.IsLessThanOrEqualTo(300, ball.X + ball.Diameter);
                Assert.IsGreaterThanOrEqualTo(0, ball.Y);
                Assert.IsLessThanOrEqualTo(300, ball.Y + ball.Diameter);
            }
        }
    }

    [TestMethod]
    public void UpdatePositions_BallOnExactEdge_DoesNotEscape()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 180, y: 180, diameter: 20, velocity: new Vector2D(0, 0));

        repo.UpdatePositions();

        var ball = repo.Balls[0];
        Assert.AreEqual(180.0, ball.X);
        Assert.AreEqual(180.0, ball.Y);
    }

    [TestMethod]
    public void UpdatePositions_ActuallyMovesBalls()
    {
        var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(5);

        var before = api.Balls.Select(b => (b.X, b.Y)).ToList();
        api.UpdatePositions();
        var after = api.Balls.Select(b => (b.X, b.Y)).ToList();

        bool anyMoved = false;
        for (int i = 0; i < before.Count; i++)
            if (before[i] != after[i]) { anyMoved = true; break; }

        Assert.IsTrue(anyMoved);
    }

    [TestMethod]
    public void UpdatePositions_BouncePreservesSpeedMagnitude()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 170, y: 50, diameter: 20, velocity: new Vector2D(15, 3));

        double speedBefore = repo.Balls[0].Velocity.Length;
        repo.UpdatePositions();
        double speedAfter = repo.Balls[0].Velocity.Length;

        Assert.AreEqual(speedBefore, speedAfter);
    }
}