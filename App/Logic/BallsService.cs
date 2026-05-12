using System;
using System.Collections.Generic;
using System.Threading;
using Data;
using Logic.Physics;

namespace Logic
{
    internal sealed class BallsService : BallLogicApi
    {
        private const int PhysicsTickMs = 16;

        private readonly BallDataApi _data;
        private readonly object _lifecycleLock = new object();

        private ManualResetEventSlim _stopEvent;
        private Thread _physicsThread;
        private volatile bool _running;

        public BallsService(BallDataApi data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public override int BoardWidth => _data.BoardWidth;
        public override int BoardHeight => _data.BoardHeight;
        public override bool IsRunning => _running;
        public override event EventHandler<IReadOnlyList<IBallStatus>> BallsChanged;

        public override void Start(int ballsCount)
        {
            if (ballsCount < MinBallsCount)
                throw new ArgumentOutOfRangeException(
                    nameof(ballsCount),
                    $"At least {MinBallsCount} balls are required.");

            List<IBallStatus> snapshot;
            lock (_lifecycleLock)
            {
                StopUnlocked();
                _data.GenerateBalls(ballsCount);
                snapshot = CreateSnapshot();
                StartUnlocked();
            }

            RaiseChanged(snapshot);
        }

        public override void Stop()
        {
            lock (_lifecycleLock)
            {
                StopUnlocked();
            }
        }

        public override void Resume()
        {
            List<IBallStatus> snapshot = null;
            lock (_lifecycleLock)
            {
                if (_running) return;
                if (_data.Balls.Count == 0) return;

                snapshot = CreateSnapshot();
                StartUnlocked();
            }

            if (snapshot != null)
                RaiseChanged(snapshot);
        }

        public override void Toggle()
        {
            List<IBallStatus> snapshot = null;
            lock (_lifecycleLock)
            {
                if (_running)
                {
                    StopUnlocked();
                }
                else if (_data.Balls.Count > 0)
                {
                    snapshot = CreateSnapshot();
                    StartUnlocked();
                }
            }

            if (snapshot != null)
                RaiseChanged(snapshot);
        }

        public override void Dispose()
        {
            lock (_lifecycleLock)
            {
                StopUnlocked();
            }
            _data.Dispose();
        }

        private void StartUnlocked()
        {
            _stopEvent = new ManualResetEventSlim(false);
            ManualResetEventSlim stopEvent = _stopEvent;
            _data.StartMovement();
            _running = true;
            _physicsThread = new Thread(() => PhysicsLoop(stopEvent))
            {
                IsBackground = true,
                Name = "PhysicsLoop"
            };
            _physicsThread.Start();
        }

        private void StopUnlocked()
        {
            if (!_running && _stopEvent == null) return;

            ManualResetEventSlim stopEvent = _stopEvent;
            Thread thread = _physicsThread;
            _stopEvent = null;
            _physicsThread = null;
            _running = false;

            stopEvent?.Set();
            _data.StopMovement();
            thread?.Join(TimeSpan.FromSeconds(2));
            stopEvent?.Dispose();
        }

        private void PhysicsLoop(ManualResetEventSlim stopEvent)
        {
            while (!stopEvent.IsSet)
            {
                ApplyPhysics();
                List<IBallStatus> snapshot = CreateSnapshot();
                RaiseChanged(snapshot);
                if (stopEvent.Wait(PhysicsTickMs)) break;
            }
        }

        internal void ApplyPhysics()
        {
            IReadOnlyList<IBallData> balls = _data.Balls;
            int n = balls.Count;

            for (int i = 0; i < n; i++)
                CollisionResolver.ResolveWalls(balls[i], _data.BoardWidth, _data.BoardHeight);

            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    CollisionResolver.ResolveBallCollision(balls[i], balls[j]);
        }

        private List<IBallStatus> CreateSnapshot()
        {
            IReadOnlyList<IBallData> balls = _data.Balls;
            var snapshot = new List<IBallStatus>(balls.Count);
            foreach (IBallData b in balls)
            {
                BallSnapshot s = b.Snapshot;
                snapshot.Add(new BallStatus(
                    id: b.Id,
                    x: s.X,
                    y: s.Y,
                    diameter: b.Diameter,
                    mass: b.Mass,
                    velocityX: s.Velocity.X,
                    velocityY: s.Velocity.Y,
                    r: b.R, g: b.G, b: b.B));
            }
            return snapshot;
        }

        private void RaiseChanged(List<IBallStatus> snapshot)
        {
            EventHandler<IReadOnlyList<IBallStatus>> handler = BallsChanged;
            if (handler == null) return;
            handler(this, snapshot);
        }

        private sealed class BallStatus : IBallStatus
        {
            public BallStatus(int id, double x, double y, int diameter, double mass,
                              double velocityX, double velocityY, byte r, byte g, byte b)
            {
                Id = id;
                X = x;
                Y = y;
                Diameter = diameter;
                Mass = mass;
                VelocityX = velocityX;
                VelocityY = velocityY;
                R = r;
                G = g;
                B = b;
            }

            public int Id { get; }
            public double X { get; }
            public double Y { get; }
            public int Diameter { get; }
            public double Mass { get; }
            public double VelocityX { get; }
            public double VelocityY { get; }
            public byte R { get; }
            public byte G { get; }
            public byte B { get; }
        }
    }
}
