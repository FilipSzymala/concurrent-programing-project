using System;
using System.Collections.Generic;
using Data.Models;

namespace Data
{
    internal sealed class BallRepository : BallDataApi
    {
        private const int MinDiameter = 35;
        private const int MaxDiameter = 70;
        private const double MaxAbsVelocity = 2.0;

        private readonly Random _random = new Random();
        private readonly int _boardWidth;
        private readonly int _boardHeight;
        private readonly List<BallEntity> _balls = new List<BallEntity>();

        public BallRepository(int boardWidth, int boardHeight)
        {
            if (boardWidth <= MaxDiameter || boardHeight <= MaxDiameter)
                throw new ArgumentOutOfRangeException(
                    nameof(boardWidth),
                    $"Board must be larger than the maximum ball diameter ({MaxDiameter}).");

            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
        }

        public override int BoardWidth => _boardWidth;
        public override int BoardHeight => _boardHeight;
        public override IReadOnlyList<IBallData> Balls => _balls;

        public override void GenerateBalls(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            _balls.Clear();

            for (int i = 0; i < count; i++)
            {
                int diameter = _random.Next(MinDiameter, MaxDiameter + 1);

                double x = _random.Next(0, _boardWidth - diameter + 1);
                double y = _random.Next(0, _boardHeight - diameter + 1);

                byte r = (byte)_random.Next(0, 200);
                byte g = (byte)_random.Next(0, 200);
                byte b = (byte)_random.Next(0, 200);

                double vx = NextNonZeroVelocity();
                double vy = NextNonZeroVelocity();

                _balls.Add(new BallEntity
                {
                    Id = i,
                    X = x,
                    Y = y,
                    Diameter = diameter,
                    Velocity = new Vector2D(vx, vy),
                    R = r,
                    G = g,
                    B = b
                });
            }
        }

        public override void UpdatePositions()
        {
            foreach (var ball in _balls)
            {
                double maxX = _boardWidth - ball.Diameter;
                double maxY = _boardHeight - ball.Diameter;

                double newX = ball.X + ball.Velocity.X;
                double newY = ball.Y + ball.Velocity.Y;
                var velocity = ball.Velocity;

                if (newX < 0)
                {
                    newX = -newX;
                    velocity = velocity.WithX(-velocity.X);
                }
                else if (newX > maxX)
                {
                    newX = 2 * maxX - newX;
                    velocity = velocity.WithX(-velocity.X);
                }

                if (newY < 0)
                {
                    newY = -newY;
                    velocity = velocity.WithY(-velocity.Y);
                }
                else if (newY > maxY)
                {
                    newY = 2 * maxY - newY;
                    velocity = velocity.WithY(-velocity.Y);
                }

                if (newX < 0) newX = 0;
                else if (newX > maxX) newX = maxX;
                if (newY < 0) newY = 0;
                else if (newY > maxY) newY = maxY;

                ball.X = newX;
                ball.Y = newY;
                ball.Velocity = velocity;
            }
        }

        internal void SeedBall(double x, double y, int diameter, Vector2D velocity)
        {
            _balls.Add(new BallEntity
            {
                Id = _balls.Count,
                X = x,
                Y = y,
                Diameter = diameter,
                Velocity = velocity,
                R = 0, G = 0, B = 0
            });
        }

        internal void ClearBalls() => _balls.Clear();

        private double NextNonZeroVelocity()
        {
            double v;
            do
            {
                v = (_random.NextDouble() * 2.0 - 1.0) * MaxAbsVelocity;
            } while (Math.Abs(v) < 0.3);
            return v;
        }
    }
}