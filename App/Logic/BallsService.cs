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
        private readonly object _dragLock = new object();

        private ManualResetEventSlim _stopEvent;
        private Barrier _frameBarrier;
        private Thread _heartbeatThread;
        private volatile bool _running;
        private long _lastFrameTicks;
        private long _lastTickTicks;
        private long _lastFrameIntervalTicks;

        private int _draggedBallId = -1;
        private double _dragTargetX;
        private double _dragTargetY;
        private double _prevDragX;
        private double _prevDragY;
        private double _lastEffVX;
        private double _lastEffVY;

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

        public override void DragBall(int ballId, double x, double y)
        {
            bool isNewDrag;
            lock (_dragLock)
            {
                isNewDrag = _draggedBallId != ballId;
                _draggedBallId = ballId;
                _dragTargetX = x;
                _dragTargetY = y;
                if (isNewDrag)
                {
                    _prevDragX = x;
                    _prevDragY = y;
                    _lastEffVX = 0;
                    _lastEffVY = 0;
                }
            }
            if (isNewDrag)
            {
                IBallData ball = FindBall(_data.Balls, ballId);
                ball?.SetVelocity(new Vector2D(0, 0));
            }
        }

        public override void StopDragging(int ballId)
        {
            IBallData ball;
            double vx, vy;
            lock (_dragLock)
            {
                if (_draggedBallId != ballId) return;
                vx = _dragTargetX - _prevDragX;
                vy = _dragTargetY - _prevDragY;
                if (vx == 0 && vy == 0)
                {
                    vx = _lastEffVX;
                    vy = _lastEffVY;
                }
                _draggedBallId = -1;
                ball = FindBall(_data.Balls, ballId);
            }

            const double MaxDragSpeed = 6.0;
            double spd = Math.Sqrt(vx * vx + vy * vy);
            if (spd > MaxDragSpeed)
            {
                vx = vx / spd * MaxDragSpeed;
                vy = vy / spd * MaxDragSpeed;
            }

            ball?.SetVelocity(new Vector2D(vx, vy));
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

        private void HeartbeatLoop(ManualResetEventSlim stop)
        {
            while (!stop.IsSet)
            {
                if (stop.Wait(HeartbeatIntervalMs)) return;
                EmitFrame(applyCollisions: false);
            }
        }

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

            int draggedId;
            double dragX, dragY, effVX, effVY;
            lock (_dragLock)
            {
                draggedId = _draggedBallId;
                dragX = _dragTargetX;
                dragY = _dragTargetY;

                effVX = dragX - _prevDragX;
                effVY = dragY - _prevDragY;
                _prevDragX = dragX;
                _prevDragY = dragY;
                _lastEffVX = effVX;
                _lastEffVY = effVY;
            }

            const double MaxDragSpeed = 6.0;
            double spd = Math.Sqrt(effVX * effVX + effVY * effVY);
            if (spd > MaxDragSpeed)
            {
                effVX = effVX / spd * MaxDragSpeed;
                effVY = effVY / spd * MaxDragSpeed;
            }

            if (draggedId >= 0)
                OverrideDraggedBall(balls, n, draggedId, dragX, dragY,
                    new Vector2D(effVX, effVY));

            for (int i = 0; i < n; i++)
                if (balls[i].Id != draggedId)
                    CollisionResolver.ResolveWalls(balls[i], _data.BoardWidth, _data.BoardHeight);

            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    CollisionResolver.ResolveBallCollision(balls[i], balls[j]);

            for (int i = 0; i < n; i++)
                if (balls[i].Id != draggedId)
                    CollisionResolver.ResolveWalls(balls[i], _data.BoardWidth, _data.BoardHeight);

            ClampAllToBounds(balls, n, draggedId);

            if (draggedId >= 0)
                OverrideDraggedBall(balls, n, draggedId, dragX, dragY, new Vector2D(0, 0));
        }

        private void ClampAllToBounds(IReadOnlyList<IBallData> balls, int n, int draggedId)
        {
            int w = _data.BoardWidth;
            int h = _data.BoardHeight;
            for (int i = 0; i < n; i++)
            {
                if (balls[i].Id == draggedId) continue;
                BallSnapshot s = balls[i].Snapshot;
                double maxX = w - balls[i].Diameter;
                double maxY = h - balls[i].Diameter;
                double cx = s.X < 0 ? 0 : s.X > maxX ? maxX : s.X;
                double cy = s.Y < 0 ? 0 : s.Y > maxY ? maxY : s.Y;
                if (Math.Abs(cx - s.X) < 1e-9 && Math.Abs(cy - s.Y) < 1e-9) continue;

                double vx = s.Velocity.X;
                double vy = s.Velocity.Y;
                if (cx <= 0    && vx < 0) vx = -vx;
                if (cx >= maxX && vx > 0) vx = -vx;
                if (cy <= 0    && vy < 0) vy = -vy;
                if (cy >= maxY && vy > 0) vy = -vy;
                balls[i].ApplyChange(cx, cy, new Vector2D(vx, vy));
            }
        }

        private void OverrideDraggedBall(
            IReadOnlyList<IBallData> balls, int n, int draggedId, double x, double y,
            Vector2D velocity)
        {
            for (int i = 0; i < n; i++)
            {
                if (balls[i].Id != draggedId) continue;
                double maxX = _data.BoardWidth  - balls[i].Diameter;
                double maxY = _data.BoardHeight - balls[i].Diameter;
                double cx = x < 0 ? 0 : x > maxX ? maxX : x;
                double cy = y < 0 ? 0 : y > maxY ? maxY : y;
                balls[i].ApplyChange(cx, cy, velocity);
                break;
            }
        }

        private static IBallData FindBall(IReadOnlyList<IBallData> balls, int id)
        {
            for (int i = 0; i < balls.Count; i++)
                if (balls[i].Id == id) return balls[i];
            return null;
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
