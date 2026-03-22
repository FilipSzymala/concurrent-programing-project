using System.Collections.Generic;
using Data;
using Data.Models;

namespace Logic
{
    public class BallsService
    {
        // Here will be business logic in the future (but we leave it for now) 
        // for wonderers it's not AI comment, but mine : p
        private readonly HardcodedBallRepository _repository;
        
        public BallsService(HardcodedBallRepository repository)
        {
            _repository = repository;
        }

        public IEnumerable<BallEntity> FetchAllBalls()
        {
            return _repository.GetAllPositions();
        }
    }
}
