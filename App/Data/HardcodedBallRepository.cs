using System;
using System.Collections.Generic;
using Data.Models;

namespace Data
{
    public class HardcodedBallRepository
    {
        private readonly Random _random = new Random();
        private List<BallEntity> _balls;


        public void GenerateRandomBalls()
        {
           _balls = new List<BallEntity>();
           
           for (int i = 0; i < 10; i++)
           {
               int diameter = _random.Next(20, 51);
                
               int maxX = 500 - diameter;
               int maxY = 500 - diameter;

               int x = _random.Next(0, maxX + 1);
               int y = _random.Next(0, maxY + 1);
               
               // we end at 200 to make it at least visible on a white background
               byte r = (byte)_random.Next(0, 200);
               byte g = (byte)_random.Next(0, 200);
               byte b = (byte)_random.Next(0, 200);

               _balls.Add(new BallEntity
               {
                   X = x,
                   Y = y,
                   Diameter = diameter,
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