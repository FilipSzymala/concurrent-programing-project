using Presentation.Models;
using Presentation.ViewModels;
using Logic;

namespace PresentationTest;

[TestClass]
public sealed class BoardViewModelTests
{
    [TestMethod]
    public void ResolveBallsCount_EmptyInput_ReturnsDefault()
    {
        int result = BoardViewModel.ResolveBallsCount("", out _);

        Assert.AreEqual(BallLogicApi.DefaultBallsCount, result);
    }

    [TestMethod]
    public void ResolveBallsCount_NonNumericInput_ReturnsDefault()
    {
        int result = BoardViewModel.ResolveBallsCount("abc", out _);

        Assert.AreEqual(BallLogicApi.DefaultBallsCount, result);
    }

    [TestMethod]
    public void ResolveBallsCount_BelowMinimum_ClampsToMinimum()
    {
        int result = BoardViewModel.ResolveBallsCount("0", out _);

        Assert.AreEqual(BallLogicApi.MinBallsCount, result);
    }

    [TestMethod]
    public void ResolveBallsCount_ValidNumber_ReturnsParsedValue()
    {
        int result = BoardViewModel.ResolveBallsCount("15", out _);

        Assert.AreEqual(15, result);
    }

    [TestMethod]
    public void ResolveBallsCount_ExactMinimum_Accepted()
    {
        int result = BoardViewModel.ResolveBallsCount(
            BallLogicApi.MinBallsCount.ToString(), out _);

        Assert.AreEqual(BallLogicApi.MinBallsCount, result);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_MapsPositionCorrectly()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 123.5, y: 456.7, diameter: 50, mass: 5, r: 0, g: 0, b: 0), 1.0);

        Assert.AreEqual(123.5, item.X);
        Assert.AreEqual(456.7, item.Y);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_MapsDiameterCorrectly()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 0, y: 0, diameter: 42, mass: 1, r: 0, g: 0, b: 0), 1.0);

        Assert.AreEqual(42, item.Diameter);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_MapsMassCorrectly()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 0, y: 0, diameter: 10, mass: 123.45, r: 0, g: 0, b: 0), 1.0);

        Assert.AreEqual(123.45, item.Mass);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_MapsColorFromRgb()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 0, y: 0, diameter: 10, mass: 1, r: 100, g: 150, b: 200), 1.0);

        Assert.AreEqual(100, item.Color.R);
        Assert.AreEqual(150, item.Color.G);
        Assert.AreEqual(200, item.Color.B);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_RaisesPropertyChangedForPosition()
    {
        var item = new BallListItem();
        var changedProps = new List<string>();
        item.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 10, y: 20, diameter: 30, mass: 1, r: 0, g: 0, b: 0), 1.0);

        CollectionAssert.Contains(changedProps, nameof(BallListItem.X));
        CollectionAssert.Contains(changedProps, nameof(BallListItem.Y));
        CollectionAssert.Contains(changedProps, nameof(BallListItem.Diameter));
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_SameValues_DoesNotRaisePropertyChanged()
    {
        var item = new BallListItem();
        var status = new FakeBallStatus(id: 0, x: 10, y: 20, diameter: 30, mass: 1, r: 50, g: 60, b: 70);
        item.UpdateFrom(status, 1.0);

        var changedProps = new List<string>();
        item.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

        item.UpdateFrom(status, 1.0);

        CollectionAssert.AreEqual(Array.Empty<string>(), changedProps);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_PositionChanges_RaisesPropertyChanged()
    {
        var item = new BallListItem();
        item.UpdateFrom(new FakeBallStatus(id: 0, x: 10, y: 20, diameter: 30, mass: 1, r: 0, g: 0, b: 0), 1.0);

        var changedProps = new List<string>();
        item.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 15, y: 25, diameter: 30, mass: 1, r: 0, g: 0, b: 0), 1.0);

        CollectionAssert.Contains(changedProps, nameof(BallListItem.X));
        CollectionAssert.Contains(changedProps, nameof(BallListItem.Y));
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_ScalesPositionAndDiameter()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 100, y: 200, diameter: 50, mass: 1, r: 0, g: 0, b: 0), 2.0);

        Assert.AreEqual(200.0, item.X);
        Assert.AreEqual(400.0, item.Y);
        Assert.AreEqual(100.0, item.Diameter);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_ScaleOne_PreservesOriginalValues()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 55.5, y: 77.3, diameter: 40, mass: 1, r: 0, g: 0, b: 0), 1.0);

        Assert.AreEqual(55.5, item.X);
        Assert.AreEqual(77.3, item.Y);
        Assert.AreEqual(40.0, item.Diameter);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_FractionalScale_ComputesCorrectly()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 300, y: 300, diameter: 60, mass: 1, r: 0, g: 0, b: 0), 0.5);

        Assert.AreEqual(150.0, item.X);
        Assert.AreEqual(150.0, item.Y);
        Assert.AreEqual(30.0, item.Diameter);
    }

    private static BoardViewModel CreateVm(FakeBallLogic logic) =>
        new BoardViewModel(logic, action => action());

    [TestMethod]
    public void BoardViewModel_OnBallsChanged_AddsItemsAndUpdatesAverageSpeed()
    {
        var fakeLogic = new FakeBallLogic(boardWidth: 200, boardHeight: 200);
        var vm = CreateVm(fakeLogic);
        vm.SetAvailableSize(200, 200);

        var snapshot = new List<IBallStatus>
        {
            new FakeBallStatus(0, 10, 10, 20, 5, 0, 0, 0) { VelocityX = 3, VelocityY = 4 },
            new FakeBallStatus(1, 50, 50, 20, 5, 0, 0, 0) { VelocityX = 0, VelocityY = 5 },
        };

        fakeLogic.Raise(snapshot);

        Assert.AreEqual(2, vm.BallsCount);
        Assert.AreEqual((5.0 + 5.0) / 2.0, vm.AverageSpeed, 1e-6);
    }

    [TestMethod]
    public void BoardViewModel_OnBallsChanged_ReusesExistingBallListItemsAcrossEvents()
    {
        var fakeLogic = new FakeBallLogic(200, 200);
        var vm = CreateVm(fakeLogic);

        var first = new List<IBallStatus>
        {
            new FakeBallStatus(0, 10, 10, 20, 5, 0, 0, 0) { VelocityX = 1, VelocityY = 1 },
        };
        fakeLogic.Raise(first);
        Assert.AreEqual(1, vm.BallsCount);

        var second = new List<IBallStatus>
        {
            new FakeBallStatus(0, 20, 20, 20, 5, 0, 0, 0) { VelocityX = 1, VelocityY = 1 },
        };
        fakeLogic.Raise(second);
        Assert.AreEqual(1, vm.BallsCount);
    }

    [TestMethod]
    public async Task BoardViewModel_Start_DelegatesToLogic()
    {
        var fakeLogic = new FakeBallLogic(200, 200);
        var vm = CreateVm(fakeLogic);

        vm.BallsCountText = "4";
        await vm.StartCommand.ExecuteAsync(null);

        Assert.AreEqual(1, fakeLogic.StartCalls);
        Assert.AreEqual(4, fakeLogic.LastStartCount);
    }

    [TestMethod]
    public async Task BoardViewModel_Start_BelowMinimum_ForwardsClampedValue()
    {
        var fakeLogic = new FakeBallLogic(200, 200);
        var vm = CreateVm(fakeLogic);

        vm.BallsCountText = "1";
        await vm.StartCommand.ExecuteAsync(null);

        Assert.AreEqual(BallLogicApi.MinBallsCount, fakeLogic.LastStartCount);
    }

    [TestMethod]
    public async Task BoardViewModel_Stop_DelegatesToLogic()
    {
        var fakeLogic = new FakeBallLogic(200, 200);
        var vm = CreateVm(fakeLogic);

        await vm.StopCommand.ExecuteAsync(null);

        Assert.AreEqual(1, fakeLogic.StopCalls);
    }

    [TestMethod]
    public async Task BoardViewModel_Toggle_DelegatesToLogic()
    {
        var fakeLogic = new FakeBallLogic(200, 200);
        var vm = CreateVm(fakeLogic);

        vm.ToggleMoveSimulationCommand.NotifyCanExecuteChanged();
        var snapshot = new List<IBallStatus>
        {
            new FakeBallStatus(0, 0, 0, 10, 1, 0, 0, 0) { VelocityX = 1, VelocityY = 0 },
        };
        fakeLogic.Raise(snapshot);

        await vm.ToggleMoveSimulationCommand.ExecuteAsync(null);

        Assert.AreEqual(1, fakeLogic.ToggleCalls);
    }

    internal sealed class FakeBallLogic : BallLogicApi
    {
        public FakeBallLogic(int boardWidth, int boardHeight)
        {
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
        }

        public override int BoardWidth { get; }
        public override int BoardHeight { get; }
        public override bool IsRunning { get; }
        public override double LastTickDurationMs => 0;
        public override double LastFrameIntervalMs => 16;
        public override double PhysicsBudgetMs => 16;
        public override event EventHandler<IReadOnlyList<IBallStatus>>? BallsChanged;

        public int StartCalls { get; private set; }
        public int LastStartCount { get; private set; }
        public int StopCalls { get; private set; }
        public int ResumeCalls { get; private set; }
        public int ToggleCalls { get; private set; }

        public override void Start(int ballsCount)
        {
            StartCalls++;
            LastStartCount = ballsCount;
        }

        public override void Stop() => StopCalls++;
        public override void Resume() => ResumeCalls++;
        public override void DragBall(int ballId, double x, double y) { }
        public override void StopDragging(int ballId) { }
        public override void Toggle() => ToggleCalls++;
        public override void Dispose() { }

        public void Raise(IReadOnlyList<IBallStatus> snapshot) =>
            BallsChanged?.Invoke(this, snapshot);
    }

    private sealed class FakeBallStatus : IBallStatus
    {
        public FakeBallStatus(int id, double x, double y, int diameter, double mass, byte r, byte g, byte b)
        {
            Id = id; X = x; Y = y; Diameter = diameter; Mass = mass; R = r; G = g; B = b;
        }
        public int Id { get; }
        public double X { get; }
        public double Y { get; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public int Diameter { get; }
        public double Mass { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
    }
}
