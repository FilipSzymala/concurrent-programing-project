using System.Threading;
using Data;
using Logic;

namespace LogicTests;

[TestClass]
public sealed class BallsServiceTests
{
    [TestMethod]
    public void BoardDimensions_ForwardedFromDataLayer()
    {
        var data = new FakeBallData(800, 600);
        using var logic = BallLogicApi.CreateApi(data);

        Assert.AreEqual(800, logic.BoardWidth);
        Assert.AreEqual(600, logic.BoardHeight);
    }

    [TestMethod]
    public void Start_GeneratesRequestedNumberOfBalls()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(7);

        Assert.AreEqual(7, data.LastGeneratedCount);
    }

    [TestMethod]
    public void Start_RaisesBallsChangedWithCorrectCount()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        int receivedCount = -1;
        logic.BallsChanged += (_, snapshot) =>
        {
            if (receivedCount < 0) receivedCount = snapshot.Count;
        };

        logic.Start(7);

        Assert.AreEqual(7, receivedCount);
    }

    [TestMethod]
    public void Start_SetsIsRunningTrue()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);

        Assert.IsTrue(logic.IsRunning);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    public void Start_BelowMinimum_Throws(int count)
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => logic.Start(count));
    }

    [TestMethod]
    public void Start_ExactMinimum_Succeeds()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(BallLogicApi.MinBallsCount);

        Assert.AreEqual(BallLogicApi.MinBallsCount, data.LastGeneratedCount);
        Assert.IsTrue(logic.IsRunning);
    }

    [TestMethod]
    public void Stop_SetsIsRunningFalse()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        logic.Stop();

        Assert.IsFalse(logic.IsRunning);
    }

    [TestMethod]
    public void PhysicsLoop_RaisesBallsChangedRepeatedly()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        int events = 0;
        logic.BallsChanged += (_, _) => Interlocked.Increment(ref events);

        logic.Start(2);
        Thread.Sleep(200);
        logic.Stop();

        Assert.IsGreaterThan(1, events);
    }

    [TestMethod]
    public void Resume_AfterStop_RestoresIsRunning()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        logic.Stop();
        Assert.IsFalse(logic.IsRunning);

        logic.Resume();
        Assert.IsTrue(logic.IsRunning);
    }

    [TestMethod]
    public void Resume_AfterStop_ContinuesRaisingEvents()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        logic.Stop();

        int eventsAfterResume = 0;
        logic.BallsChanged += (_, _) => Interlocked.Increment(ref eventsAfterResume);
        logic.Resume();
        Thread.Sleep(150);
        logic.Stop();

        Assert.IsGreaterThan(0, eventsAfterResume);
    }

    [TestMethod]
    public void Resume_WithoutPriorStart_DoesNotRun()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Resume();

        Assert.IsFalse(logic.IsRunning);
    }

    [TestMethod]
    public void Toggle_FromStoppedToRunning_StartsLoop()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        logic.Stop();
        Assert.IsFalse(logic.IsRunning);

        logic.Toggle();
        Assert.IsTrue(logic.IsRunning);
        logic.Stop();
    }

    [TestMethod]
    public void Toggle_FromRunningToStopped_HaltsLoop()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        Assert.IsTrue(logic.IsRunning);

        logic.Toggle();
        Assert.IsFalse(logic.IsRunning);
    }

    [TestMethod]
    public void Toggle_WithoutBalls_DoesNotStart()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Toggle();

        Assert.IsFalse(logic.IsRunning);
    }

    [TestMethod]
    public void BallsChanged_SnapshotContainsAllBallIds()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        IReadOnlyList<IBallStatus>? snapshot = null;
        logic.BallsChanged += (_, s) => snapshot ??= s;

        logic.Start(5);

        Assert.IsNotNull(snapshot);
        var ids = snapshot!.Select(b => b.Id).OrderBy(id => id).ToList();
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, ids);
    }

    [TestMethod]
    public void BallsChanged_SnapshotForwardsMass()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        IReadOnlyList<IBallStatus>? snapshot = null;
        logic.BallsChanged += (_, s) => snapshot ??= s;

        logic.Start(3);

        Assert.IsNotNull(snapshot);
        foreach (var b in snapshot!)
            Assert.IsGreaterThan(0, b.Mass);
    }

    [TestMethod]
    public void PhysicsLoop_RecordsTickDurationViaHighResolutionStopwatch()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        Thread.Sleep(200);
        double tick = logic.LastTickDurationMs;
        logic.Stop();

        Assert.IsTrue(System.Diagnostics.Stopwatch.IsHighResolution);
        Assert.IsGreaterThan(0.0, tick);
        Assert.AreEqual(16, logic.PhysicsBudgetMs);
    }

    [TestMethod]
    public void Dispose_StopsSimulation()
    {
        var data = new FakeBallData(500, 500);
        var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        logic.Dispose();

        Assert.IsFalse(logic.IsRunning);
    }

    internal sealed class FakeBallData : BallDataApi
    {
        private readonly List<IBallData> _balls = new();
        public FakeBallData(int w, int h) { BoardWidth = w; BoardHeight = h; }
        public override int BoardWidth { get; }
        public override int BoardHeight { get; }
        public override IReadOnlyList<IBallData> Balls => _balls;
        public override int FrameParticipants => 0;
        public int LastGeneratedCount { get; private set; }
        public bool MovementStarted { get; private set; }
        public bool MovementStopped { get; private set; }

        public override void GenerateBalls(int count)
        {
            LastGeneratedCount = count;
            _balls.Clear();
            for (int i = 0; i < count; i++)
                _balls.Add(new FakeBall(i, 10, 2.0));
        }

        public override void StartMovement(System.Threading.Barrier frameBarrier = null) => MovementStarted = true;
        public override void StopMovement() => MovementStopped = true;
        public override void Dispose() { }
    }

    internal sealed class FakeBall : IBallData
    {
        private readonly object _sync = new();
        private double _x;
        private double _y;
        private Vector2D _velocity;

        public FakeBall(int id, int diameter, double mass)
        {
            Id = id;
            Diameter = diameter;
            Mass = mass;
            _velocity = new Vector2D(1, 1);
        }

        public int Id { get; }
        public int Diameter { get; }
        public double Mass { get; }
        public byte R => 0;
        public byte G => 0;
        public byte B => 0;

        public double X { get { lock (_sync) { return _x; } } }
        public double Y { get { lock (_sync) { return _y; } } }
        public Vector2D Velocity { get { lock (_sync) { return _velocity; } } }
        public BallSnapshot Snapshot
        {
            get { lock (_sync) { return new BallSnapshot(_x, _y, _velocity); } }
        }

        public void ApplyChange(double newX, double newY, Vector2D newVelocity)
        {
            lock (_sync) { _x = newX; _y = newY; _velocity = newVelocity; }
        }

        public void SetVelocity(Vector2D newVelocity)
        {
            lock (_sync) { _velocity = newVelocity; }
        }
    }
}
