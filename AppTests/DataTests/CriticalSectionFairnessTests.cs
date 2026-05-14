using System.Threading;

namespace DataTests;

[TestClass]
public sealed class CriticalSectionFairnessTests
{
    [TestMethod]
    public void Barrier_Rendezvous_GuaranteesEqualCriticalSectionEntries_WithinOne()
    {
        const int threadCount = 100;
        const int rounds = 50;
        using var barrier = new Barrier(threadCount);
        int[] perThreadEntries = new int[threadCount];
        var sharedLock = new object();
        int sharedCounter = 0;

        var threads = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            int idx = i;
            threads[i] = new Thread(() =>
            {
                for (int r = 0; r < rounds; r++)
                {
                    barrier.SignalAndWait();

                    lock (sharedLock)
                    {
                        sharedCounter++;
                        perThreadEntries[idx]++;
                    }
                }
            }) { IsBackground = true };
        }

        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join();

        int min = perThreadEntries.Min();
        int max = perThreadEntries.Max();

        Assert.AreEqual(rounds, min);
        Assert.AreEqual(rounds, max);
        Assert.IsLessThanOrEqualTo(1, max - min);
        Assert.AreEqual(threadCount * rounds, sharedCounter);
    }

    [TestMethod]
    public void Barrier_TwoThreadRendezvous_AllowsAtomicDataExchange()
    {
        using var barrier = new Barrier(2);
        int produced = 0;
        int observed = -1;

        var producer = new Thread(() =>
        {
            produced = 42;
            barrier.SignalAndWait();
        }) { IsBackground = true };

        var consumer = new Thread(() =>
        {
            barrier.SignalAndWait();
            observed = produced;
        }) { IsBackground = true };

        producer.Start();
        consumer.Start();
        producer.Join();
        consumer.Join();

        Assert.AreEqual(42, observed);
    }

    [TestMethod]
    public void MonitorLock_TimeBounded_DoesNotStarveAnyThread()
    {
        const int threadCount = 8;
        var sharedLock = new object();
        int[] perThreadCount = new int[threadCount];
        using var stop = new ManualResetEventSlim(false);

        var threads = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            int idx = i;
            threads[i] = new Thread(() =>
            {
                while (!stop.IsSet)
                {
                    lock (sharedLock)
                    {
                        perThreadCount[idx]++;
                    }
                }
            }) { IsBackground = true };
        }

        foreach (var t in threads) t.Start();
        Thread.Sleep(300);
        stop.Set();
        foreach (var t in threads) t.Join();

        int min = perThreadCount.Min();
        Assert.IsGreaterThan(0, min);
    }
}