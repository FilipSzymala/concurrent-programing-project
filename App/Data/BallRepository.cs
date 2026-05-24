using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Data.Diagnostics;
using Data.Models;

namespace Data
{
    internal sealed class BallRepository : BallDataApi
    {
        private const int MinDiameter = 35;
        private const int MaxDiameter = 70;
        private const double MaxAbsVelocity = 2.0;
        private const int StepIntervalMs = 16;

        // Throttle: each ball emits at most one diagnostic line every DiagnosticIntervalMs
        // (set to 0 to log every step). At 100 ms we get ~10 Hz per ball — enough to see
        // motion in the log, ~6× less I/O than logging every step at 60 fps.
        private const int DiagnosticIntervalMs = 100;

        private readonly Random _random = new Random();
        private readonly int _boardWidth;
        private readonly int _boardHeight;
        private readonly List<BallEntity> _balls = new List<BallEntity>();
        private readonly object _lifecycleLock = new object();
        private readonly BallDiagnosticsLogger _logger;

        private ManualResetEventSlim _stopEvent;
        private Thread[] _threads = Array.Empty<Thread>();
        private bool _isMoving;

        public BallRepository(int boardWidth, int boardHeight)
            : this(boardWidth, boardHeight, BallDiagnosticsLogger.Null) { }

        public BallRepository(int boardWidth, int boardHeight, BallDiagnosticsLogger logger)
        {
            if (boardWidth <= MaxDiameter || boardHeight <= MaxDiameter)
                throw new ArgumentOutOfRangeException(
                    nameof(boardWidth),
                    $"Board must be larger than the maximum ball diameter ({MaxDiameter}).");

            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
            _logger = logger ?? BallDiagnosticsLogger.Null;
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

                _logger.Start();
                _stopEvent = new ManualResetEventSlim(false);
                ManualResetEventSlim stop = _stopEvent;
                BallDiagnosticsLogger logger = _logger;

                var threads = new Thread[_balls.Count];
                for (int i = 0; i < _balls.Count; i++)
                {
                    BallEntity ball = _balls[i];
                    var thread = new Thread(() =>
                    {
                        if (frameBarrier != null)
                            RunBarrierStepLoop(ball, frameBarrier, stop, logger);
                        else
                            RunIndependentStepLoop(ball, stop, logger);
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
            Thread[] threads;
            lock (_lifecycleLock)
            {
                if (!_isMoving) return;
                stopEvent = _stopEvent;
                threads = _threads;
                _stopEvent = null;
                _threads = Array.Empty<Thread>();
                _isMoving = false;
            }

            stopEvent?.Set();
            foreach (Thread t in threads)
                t.Join(TimeSpan.FromSeconds(2));
            stopEvent?.Dispose();

            _logger.Stop();
        }

        public override void Dispose()
        {
            StopMovement();
            _logger.Dispose();
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

        // Independent loop: ball moves on its own thread, no rendezvous with peers.
        // Used when caller does not supply a frame barrier (e.g. unit tests that
        // only need movement, or the no-collision integration path).
        private static void RunIndependentStepLoop(BallEntity ball, ManualResetEventSlim stop, BallDiagnosticsLogger logger)
        {
            var sw = Stopwatch.StartNew();
            double lastMs = sw.Elapsed.TotalMilliseconds;
            double lastLogMs = double.NegativeInfinity;
            while (!stop.IsSet)
            {
                if (stop.Wait(StepIntervalMs)) return;
                double nowMs = sw.Elapsed.TotalMilliseconds;
                double dtMs = nowMs - lastMs;
                lastMs = nowMs;
                ball.Step(dtMs);
                if (nowMs - lastLogMs >= DiagnosticIntervalMs)
                {
                    EmitDiagnostic(ball, logger);
                    lastLogMs = nowMs;
                }
            }
        }

        // Barrier-synchronised loop: ball performs its time-scaled step, then meets
        // its peers at the frame barrier. When the last ball arrives, the barrier
        // automatically invokes its postPhaseAction (collision detection + snapshot
        // in the Logic layer) — all other ball threads are blocked here, so that
        // action sees a frozen, consistent world. One SignalAndWait per frame.
        private static void RunBarrierStepLoop(BallEntity ball, Barrier barrier, ManualResetEventSlim stop, BallDiagnosticsLogger logger)
        {
            var sw = Stopwatch.StartNew();
            double lastMs = sw.Elapsed.TotalMilliseconds;
            double lastLogMs = double.NegativeInfinity;
            while (!stop.IsSet)
            {
                if (stop.Wait(StepIntervalMs))
                {
                    LeaveBarrier(barrier);
                    return;
                }

                double nowMs = sw.Elapsed.TotalMilliseconds;
                double dtMs = nowMs - lastMs;
                lastMs = nowMs;
                ball.Step(dtMs);
                if (nowMs - lastLogMs >= DiagnosticIntervalMs)
                {
                    EmitDiagnostic(ball, logger);
                    lastLogMs = nowMs;
                }

                if (stop.IsSet)
                {
                    LeaveBarrier(barrier);
                    return;
                }

                if (!TryRendezvous(barrier))
                    return;
            }
        }

        // Single shutdown-exception sink. SignalAndWait can fail in three ways once
        // we begin tearing the simulation down — barrier disposed, post-phase action
        // observed an inconsistent state, or another thread already removed itself.
        // All three mean the same thing here: we're stopping; exit the loop quietly.
        private static bool TryRendezvous(Barrier barrier)
        {
            try
            {
                barrier.SignalAndWait();
                return true;
            }
            catch (BarrierPostPhaseException) { return false; }
            catch (ObjectDisposedException)   { return false; }   // ObjectDisposedException inherits from InvalidOperationException — keep it first.
            catch (InvalidOperationException) { return false; }
        }

        // Called once per ball thread on shutdown so peers still waiting at the
        // barrier are released without waiting for a participant that won't arrive.
        private static void LeaveBarrier(Barrier barrier)
        {
            try
            {
                barrier.RemoveParticipant();
            }
            catch (ObjectDisposedException)   { /* barrier disposed by Stop()      */ }
            catch (InvalidOperationException) { /* already removed / barrier done */ }
        }

        private static void EmitDiagnostic(BallEntity ball, BallDiagnosticsLogger logger)
        {
            if (logger == null) return;
            ball.CaptureDiagnostic(out double x, out double y, out Vector2D velocity, out int stepCount);
            logger.Log(new BallDiagnosticEntry(
                timestampTicks: Stopwatch.GetTimestamp(),
                ballId: ball.Id,
                x: x, y: y,
                velocityX: velocity.X, velocityY: velocity.Y,
                stepCount: stepCount));
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
