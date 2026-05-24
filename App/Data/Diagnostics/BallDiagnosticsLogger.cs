using System;

namespace Data.Diagnostics
{
    public abstract class BallDiagnosticsLogger : IDisposable
    {
        public abstract long WrittenCount { get; }
        public abstract long DroppedCount { get; }
        public abstract int QueueCapacity { get; }

        public abstract void Start();
        public abstract void Stop();
        public abstract bool Log(BallDiagnosticEntry entry);
        public abstract void Dispose();

        public static BallDiagnosticsLogger Create(string filePath, int queueCapacity = 1024) =>
            new FileBallDiagnosticsLogger(filePath, queueCapacity);

        public static BallDiagnosticsLogger Null { get; } = new NullBallDiagnosticsLogger();

        private sealed class NullBallDiagnosticsLogger : BallDiagnosticsLogger
        {
            public override long WrittenCount => 0;
            public override long DroppedCount => 0;
            public override int QueueCapacity => 0;
            public override void Start() { }
            public override void Stop() { }
            public override bool Log(BallDiagnosticEntry entry) => true;
            public override void Dispose() { }
        }
    }
}
