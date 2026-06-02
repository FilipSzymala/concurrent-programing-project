using System.Threading;
using Data;
using Logic;

namespace LogicTests;

[TestClass]
public sealed class BallDragTests
{
    [TestMethod]
    public void DragBall_FirstCall_ZeroesBallVelocity()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(3);

        Assert.IsGreaterThan(0.0, data.Balls[0].Velocity.Length);

        logic.DragBall(0, 100, 100);

        Assert.AreEqual(0.0, data.Balls[0].Velocity.X);
        Assert.AreEqual(0.0, data.Balls[0].Velocity.Y);
    }

    [TestMethod]
    public void DragBall_SameBallTwice_DoesNotReZeroVelocity()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(2);

        logic.DragBall(0, 100, 100);
        data.Balls[0].SetVelocity(new Vector2D(3, 4));

        logic.DragBall(0, 150, 150);

        Assert.AreEqual(3.0, data.Balls[0].Velocity.X);
        Assert.AreEqual(4.0, data.Balls[0].Velocity.Y);
    }

    [TestMethod]
    public void DragBall_DifferentBall_ZeroesNewBallVelocity()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(3);

        logic.DragBall(0, 100, 100);
        Assert.AreEqual(0.0, data.Balls[0].Velocity.X);
        Assert.IsGreaterThan(0.0, data.Balls[1].Velocity.Length);

        logic.DragBall(1, 200, 200);

        Assert.AreEqual(0.0, data.Balls[1].Velocity.X);
        Assert.AreEqual(0.0, data.Balls[1].Velocity.Y);
    }

    [TestMethod]
    public void StopDragging_TransfersCursorMovementAsThrowVelocity()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(2);

        logic.DragBall(0, 100, 100);
        logic.DragBall(0, 103, 101);

        logic.StopDragging(0);

        Assert.AreEqual(3.0, data.Balls[0].Velocity.X, 1e-9);
        Assert.AreEqual(1.0, data.Balls[0].Velocity.Y, 1e-9);
    }

    [TestMethod]
    public void StopDragging_ThrowVelocityClampedToMaxDragSpeed()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(2);

        logic.DragBall(0, 0, 0);
        logic.DragBall(0, 1000, 0);

        logic.StopDragging(0);

        Assert.AreEqual(6.0, data.Balls[0].Velocity.Length, 1e-6);
    }

    [TestMethod]
    public void StopDragging_WhenNoBallWasDragged_DoesNothing()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(2);
        Vector2D before = data.Balls[0].Velocity;

        logic.StopDragging(0);

        Assert.AreEqual(before.X, data.Balls[0].Velocity.X);
        Assert.AreEqual(before.Y, data.Balls[0].Velocity.Y);
    }

    [TestMethod]
    public void StopDragging_DifferentBallId_LeavesActiveDragUntouched()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(3);

        logic.DragBall(0, 50, 50);
        Vector2D ball1Before = data.Balls[1].Velocity;

        logic.StopDragging(1);

        Assert.AreEqual(ball1Before.X, data.Balls[1].Velocity.X);
        Assert.AreEqual(ball1Before.Y, data.Balls[1].Velocity.Y);
    }

    [TestMethod]
    public void DragBall_ConcurrentCallsFromMultipleThreads_NoCrashAndStateConsistent()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(2);

        const int iterations = 2000;
        Exception? failure = null;

        var dragger = new Thread(() =>
        {
            try
            {
                for (int i = 0; i < iterations; i++)
                    logic.DragBall(0, i % 400, (i * 2) % 400);
            }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };

        var stopper = new Thread(() =>
        {
            try
            {
                for (int i = 0; i < iterations; i++)
                    logic.StopDragging(0);
            }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };

        dragger.Start();
        stopper.Start();
        dragger.Join();
        stopper.Join();

        if (failure != null) throw failure;
    }

    [TestMethod]
    public void StopDragging_HoldStill_BallDoesNotFly()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(2);

        logic.DragBall(0, 200, 200);
        logic.DragBall(0, 200, 200);
        logic.DragBall(0, 200, 200);

        logic.StopDragging(0);

        Assert.AreEqual(0.0, data.Balls[0].Velocity.X, 1e-9);
        Assert.AreEqual(0.0, data.Balls[0].Velocity.Y, 1e-9);
    }

    [TestMethod]
    public void StopDragging_MoveThenHold_BallStops_BecauseRecentMotionIsZero()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        var service = new BallsService(data);
        using BallLogicApi logic = service;
        logic.Start(2);

        logic.DragBall(0, 100, 100);
        logic.DragBall(0, 200, 100);
        service.ApplyPhysics();
        logic.DragBall(0, 200, 100);
        logic.DragBall(0, 200, 100);
        service.ApplyPhysics();

        logic.StopDragging(0);

        Assert.AreEqual(0.0, data.Balls[0].Velocity.X, 1e-9);
        Assert.AreEqual(0.0, data.Balls[0].Velocity.Y, 1e-9);
    }

    [TestMethod]
    public void StopDragging_MoveThroughRelease_BallGetsRecentVelocity_EvenIfPhysicsJustSampled()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        var service = new BallsService(data);
        using BallLogicApi logic = service;
        logic.Start(2);

        logic.DragBall(0, 100, 100);
        logic.DragBall(0, 103, 100);
        service.ApplyPhysics();

        logic.StopDragging(0);

        Assert.AreEqual(3.0, data.Balls[0].Velocity.X, 1e-9);
        Assert.AreEqual(0.0, data.Balls[0].Velocity.Y, 1e-9);
    }

    [TestMethod]
    public void DragBall_AndStopDragging_DoNotCrashWhenLogicAlreadyStopped()
    {
        var data = new BallsServiceTests.FakeBallData(500, 500);
        using var logic = BallLogicApi.CreateApi(data);
        logic.Start(2);
        logic.Stop();

        logic.DragBall(0, 10, 10);
        logic.StopDragging(0);
    }
}
