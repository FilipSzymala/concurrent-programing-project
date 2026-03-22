using System;
using System.Collections.Generic;
using Data.Models;

namespace Data
{
    public class HardcodedBallRepository
    {
        private readonly Random _random = new Random();
        public IEnumerable<BallEntity> GetAllPositions()
        {
            var balls = new List<BallEntity>();

            for (int i = 0; i < 10; i++)
            {
                int diameter = _random.Next(10, 51);
                
                int maxX = 500 - diameter;
                int maxY = 500 - diameter;

                int x = _random.Next(0, maxX + 1);
                int y = _random.Next(0, maxY + 1);

                balls.Add(new BallEntity
                {
                    X = x,
                    Y = y,
                    Diameter = diameter
                });
            }

            return balls;
        }
    }
}