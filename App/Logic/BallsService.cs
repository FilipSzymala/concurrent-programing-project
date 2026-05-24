using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Data;
using Logic.Physics;

namespace Logic
{
    internal sealed class BallsService : BallLogicApi
    {
        private const int HeartbeatIntervalMs = 16;

        private readonly BallDataApi _data;
        private readonly object _lifecycleLock = new object();
        private readonly object _collisionLock = new object();

        private ManualResetEventSlim _stopEvent;
        private Barrier _frameBarrier;
        private Thread _heartbeatThread;
        private volatile bool _running;
        private long _lastFrameTicks;
        private long _lastTickTicks;
        private long _lastFrameIntervalTicks;

        public BallsService(BallDataApi data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public override int BoardWidth => _data.BoardWidth;
        public override int BoardHeight => _data.BoardHeight;
        public override bool IsRunning => _running;
        public override double LastTickDurationMs =>
            Interlocked.Read(ref _lastTickTicks) * 1000.0 / Stopwatch.Frequency;
        public override double LastFrameIntervalMs =>
            Interlocked.Read(ref _lastFrameIntervalTicks) * 1000.0 / Stopwatch.Frequency;
        public override double PhysicsBudgetMs => HeartbeatIntervalMs;
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

        // Two execution modes:
        //   - N > 0 balls: build a Barrier(N, OnFrameRendezvous). Each ball signals
        //     once per frame; when the last arrives, OnFrameRendezvous runs in that
        //     thread while the others are blocked → physics happens lock-free.
        //   - N == 0 (test/empty fake data): no barrier, run a heartbeat thread so
        //     BallsChanged is still raised on a timer for subscribers.
        private void StartUnlocked()
        {
            _lastFrameTicks = 0;
            _stopEvent = new ManualResetEventSlim(false);
            ManualResetEventSlim stop = _stopEvent;

            int participants = _data.FrameParticipants;
            if (participants > 0)
            {
                _frameBarrier = new Barrier(participants, _ => EmitFrame(applyCollisions: true));
                _data.StartMovement(_frameBarrier);
                _heartbeatThread = null;
            }
            else
            {
                _frameBarrier = null;
                _data.StartMovement(null);
                _heartbeatThread = new Thread(() => HeartbeatLoop(stop))
                {
                    IsBackground = true,
                    Name = "BallsServiceHeartbeat"
                };
                _heartbeatThread.Start();
            }

            _running = true;
        }

        private void StopUnlocked()
        {
            if (!_running && _stopEvent == null) return;

            ManualResetEventSlim stopEvent = _stopEvent;
            Barrier barrier = _frameBarrier;
            Thread heartbeat = _heartbeatThread;
            _stopEvent = null;
            _frameBarrier = null;
            _heartbeatThread = null;
            _running = false;

            stopEvent?.Set();
            _data.StopMovement();
            heartbeat?.Join(TimeSpan.FromSeconds(2));
            barrier?.Dispose();
            stopEvent?.Dispose();
        }

        // Heartbeat for the no-balls path: just emit periodic snapshots so observers
        // (and the existing test suite) see "the simulation is alive". No collisions.
        private void HeartbeatLoop(ManualResetEventSlim stop)
        {
            while (!stop.IsSet)
            {
                if (stop.Wait(HeartbeatIntervalMs)) return;
                EmitFrame(applyCollisions: false);
            }
        }

        // Single frame transaction: optional collision resolution, snapshot the
        // world, update timing counters, notify subscribers. When called from the
        // Barrier's postPhaseAction (applyCollisions: true), all ball threads are
        // blocked at the barrier, so it's safe to mutate ball state directly.
        private void EmitFrame(bool applyCollisions)
        {
            var sw = Stopwatch.StartNew();

            if (applyCollisions)
            {
                lock (_collisionLock)
                {
                    ApplyPhysics();
                }
            }

            List<IBallStatus> snapshot = CreateSnapshot();

            long now = Stopwatch.GetTimestamp();
            long prev = Interlocked.Exchange(ref _lastFrameTicks, now);
            if (prev > 0)
                Interlocked.Exchange(ref _lastFrameIntervalTicks, now - prev);

            RaiseChanged(snapshot);

            sw.Stop();
            Interlocked.Exchange(ref _lastTickTicks, sw.ElapsedTicks);
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
