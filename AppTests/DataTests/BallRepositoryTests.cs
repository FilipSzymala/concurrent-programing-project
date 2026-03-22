using Data;

namespace DataTests
{
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
            Assert.HasCount(balls.Count, balls, "Repository generated wrong number of balls.");
        }

        [TestMethod]
        [DataRow(500, 500, 10)]
        [DataRow(200, 800, 20)]
        public void GetAllPositions_BallsShouldStayWithinDynamicBounds(int width, int height, int count)
        {
            var repository = new BallRepository(width, height, count);
            repository.GenerateRandomBalls();

            var balls = repository.GetAllPositions().ToList();

            foreach (var ball in balls)
            {
                Assert.IsTrue(ball.X >= 0, $"Negative X detected on ball (X: {ball.X})");
                Assert.IsTrue(ball.X <= width - ball.Diameter, $"Ball out of right bounds (X:{ball.X}; Y:{ball.Y};diameter:{ball.Diameter})");
                
                Assert.IsTrue(ball.Y >= 0, $"Negative Y detected on ball (Y: {ball.Y})");
                Assert.IsTrue(ball.Y <= height - ball.Diameter, $"Ball out of left bounds (X:{ball.X}; Y:{ball.Y};diameter:{ball.Diameter})");
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
                Assert.IsTrue(ball.Diameter >= 10 && ball.Diameter <= 50, "Diameter over size limit");
                
                Assert.IsTrue(ball.R >= 0 && ball.R < 200, "Ball R over size limit");
                Assert.IsTrue(ball.G >= 0 && ball.G < 200, "Ball G over size limit");
                Assert.IsTrue(ball.B >= 0 && ball.B < 200, "Ball B over size limit");
            }
        }
    }
}