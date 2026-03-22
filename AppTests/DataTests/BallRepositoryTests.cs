namespace DataTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BallRepositoryTests
{
    [DataTestMethod]
    [DataRow(800, 600, 15)]   // Standardowe okno
    [DataRow(100, 100, 5)]    // Bardzo małe okno
    [DataRow(1920, 1080, 50)] // Ekran Full HD
    public void GetAllPositions_ShouldGenerateCorrectNumberOfBalls(int width, int height, int count)
    {
        // Arrange
        var repository = new HardcodedBallRepository(width, height, count);

        // Act
        var balls = repository.GetAllPositions().ToList();

        // Assert
        Assert.IsNotNull(balls);
        Assert.AreEqual(count, balls.Count);
    }

    [DataTestMethod]
    [DataRow(500, 500, 10)]
    [DataRow(200, 800, 20)]
    public void GetAllPositions_BallsShouldStayWithinDynamicBounds(int width, int height, int count)
    {
        // Arrange
        var repository = new HardcodedBallRepository(width, height, count);

        // Act
        var balls = repository.GetAllPositions().ToList();

        // Assert
        foreach (var ball in balls)
        {
            // Oś X
            Assert.IsTrue(ball.X >= 0, "Kulka uciekła z lewej strony!");
            Assert.IsTrue(ball.X <= width - ball.Diameter, "Kulka uciekła z prawej strony!");
            
            // Oś Y
            Assert.IsTrue(ball.Y >= 0, "Kulka uciekła górą!");
            Assert.IsTrue(ball.Y <= height - ball.Diameter, "Kulka uciekła dołem!");
        }
    }

    [TestMethod]
    public void GetAllPositions_ShouldGenerateValidColorsAndDiameters()
    {
        // Arrange
        var repository = new HardcodedBallRepository(500, 500, 10);

        // Act
        var balls = repository.GetAllPositions().ToList();

        // Assert
        foreach (var ball in balls)
        {
            // Ręczne sprawdzenie zakresów (bo MSTest nie ma Assert.InRange)
            Assert.IsTrue(ball.Diameter >= 10 && ball.Diameter <= 50, "Średnica poza zakresem!");
            
            Assert.IsTrue(ball.R >= 50 && ball.R <= 255, "Kolor R poza zakresem!");
            Assert.IsTrue(ball.G >= 50 && ball.G <= 255, "Kolor G poza zakresem!");
            Assert.IsTrue(ball.B >= 50 && ball.B <= 255, "Kolor B poza zakresem!");
        }
    }
}