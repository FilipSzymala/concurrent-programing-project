using System;
using System.Collections.Generic;
using System.Timers;
using Data;

namespace Logic
{
    internal sealed class BallsService : BallLogicApi
    {
        private const double TickIntervalMs = 16.0;
        private readonly BallDataApi _data;
        private readonly Timer _timer;
        private readonly object _lock = new object();
        private bool _running;

        public BallsService(BallDataApi data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _timer = new Timer(TickIntervalMs) { AutoReset = true };
            _timer.Elapsed += OnTick;
        }

        public override int BoardWidth => _data.BoardWidth;
        public override int BoardHeight => _data.BoardHeight;
        public override bool IsRunning => _running;
        public override event EventHandler<IReadOnlyList<IBallStatus>> BallsChanged;

        public override void Start(int ballsCount)
        {
            if (ballsCount < BallLogicApi.MinBallsCount)
                throw new ArgumentOutOfRangeException(
                    nameof(ballsCount),
                    $"At least {BallLogicApi.MinBallsCount} balls are required.");

            List<IBallStatus> snapshot;
            lock (_lock)
            {
                _data.GenerateBalls(ballsCount);
                _running = true;
                _timer.Start();
                snapshot = CreateSnapshot();
            }

            RaiseChanged(snapshot);
        }

        public override void Stop()
        {
            lock (_lock)
            {
                _timer.Stop();
                _running = false;
            }
        }

        public override void Resume()
        {
            List<IBallStatus> snapshot = null;
            lock (_lock)
            {
                if (!_running && _data.Balls.Count > 0)
                {
                    _running = true;
                    _timer.Start();
                    snapshot = CreateSnapshot();
                }
            }

            if (snapshot != null)
                RaiseChanged(snapshot);
        }

        public override void Toggle()
        {
            if (!_running) Resume(); else Stop();
        }

        public override void Dispose()
        {
            Stop();
            _timer.Elapsed -= OnTick;
            _timer.Dispose();
        }

        private void OnTick(object sender, ElapsedEventArgs e)
        {
            List<IBallStatus> snapshot = null;
            lock (_lock)
            {
                if (_running)
                {
                    _data.UpdatePositions();
                    snapshot = CreateSnapshot();
                }
            }

            if (snapshot != null)
                RaiseChanged(snapshot);
        }

        private List<IBallStatus> CreateSnapshot()
        {
            var snapshot = new List<IBallStatus>(_data.Balls.Count);
            foreach (var b in _data.Balls)
                snapshot.Add(new BallStatus(b));
            return snapshot;
        }

        private void RaiseChanged(List<IBallStatus> snapshot)
        {
            var handler = BallsChanged;
            if (handler == null) return;
            handler(this, snapshot);
        }

        private sealed class BallStatus : IBallStatus
        {
            public BallStatus(IBallData b)
            {
                Id = b.Id; 
                X = b.X; 
                Y = b.Y; 
                Diameter = b.Diameter;
                VelocityX = b.VelocityX;
                VelocityY = b.VelocityY;
                R = b.R; G = b.G; B = b.B;
            }
            public int Id { get; }
            public double X { get; }
            public double Y { get; }
            public int Diameter { get; }
            public double VelocityX { get; }
            public double VelocityY { get; }
            public byte R { get; }
            public byte G { get; }
            public byte B { get; }
        }
    }
}