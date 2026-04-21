using System.Collections.Generic;

namespace Data
{
    public abstract class BallDataApi
    {
        public abstract int BoardWidth { get; }
        public abstract int BoardHeight { get; }
        public abstract IReadOnlyList<IBallData> Balls { get; }

        public abstract void GenerateBalls(int count);

        public abstract void UpdatePositions();

        public static BallDataApi CreateApi(int boardWidth, int boardHeight) =>
            new BallRepository(boardWidth, boardHeight);
    }
}