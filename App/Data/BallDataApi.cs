using System;
using System.Collections.Generic;
using System.Threading;

namespace Data
{
    public abstract class BallDataApi : IDisposable
    {
        public abstract int BoardWidth { get; }
        public abstract int BoardHeight { get; }
        public abstract IReadOnlyList<IBallData> Balls { get; }
        public abstract int FrameParticipants { get; }

        public abstract void GenerateBalls(int count);
        public abstract void StartMovement(Barrier frameBarrier = null);
        public abstract void StopMovement();
        public abstract void Dispose();

        public static BallDataApi CreateApi(int boardWidth, int boardHeight) =>
            new BallRepository(boardWidth, boardHeight);
    }
}
