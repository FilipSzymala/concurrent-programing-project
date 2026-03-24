using Logic;
using Data;

namespace LogicTests;

[TestClass]
public sealed class BallsServiceTests
{
    [TestMethod]
    public void Constructor_ShouldSetDimensionsCorrectly()
    {
        var repository = new BallRepository(800, 600, 10);
        var service = new BallsService(repository);

        Assert.AreEqual(800, service.BoardWidth);
        Assert.AreEqual(600, service.BoardHeight);
    }

    [TestMethod]
    public void FetchAllBalls_ShouldReturnNullBeforeGeneration()
    {
        var repository = new BallRepository(500, 500, 5);
        var service = new BallsService(repository);

        var balls = service.FetchAllBalls();

        Assert.IsNull(balls);
    }

    [TestMethod]
    public void GenerateRandomBalls_ShouldPopulateRepository()
    {
        int ballsCount = 15;
        var repository = new BallRepository(1920, 1080, ballsCount);
        var service = new BallsService(repository);

        service.GenerateRandomBalls();
        var balls = service.FetchAllBalls().ToList();

        Assert.IsNotNull(balls);
        Assert.AreEqual(ballsCount, balls.Count);
    }
}