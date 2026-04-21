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
    public void TimerLoop_UpdatesPositionsOverTime()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(2);
        Thread.Sleep(150);
        logic.Stop();

        Assert.IsGreaterThan(0, data.UpdateCalls);
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
    public void Resume_AfterStop_ContinuesUpdatingPositions()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        logic.Stop();
        int callsAfterStop = data.UpdateCalls;

        logic.Resume();
        Thread.Sleep(100);
        logic.Stop();

        Assert.IsGreaterThan(callsAfterStop, data.UpdateCalls);
    }

    [TestMethod]
    public void Resume_WithoutPriorStart_DoesNotRun()
    {
        var data = new FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);

        logic.Resume();

        Assert.IsFalse(logic.IsRunning);
        Assert.AreEqual(0, data.UpdateCalls);
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
    public void Dispose_StopsSimulation()
    {
        var data = new FakeBallData(500, 500);
        var logic = BallLogicApi.CreateApi(data);

        logic.Start(3);
        logic.Dispose();

        Assert.IsFalse(logic.IsRunning);
    }

    private sealed class FakeBallData : BallDataApi
    {
        private readonly List<IBallData> _balls = new();
        public FakeBallData(int w, int h) { BoardWidth = w; BoardHeight = h; }
        public override int BoardWidth { get; }
        public override int BoardHeight { get; }
        public override IReadOnlyList<IBallData> Balls => _balls;
        public int LastGeneratedCount { get; private set; }
        public int UpdateCalls { get; private set; }

        public override void GenerateBalls(int count)
        {
            LastGeneratedCount = count;
            _balls.Clear();
            for (int i = 0; i < count; i++)
                _balls.Add(new FakeBall { Id = i, Diameter = 10 });
        }

        public override void UpdatePositions() => UpdateCalls++;

        private sealed class FakeBall : IBallData
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
}