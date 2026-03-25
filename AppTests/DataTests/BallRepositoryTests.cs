using Data;

namespace DataTests;

[TestClass]
public class BallRepositoryTests
{
    [TestMethod]
    [DataRow(800, 600, 15)]
    [DataRow(100, 100, 5)]
    [DataRow(1920, 1080, 50)]
    public void GetAllPositions_ShouldGenerateCorrectNumberOfBalls(int width, int height, int count)
    {
        var repository = new BallRepository(width, height, count);
        repository.GenerateRandomBalls();

        var balls = repository.GetAllPositions().ToList();

        Assert.IsNotNull(balls);
        Assert.HasCount(balls.Count, balls);
    }

    [TestMethod][DataRow(500, 500, 10)]
    [DataRow(200, 800, 20)]
    public void GetAllPositions_BallsShouldStayWithinDynamicBounds(int width, int height, int count)
    {
        var repository = new BallRepository(width, height, count);
        repository.GenerateRandomBalls();

        var balls = repository.GetAllPositions().ToList();

        foreach (var ball in balls)
        {
            Assert.IsGreaterThanOrEqualTo(0.0, ball.X);
            Assert.IsLessThanOrEqualTo((double)(width - ball.Diameter), ball.X);
        
            Assert.IsGreaterThanOrEqualTo(0.0, ball.Y);
            Assert.IsLessThanOrEqualTo((double)(height - ball.Diameter), ball.Y);
        }
    }

    [TestMethod]
    public void GetAllPositions_ShouldGenerateValidColorsAndDiameters()
    {
        int width = 500, height = 500, count = 10;
        var repository = new BallRepository(width, height, count);
        repository.GenerateRandomBalls();

        var balls = repository.GetAllPositions().ToList();

        foreach (var ball in balls)
        {
            Assert.IsTrue(ball.Diameter >= 10 && ball.Diameter <= 50);

            Assert.IsTrue(ball.R >= 0 && ball.R < 200);
            Assert.IsTrue(ball.G >= 0 && ball.G < 200);
            Assert.IsTrue(ball.B >= 0 && ball.B < 200);
        }
    }

    [TestMethod]
    public void GenerateRandomBalls_ShouldThrowException_WhenBoardIsTooSmall()
    {
        var repository = new BallRepository(10, 10, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => repository.GenerateRandomBalls());
    }

    [TestMethod]
    public void GenerateRandomBalls_ShouldAssignUniqueAndSequentialIds()
    {
        var repository = new BallRepository(500, 500, 10);

        repository.GenerateRandomBalls();
        var balls = repository.GetAllPositions().ToList();

        for (int i = 0; i < balls.Count; i++)
        {
            Assert.AreEqual(i, balls[i].Id);
        }
    }
}