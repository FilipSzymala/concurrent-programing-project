using System.Collections.Generic;
using Data;
using Data.Models;

namespace Logic
{
    public class BallsService
    {
        // Here will be business logic in the future (but we leave it for now) 
        // for wonderers it's not AI comment, but mine : p
        private readonly BallRepository _repository;
        
        public int BoardWidth => _repository.BoardWidth;
        public int BoardHeight => _repository.BoardHeight;
        
        public BallsService(BallRepository repository)
        {
            _repository = repository;
        }

        public void GenerateRandomBalls()
        {
            _repository.GenerateRandomBalls();
        }

        public IEnumerable<BallEntity> FetchAllBalls()
        {
            return _repository.GetAllPositions();
        }
    }
}
