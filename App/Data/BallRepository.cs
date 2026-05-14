using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Data.Models;

namespace Data
{
    internal sealed class BallRepository : BallDataApi
    {
        private const int MinDiameter = 35;
        private const int MaxDiameter = 70;
        private const double MaxAbsVelocity = 2.0;
        private const int StepIntervalMs = 16;

        private readonly Random _random = new Random();
        private readonly int _boardWidth;
        private readonly int _boardHeight;
        private readonly List<BallEntity> _balls = new List<BallEntity>();
        private readonly object _lifecycleLock = new object();

        private ManualResetEventSlim _stopEvent;
        private CancellationTokenSource _cts;
        private Thread[] _threads = Array.Empty<Thread>();
        private bool _isMoving;

        public BallRepository(int boardWidth, int boardHeight)
        {
            if (boardWidth <= MaxDiameter || boardHeight <= MaxDiameter)
                throw new ArgumentOutOfRangeException(
                    nameof(boardWidth),
                    $"Board must be larger than the maximum ball diameter ({MaxDiameter}).");

            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
        }

        public override int BoardWidth => _boardWidth;
        public override int BoardHeight => _boardHeight;
        public override IReadOnlyList<IBallData> Balls => _balls;
        public override int FrameParticipants => _balls.Count;

        public override void GenerateBalls(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            StopMovement();

            lock (_lifecycleLock)
            {
                _balls.Clear();

                for (int i = 0; i < count; i++)
                {
                    int diameter = _random.Next(MinDiameter, MaxDiameter + 1);

                    double x = _random.Next(0, _boardWidth - diameter + 1);
                    double y = _random.Next(0, _boardHeight - diameter + 1);

                    byte r = (byte)_random.Next(0, 200);
                    byte g = (byte)_random.Next(0, 200);
                    byte b = (byte)_random.Next(0, 200);

                    double vx = NextNonZeroVelocity();
                    double vy = NextNonZeroVelocity();

                    double radius = diameter / 2.0;
                    double mass = Math.PI * radius * radius;

                    _balls.Add(new BallEntity(
                        id: i,
                        x: x,
                        y: y,
                        diameter: diameter,
                        mass: mass,
                        velocity: new Vector2D(vx, vy),
                        r: r, g: g, b: b));
                }
            }
        }

        public override void StartMovement(Barrier frameBarrier = null)
        {
            lock (_lifecycleLock)
            {
                if (_isMoving) return;
                if (_balls.Count == 0) return;

                _stopEvent = new ManualResetEventSlim(false);
                _cts = new CancellationTokenSource();
                CancellationToken token = _cts.Token;
                ManualResetEventSlim stop = _stopEvent;

                var threads = new Thread[_balls.Count];
                for (int i = 0; i < _balls.Count; i++)
                {
                    BallEntity ball = _balls[i];
                    var thread = new Thread(() =>
                    {
                        if (frameBarrier != null)
                            RunCoordinatedLoop(ball, frameBarrier, token);
                        else
                            RunFreeLoop(ball, stop);
                    })
                    {
                        IsBackground = true,
                        Name = $"Ball-{i}"
                    };
                    threads[i] = thread;
                    thread.Start();
                }
                _threads = threads;
                _isMoving = true;
            }
        }

        public override void StopMovement()
        {
            ManualResetEventSlim stopEvent;
            CancellationTokenSource cts;
            Thread[] threads;
            lock (_lifecycleLock)
            {
                if (!_isMoving) return;
                stopEvent = _stopEvent;
                cts = _cts;
                threads = _threads;
                _stopEvent = null;
                _cts = null;
                _threads = Array.Empty<Thread>();
                _isMoving = false;
            }

            stopEvent?.Set();
            cts?.Cancel();
            foreach (Thread t in threads)
                t.Join(TimeSpan.FromSeconds(2));
            stopEvent?.Dispose();
            cts?.Dispose();
        }

        public override void Dispose()
        {
            StopMovement();
        }

        internal void SeedBall(double x, double y, int diameter, double mass, Vector2D velocity)
        {
            lock (_lifecycleLock)
            {
                _balls.Add(new BallEntity(
                    id: _balls.Count,
                    x: x, y: y,
                    diameter: diameter,
                    mass: mass,
                    velocity: velocity,
                    r: 0, g: 0, b: 0));
            }
        }

        internal void ClearBalls()
        {
            lock (_lifecycleLock)
            {
                _balls.Clear();
            }
        }

        private static void RunFreeLoop(BallEntity ball, ManualResetEventSlim stopEvent)
        {
            var sw = Stopwatch.StartNew();
            double lastMs = sw.Elapsed.TotalMilliseconds;
            while (!stopEvent.IsSet)
            {
                if (stopEvent.Wait(StepIntervalMs)) break;
                double nowMs = sw.Elapsed.TotalMilliseconds;
                double dtMs = nowMs - lastMs;
                lastMs = nowMs;
                ball.Step(dtMs);
            }
        }

        private static void RunCoordinatedLoop(BallEntity ball, Barrier barrier, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            double lastMs = sw.Elapsed.TotalMilliseconds;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    barrier.SignalAndWait(token);
                    double nowMs = sw.Elapsed.TotalMilliseconds;
                    double dtMs = nowMs - lastMs;
                    lastMs = nowMs;
                    ball.Step(dtMs);
                    barrier.SignalAndWait(token);
                }
            }
            catch (OperationCanceledException) { }
            catch (BarrierPostPhaseException) { }
            catch (ObjectDisposedException) { }
        }

        private double NextNonZeroVelocity()
        {
            double v;
            do
            {
                v = (_random.NextDouble() * 2.0 - 1.0) * MaxAbsVelocity;
            } while (Math.Abs(v) < 0.3);
            return v;
        }
    }
}
