using System;
using System.Collections.Generic;
using Data;

namespace Logic
{
    public abstract class BallLogicApi : IDisposable
    {
        public const int MinBallsCount = 2;
        public const int DefaultBallsCount = 5;

        public abstract int BoardWidth { get; }
        public abstract int BoardHeight { get; }
        public abstract bool IsRunning { get; }

        public abstract event EventHandler<IReadOnlyList<IBallStatus>> BallsChanged;

        public abstract void Start(int ballsCount);
        public abstract void Stop();
        public abstract void Resume();
        public abstract void Toggle();
        public abstract void Dispose();

        public static BallLogicApi CreateApi(BallDataApi data) => new BallsService(data);
        public static BallLogicApi CreateApi(int boardWidth, int boardHeight) =>
            new BallsService(BallDataApi.CreateApi(boardWidth, boardHeight));
    }
}