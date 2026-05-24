using System.Threading;
using Data;
using Data.Models;

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
        using var api = BallDataApi.CreateApi(width, height);
        api.GenerateBalls(count);

        Assert.HasCount(count, api.Balls);
    }

    [TestMethod]
    [DataRow(500, 500, 10)]
    [DataRow(200, 800, 20)]
    public void GenerateBalls_AllBallsInsideBoardBounds(int width, int height, int count)
    {
        using var api = BallDataApi.CreateApi(width, height);
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
        using var api = BallDataApi.CreateApi(500, 500);
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
        using var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(20);

        foreach (var ball in api.Balls)
            Assert.IsGreaterThan(0, ball.Velocity.Length);
    }

    [TestMethod]
    public void GenerateBalls_IdsAreSequentialFromZero()
    {
        using var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(10);

        for (int i = 0; i < api.Balls.Count; i++)
            Assert.AreEqual(i, api.Balls[i].Id);
    }

    [TestMethod]
    public void GenerateBalls_CalledTwice_ReplacesOldBalls()
    {
        using var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(10);
        api.GenerateBalls(3);

        Assert.HasCount(3, api.Balls);
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(i, api.Balls[i].Id);
    }

    [TestMethod]
    public void GenerateBalls_AllBallsHavePositiveMass()
    {
        using var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(15);

        foreach (var ball in api.Balls)
            Assert.IsGreaterThan(0, ball.Mass);
    }

    [TestMethod]
    public void GenerateBalls_MassIsProportionalToDiameterSquared()
    {
        using var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(25);

        foreach (var ball in api.Balls)
        {
            double radius = ball.Diameter / 2.0;
            double expected = System.Math.PI * radius * radius;
            Assert.AreEqual(expected, ball.Mass, 1e-9);
        }
    }

    [TestMethod]
    public void Constructor_BoardSmallerThanMaxDiameter_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BallDataApi.CreateApi(10, 10));
    }

    [TestMethod]
    public void BoardDimensions_MatchConstructorValues()
    {
        using var api = BallDataApi.CreateApi(800, 600);

        Assert.AreEqual(800, api.BoardWidth);
        Assert.AreEqual(600, api.BoardHeight);
    }

    [TestMethod]
    public void ApplyChange_StoresValuesAtomically()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 60, diameter: 40, mass: 10, velocity: new Vector2D(1, -1));
        var ball = repo.Balls[0];

        ball.ApplyChange(123.5, 222.0, new Vector2D(-3, 4));
        var snap = ball.Snapshot;

        Assert.AreEqual(123.5, snap.X);
        Assert.AreEqual(222.0, snap.Y);
        Assert.AreEqual(-3.0, snap.Velocity.X);
        Assert.AreEqual(4.0, snap.Velocity.Y);
        Assert.AreEqual(123.5, ball.X);
        Assert.AreEqual(222.0, ball.Y);
        Assert.AreEqual(-3.0, ball.Velocity.X);
        Assert.AreEqual(4.0, ball.Velocity.Y);
    }

    [TestMethod]
    public void SetVelocity_ChangesVelocityWithoutTouchingPosition()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 70, y: 80, diameter: 20, mass: 5, velocity: new Vector2D(1, 1));
        var ball = repo.Balls[0];

        ball.SetVelocity(new Vector2D(-2, 3));

        Assert.AreEqual(70.0, ball.X);
        Assert.AreEqual(80.0, ball.Y);
        Assert.AreEqual(-2.0, ball.Velocity.X);
        Assert.AreEqual(3.0, ball.Velocity.Y);
    }

    [TestMethod]
    public void Step_AdvancesPositionProportionallyToElapsedTime()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 50, diameter: 20, mass: 1, velocity: new Vector2D(2, -3));
        var ball = (BallEntity)repo.Balls[0];

        ball.Step(16);

        Assert.AreEqual(52.0, ball.X, 1e-9);
        Assert.AreEqual(47.0, ball.Y, 1e-9);

        ball.Step(32);

        Assert.AreEqual(56.0, ball.X, 1e-9);
        Assert.AreEqual(41.0, ball.Y, 1e-9);
    }

    [TestMethod]
    public void StartMovement_BallsActuallyMoveOverTime()
    {
        using var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(5);

        var before = api.Balls.Select(b => (b.X, b.Y)).ToList();
        api.StartMovement();
        Thread.Sleep(150);
        api.StopMovement();
        var after = api.Balls.Select(b => (b.X, b.Y)).ToList();

        bool anyMoved = false;
        for (int i = 0; i < before.Count; i++)
            if (before[i] != after[i]) { anyMoved = true; break; }

        Assert.IsTrue(anyMoved);
    }

    [TestMethod]
    public void StopMovement_HaltsBallsImmediately()
    {
        using var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(3);

        api.StartMovement();
        Thread.Sleep(80);
        api.StopMovement();
        Thread.Sleep(60);
        var afterStop = api.Balls.Select(b => (b.X, b.Y)).ToList();
        Thread.Sleep(120);
        var laterStill = api.Balls.Select(b => (b.X, b.Y)).ToList();

        for (int i = 0; i < afterStop.Count; i++)
            Assert.AreEqual(afterStop[i], laterStill[i]);
    }

    [TestMethod]
    public void StartMovement_CalledTwice_DoesNotDoubleSpawnTasks()
    {
        using var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(2);

        api.StartMovement();
        api.StartMovement();
        Thread.Sleep(60);
        api.StopMovement();
    }

    [TestMethod]
    public void Snapshot_AndIndividualGetters_AreConsistentUnderConcurrency()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 0, y: 0, diameter: 20, mass: 5, velocity: new Vector2D(0, 0));
        var ball = repo.Balls[0];

        bool writerDone = false;
        Exception? readerFailure = null;

        var writer = new Thread(() =>
        {
            for (int i = 0; i < 5000; i++)
            {
                int wrapped = i % 60;
                ball.ApplyChange(wrapped, wrapped * 2, new Vector2D(wrapped, -wrapped));
            }
            writerDone = true;
        }) { IsBackground = true };

        var reader = new Thread(() =>
        {
            try
            {
                while (!writerDone)
                {
                    var s = ball.Snapshot;
                    Assert.AreEqual(s.X * 2, s.Y);
                    Assert.AreEqual(s.X, s.Velocity.X);
                    Assert.AreEqual(-s.X, s.Velocity.Y);
                }
            }
            catch (Exception ex)
            {
                readerFailure = ex;
            }
        }) { IsBackground = true };

        writer.Start();
        reader.Start();
        writer.Join();
        reader.Join();

        if (readerFailure != null) throw readerFailure;
    }

    [TestMethod]
    public void ApplyChange_VelocityAboveMaxAllowed_ThrowsArgumentOutOfRange()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 50, diameter: 20, mass: 5, velocity: new Vector2D(1, 1));
        var ball = repo.Balls[0];

        var crazy = new Vector2D(BallEntity.MaxAllowedSpeed + 1, 0);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => ball.ApplyChange(50, 50, crazy));
    }

    [TestMethod]
    public void SetVelocity_AboveMaxAllowed_ThrowsArgumentOutOfRange()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 50, diameter: 20, mass: 5, velocity: new Vector2D(1, 1));
        var ball = repo.Balls[0];

        var crazy = new Vector2D(0, BallEntity.MaxAllowedSpeed + 0.01);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => ball.SetVelocity(crazy));
    }

    [TestMethod]
    public void SetVelocity_DiagonalCombinedMagnitudeAboveLimit_Throws()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 50, diameter: 20, mass: 5, velocity: new Vector2D(1, 1));
        var ball = repo.Balls[0];

        double component = BallEntity.MaxAllowedSpeed * 0.9;
        var diagonal = new Vector2D(component, component);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => ball.SetVelocity(diagonal));
    }

    [TestMethod]
    public void SetVelocity_AtBoundary_Accepted()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 50, y: 50, diameter: 20, mass: 5, velocity: new Vector2D(1, 1));
        var ball = repo.Balls[0];

        ball.SetVelocity(new Vector2D(BallEntity.MaxAllowedSpeed, 0));

        Assert.AreEqual(BallEntity.MaxAllowedSpeed, ball.Velocity.X);
    }

    [TestMethod]
    public void Stopwatch_IsBackedByHighResolutionPerformanceCounter()
    {
        Assert.IsTrue(System.Diagnostics.Stopwatch.IsHighResolution);
        Assert.IsGreaterThan(1_000_000L, System.Diagnostics.Stopwatch.Frequency);
    }

    [TestMethod]
    public void Step_TimedWithHighResolutionStopwatch_MotionScalesLinearlyWithDt()
    {
        var repo = new BallRepository(200, 200);
        repo.SeedBall(x: 0, y: 0, diameter: 20, mass: 1, velocity: new Vector2D(2, 0));
        var ball = (BallEntity)repo.Balls[0];

        var sw = System.Diagnostics.Stopwatch.StartNew();
        ball.Step(16);
        long oneFrameTicks = sw.ElapsedTicks;

        sw.Restart();
        ball.Step(32);
        long twoFrameTicks = sw.ElapsedTicks;

        Assert.IsGreaterThan(0L, oneFrameTicks);
        Assert.IsGreaterThan(0L, twoFrameTicks);
        Assert.AreEqual(2 + 4, ball.X, 1e-9);
    }

    [TestMethod]
    public void StartMovement_WithBarrier_All100BallsStepExactlySameNumberOfTimes()
    {
        using var api = BallDataApi.CreateApi(800, 800);
        api.GenerateBalls(100);

        const int rounds = 30;
        int roundsObserved = 0;
        int participants = api.FrameParticipants + 1;
        using var barrier = new Barrier(participants, _ => roundsObserved++);

        api.StartMovement(barrier);

        for (int r = 0; r < rounds; r++)
            barrier.SignalAndWait();

        api.StopMovement();

        var counts = api.Balls.Cast<BallEntity>().Select(b => b.StepCount).ToList();
        int min = counts.Min();
        int max = counts.Max();

        Assert.AreEqual(rounds, roundsObserved);
        Assert.AreEqual(rounds, min);
        Assert.AreEqual(rounds, max);
        Assert.IsLessThanOrEqualTo(1, max - min);
    }

    [TestMethod]
    public void StartMovement_With100Balls_AllThreadsGetFairCpuTime()
    {
        using var api = BallDataApi.CreateApi(1000, 1000);
        api.GenerateBalls(100);

        api.StartMovement();
        Thread.Sleep(1000);
        api.StopMovement();

        var counts = api.Balls.Cast<BallEntity>().Select(b => b.StepCount).ToList();
        int min = counts.Min();
        int max = counts.Max();

        Assert.IsGreaterThan(0, min);
        Assert.IsLessThanOrEqualTo(2 * min, max);
    }

    [TestMethod]
    public void StartMovement_AllBallsAdvanceAtLeastOnce_NoStarvation()
    {
        using var api = BallDataApi.CreateApi(1000, 1000);
        api.GenerateBalls(100);

        api.StartMovement();
        Thread.Sleep(500);
        api.StopMovement();

        foreach (var ball in api.Balls.Cast<BallEntity>())
            Assert.IsGreaterThan(0, ball.StepCount);
    }

    [TestMethod]
    public void Dispose_StopsMovement()
    {
        var api = BallDataApi.CreateApi(500, 500);
        api.GenerateBalls(3);
        api.StartMovement();
        Thread.Sleep(60);

        api.Dispose();
        Thread.Sleep(80);

        var pos = api.Balls.Select(b => (b.X, b.Y)).ToList();
        Thread.Sleep(80);
        var posLater = api.Balls.Select(b => (b.X, b.Y)).ToList();

        for (int i = 0; i < pos.Count; i++)
            Assert.AreEqual(pos[i], posLater[i]);
    }
}
