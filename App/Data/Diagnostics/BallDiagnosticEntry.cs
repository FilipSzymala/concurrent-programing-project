using System.Globalization;

namespace Data.Diagnostics
{
    public readonly struct BallDiagnosticEntry
    {
        public BallDiagnosticEntry(
            long timestampTicks,
            int ballId,
            double x,
            double y,
            double velocityX,
            double velocityY,
            int stepCount)
        {
            TimestampTicks = timestampTicks;
            BallId = ballId;
            X = x;
            Y = y;
            VelocityX = velocityX;
            VelocityY = velocityY;
            StepCount = stepCount;
        }

        public long TimestampTicks { get; }
        public int BallId { get; }
        public double X { get; }
        public double Y { get; }
        public double VelocityX { get; }
        public double VelocityY { get; }
        public int StepCount { get; }

        public string ToAsciiLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return string.Concat(
                TimestampTicks.ToString(c), ";",
                BallId.ToString(c), ";",
                X.ToString("F4", c), ";",
                Y.ToString("F4", c), ";",
                VelocityX.ToString("F4", c), ";",
                VelocityY.ToString("F4", c), ";",
                StepCount.ToString(c));
        }
    }
}
