using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Data;
using Data.Diagnostics;

namespace DataTests;

[TestClass]
public sealed class BallDiagnosticsLoggerTests
{
    private static string NewTempFile() =>
        Path.Combine(Path.GetTempPath(), $"ball-diag-{Guid.NewGuid():N}.log");

    [TestMethod]
    public void BallDiagnosticEntry_ToAsciiLine_IsInvariantAsciiText()
    {
        var entry = new BallDiagnosticEntry(
            timestampTicks: 1234567,
            ballId: 7,
            x: 12.5,
            y: -3.75,
            velocityX: 0.125,
            velocityY: -0.5,
            stepCount: 42);

        string line = entry.ToAsciiLine();

        Assert.AreEqual("1234567;7;12.5000;-3.7500;0.1250;-0.5000;42", line);
        foreach (char ch in line)
            Assert.IsLessThan(128, (int)ch);
    }

    [TestMethod]
    public void Logger_StartLogStop_WritesAsciiLinesToFile()
    {
        string path = NewTempFile();
        try
        {
            using var logger = BallDiagnosticsLogger.Create(path, queueCapacity: 64);
            logger.Start();

            for (int i = 0; i < 10; i++)
                logger.Log(new BallDiagnosticEntry(i, i, i, i, 1, -1, i));

            logger.Stop();

            string[] lines = File.ReadAllLines(path);
            Assert.IsGreaterThanOrEqualTo(11, lines.Length);
            StringAssert.StartsWith(lines[0], "#");
            for (int i = 1; i < lines.Length; i++)
                StringAssert.Contains(lines[i], ";");
            Assert.AreEqual(10L, logger.WrittenCount);
            Assert.AreEqual(0L, logger.DroppedCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Logger_BoundedQueue_FullBuffer_DropsAndDoesNotBlockProducer()
    {
        string path = NewTempFile();
        try
        {
            using var logger = BallDiagnosticsLogger.Create(path, queueCapacity: 4);
            logger.Start();

            const int floodCount = 5000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < floodCount; i++)
                logger.Log(new BallDiagnosticEntry(0, 0, 0, 0, 0, 0, i));
            sw.Stop();

            Assert.IsLessThan(1500L, sw.ElapsedMilliseconds,
                $"Flooding {floodCount} entries took {sw.ElapsedMilliseconds} ms — producer is blocking on a full buffer.");

            logger.Stop();

            Assert.IsGreaterThan(0L, logger.DroppedCount);
            Assert.AreEqual(floodCount, logger.WrittenCount + logger.DroppedCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Logger_EmptyBuffer_WriterDoesNotBusySpin_AndShutsDownCleanly()
    {
        string path = NewTempFile();
        try
        {
            using var logger = BallDiagnosticsLogger.Create(path, queueCapacity: 32);
            logger.Start();

            Thread.Sleep(150);

            var sw = Stopwatch.StartNew();
            logger.Stop();
            sw.Stop();

            Assert.IsLessThan(2500L, sw.ElapsedMilliseconds);
            Assert.AreEqual(0L, logger.WrittenCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Logger_StopFlushesPendingEntriesBeforeReturning()
    {
        string path = NewTempFile();
        try
        {
            using var logger = BallDiagnosticsLogger.Create(path, queueCapacity: 1024);
            logger.Start();

            const int count = 200;
            for (int i = 0; i < count; i++)
                logger.Log(new BallDiagnosticEntry(0, i, i, i, 0, 0, i));

            logger.Stop();

            Assert.AreEqual(count, (int)logger.WrittenCount);
            string[] lines = File.ReadAllLines(path);
            Assert.HasCount(count + 1, lines);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Logger_LogAfterStop_DoesNotThrow_AndIsRecordedAsDropped()
    {
        string path = NewTempFile();
        try
        {
            using var logger = BallDiagnosticsLogger.Create(path, queueCapacity: 8);
            logger.Start();
            logger.Stop();

            long droppedBefore = logger.DroppedCount;
            for (int i = 0; i < 5; i++)
            {
                bool accepted = logger.Log(new BallDiagnosticEntry(0, 0, 0, 0, 0, 0, i));
                Assert.IsFalse(accepted);
            }

            Assert.AreEqual(droppedBefore + 5, logger.DroppedCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Repository_WithFileLogger_BallsContinueMovingAtComparableRate()
    {
        string path = NewTempFile();
        try
        {
            using var noLogApi = BallDataApi.CreateApi(1000, 1000);
            noLogApi.GenerateBalls(20);
            noLogApi.StartMovement();
            Thread.Sleep(600);
            noLogApi.StopMovement();
            int baselineSteps = SumStepCounts(noLogApi);

            using var logger = BallDiagnosticsLogger.Create(path, queueCapacity: 8);
            using var loggedApi = BallDataApi.CreateApi(1000, 1000, logger);
            loggedApi.GenerateBalls(20);
            loggedApi.StartMovement();
            Thread.Sleep(600);
            loggedApi.StopMovement();
            int loggedSteps = SumStepCounts(loggedApi);

            Assert.IsGreaterThan(0, baselineSteps);
            Assert.IsGreaterThan(0, loggedSteps);
            double ratio = (double)loggedSteps / baselineSteps;
            Assert.IsGreaterThan(0.5, ratio,
                $"Logged run produced only {loggedSteps} vs baseline {baselineSteps} steps — logging is slowing balls down.");
            Assert.IsGreaterThan(0L, logger.DroppedCount + logger.WrittenCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Repository_FullBuffer_FloodingDoesNotStarveBallSteps()
    {
        string path = NewTempFile();
        try
        {
            using var logger = BallDiagnosticsLogger.Create(path, queueCapacity: 2);
            using var api = BallDataApi.CreateApi(1000, 1000, logger);
            api.GenerateBalls(30);
            api.StartMovement();
            Thread.Sleep(400);
            api.StopMovement();

            int totalSteps = SumStepCounts(api);
            Assert.IsGreaterThan(0, totalSteps);
            Assert.IsGreaterThan(0L, logger.DroppedCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Logger_NullLogger_AcceptsEntriesWithoutSideEffects()
    {
        BallDiagnosticsLogger logger = BallDiagnosticsLogger.Null;
        Assert.IsTrue(logger.Log(new BallDiagnosticEntry(0, 0, 0, 0, 0, 0, 0)));
        Assert.AreEqual(0L, logger.WrittenCount);
        Assert.AreEqual(0L, logger.DroppedCount);
    }

    [TestMethod]
    public void Logger_Create_RejectsInvalidCapacity()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => BallDiagnosticsLogger.Create(NewTempFile(), queueCapacity: 0));
    }

    [TestMethod]
    public void Logger_Create_RejectsBlankPath()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => BallDiagnosticsLogger.Create("   ", queueCapacity: 16));
    }

    [TestMethod]
    public void Logger_AsciiLines_AreCultureInvariant()
    {
        CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pl-PL");

            var entry = new BallDiagnosticEntry(1, 0, 1.5, 2.5, 0, 0, 0);
            string line = entry.ToAsciiLine();

            StringAssert.Contains(line, "1.5000");
            StringAssert.Contains(line, "2.5000");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    private static int SumStepCounts(BallDataApi api)
    {
        int total = 0;
        foreach (IBallData b in api.Balls)
            total += ((Data.Models.BallEntity)b).StepCount;
        return total;
    }
}
