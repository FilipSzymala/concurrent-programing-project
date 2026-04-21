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

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 123.5, y: 456.7, diameter: 50, r: 0, g: 0, b: 0), 1.0);

        Assert.AreEqual(123.5, item.X);
        Assert.AreEqual(456.7, item.Y);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_MapsDiameterCorrectly()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 0, y: 0, diameter: 42, r: 0, g: 0, b: 0), 1.0);

        Assert.AreEqual(42, item.Diameter);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_MapsColorFromRgb()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 0, y: 0, diameter: 10, r: 100, g: 150, b: 200), 1.0);

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

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 10, y: 20, diameter: 30, r: 0, g: 0, b: 0), 1.0);

        CollectionAssert.Contains(changedProps, nameof(BallListItem.X));
        CollectionAssert.Contains(changedProps, nameof(BallListItem.Y));
        CollectionAssert.Contains(changedProps, nameof(BallListItem.Diameter));
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_SameValues_DoesNotRaisePropertyChanged()
    {
        var item = new BallListItem();
        var status = new FakeBallStatus(id: 0, x: 10, y: 20, diameter: 30, r: 50, g: 60, b: 70);
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
        item.UpdateFrom(new FakeBallStatus(id: 0, x: 10, y: 20, diameter: 30, r: 0, g: 0, b: 0), 1.0);

        var changedProps = new List<string>();
        item.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 15, y: 25, diameter: 30, r: 0, g: 0, b: 0), 1.0);

        CollectionAssert.Contains(changedProps, nameof(BallListItem.X));
        CollectionAssert.Contains(changedProps, nameof(BallListItem.Y));
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_ScalesPositionAndDiameter()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 100, y: 200, diameter: 50, r: 0, g: 0, b: 0), 2.0);

        Assert.AreEqual(200.0, item.X);
        Assert.AreEqual(400.0, item.Y);
        Assert.AreEqual(100.0, item.Diameter);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_ScaleOne_PreservesOriginalValues()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 55.5, y: 77.3, diameter: 40, r: 0, g: 0, b: 0), 1.0);

        Assert.AreEqual(55.5, item.X);
        Assert.AreEqual(77.3, item.Y);
        Assert.AreEqual(40.0, item.Diameter);
    }

    [TestMethod]
    public void BallListItem_UpdateFrom_FractionalScale_ComputesCorrectly()
    {
        var item = new BallListItem();

        item.UpdateFrom(new FakeBallStatus(id: 0, x: 300, y: 300, diameter: 60, r: 0, g: 0, b: 0), 0.5);

        Assert.AreEqual(150.0, item.X);
        Assert.AreEqual(150.0, item.Y);
        Assert.AreEqual(30.0, item.Diameter);
    }

    private sealed class FakeBallStatus : IBallStatus
    {
        public FakeBallStatus(int id, double x, double y, int diameter, byte r, byte g, byte b)
        {
            Id = id; X = x; Y = y; Diameter = diameter; R = r; G = g; B = b;
        }
        public int Id { get; }
        public double X { get; }
        public double Y { get; }
        public double VelocityX { get; }
        public double VelocityY { get; }
        public int Diameter { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
    }
}
