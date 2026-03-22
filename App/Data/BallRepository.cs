using System;
using System.Collections.Generic;
using Data.Models;

namespace Data
{
    public class BallRepository
    {
        private readonly Random _random = new Random();
        private readonly int _boardWidth;
        private readonly int _boardHeight;
        private readonly int _ballsCount;
        private List<BallEntity> _balls;
        
        public BallRepository(int boardWidth, int boardHeight, int ballsCount)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
            _ballsCount = ballsCount;
        }

        public void GenerateRandomBalls()
        {
           _balls = new List<BallEntity>();
           
           for (int i = 0; i < _ballsCount; i++)
           {
               int id = i;
               
               int diameter = _random.Next(20, 51);
                
               int maxX = _boardWidth - diameter;
               int maxY = _boardHeight - diameter;

               double x = _random.Next(0, maxX + 1);
               double y = _random.Next(0, maxY + 1);
               
               // we end at 200 to make it at least visible on a white background
               byte r = (byte)_random.Next(0, 200);
               byte g = (byte)_random.Next(0, 200);
               byte b = (byte)_random.Next(0, 200);
               
               double velocityX = (double)_random.Next(-10, 11) / 10;
               double velocityY = (double)_random.Next(-10, 11) / 10;
               

               _balls.Add(new BallEntity
               {
                   Id = id,
                   X = x,
                   Y = y,
                   Diameter = diameter,
                   VelocityX = velocityX,
                   VelocityY = velocityY,
                   R = r,
                   G = g,
                   B = b
               });
           }
           
        }
        
        public IEnumerable<BallEntity> GetAllPositions()
        {
            return _balls;
        }
    }
}