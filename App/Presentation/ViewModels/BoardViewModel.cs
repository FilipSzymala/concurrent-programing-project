using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Logic;
using Presentation.Models;
using Presentation.Views;

namespace Presentation.ViewModels;

public partial class BoardViewModel : ViewModelBase, IDisposable
{
    private static readonly IBrush GreenBrush = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
    private static readonly IBrush YellowBrush = new SolidColorBrush(Color.FromRgb(0xD2, 0x99, 0x22));
    private static readonly IBrush RedBrush = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));

    private readonly BallLogicApi _logic;
    private readonly Action<Action> _dispatch;
    private readonly Dictionary<int, BallListItem> _byId = new();

    private string _ballsCountText = BallLogicApi.DefaultBallsCount.ToString();
    private string _statusMessage = string.Empty;
    private double _availableWidth;
    private double _availableHeight;
    private IReadOnlyList<IBallStatus>? _lastSnapshot;
    private BallListItem? _selectedBallDetails;
    private double _averageSpeed;
    private double _totalKineticEnergy;
    private double _totalMomentum;
    private double _physicsTickMs;
    private double _physicsLoadPercent;
    private double _actualFps;
    private IBrush _loadBrush = GreenBrush;
    private IBrush _fpsBrush = GreenBrush;
    private string _simulationToggleLabel = "Resume";
    private int _lastBallsCount;
    private int _draggedBallId = -1;

    public ObservableCollection<BallListItem> Balls { get; } = new();

    public double AvailableWidth => _availableWidth;
    public double AvailableHeight => _availableHeight;

    public double ScaledBoardWidth => _logic.BoardWidth * Scale;
    public double ScaledBoardHeight => _logic.BoardHeight * Scale;

    public int BallsCount => Balls.Count;

    public double AverageSpeed
    {
        get => _averageSpeed;
        private set => SetProperty(ref _averageSpeed, value);
    }

    public double TotalKineticEnergy
    {
        get => _totalKineticEnergy;
        private set => SetProperty(ref _totalKineticEnergy, value);
    }

    public double TotalMomentum
    {
        get => _totalMomentum;
        private set => SetProperty(ref _totalMomentum, value);
    }

    public double PhysicsTickMs
    {
        get => _physicsTickMs;
        private set => SetProperty(ref _physicsTickMs, value);
    }

    public double PhysicsLoadPercent
    {
        get => _physicsLoadPercent;
        private set => SetProperty(ref _physicsLoadPercent, value);
    }

    public double ActualFps
    {
        get => _actualFps;
        private set => SetProperty(ref _actualFps, value);
    }

    public IBrush LoadBrush
    {
        get => _loadBrush;
        private set => SetProperty(ref _loadBrush, value);
    }

    public IBrush FpsBrush
    {
        get => _fpsBrush;
        private set => SetProperty(ref _fpsBrush, value);
    }

    public BallListItem? SelectedBallDetails
    {
        get => _selectedBallDetails;
        set => SetProperty(ref _selectedBallDetails, value);
    }

    internal double Scale
    {
        get
        {
            double usableW = Math.Max(0, _availableWidth - 4);
            double usableH = Math.Max(0, _availableHeight - 4);
            if (usableW <= 0 || usableH <= 0) return 1;
            return Math.Min(usableW / _logic.BoardWidth, usableH / _logic.BoardHeight);
        }
    }

    public void SetAvailableSize(double width, double height)
    {
        if (Math.Abs(_availableWidth - width) < 0.001 && Math.Abs(_availableHeight - height) < 0.001)
            return;

        _availableWidth = width;
        _availableHeight = height;
        OnPropertyChanged(nameof(AvailableWidth));
        OnPropertyChanged(nameof(AvailableHeight));
        OnPropertyChanged(nameof(ScaledBoardWidth));
        OnPropertyChanged(nameof(ScaledBoardHeight));
        ReapplySnapshot();
    }

    public string BallsCountText
    {
        get => _ballsCountText;
        set => SetProperty(ref _ballsCountText, value ?? string.Empty);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SimulationToggleLabel
    {
        get => _simulationToggleLabel;
        set => SetProperty(ref _simulationToggleLabel, value);
    }

    public BoardViewModel(BallLogicApi logic)
        : this(logic, action => Dispatcher.UIThread.Post(action))
    {
    }

    internal BoardViewModel(BallLogicApi logic, Action<Action> dispatch)
    {
        _logic = logic;
        _dispatch = dispatch;
        _logic.BallsChanged += OnBallsChanged;

        ToggleMoveSimulationCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        int count = ResolveBallsCount(_ballsCountText, out string message);
        StatusMessage = message;
        BallsCountText = count.ToString();

        await RunOnThreadAsync(() => _logic.Stop());

        _byId.Clear();
        Balls.Clear();
        _lastBallsCount = 0;

        ToggleMoveSimulationCommand.NotifyCanExecuteChanged();

        SelectedBallDetails = null;
        _lastSnapshot = null;

        await RunOnThreadAsync(() => _logic.Start(count));
        UpdateToggleLabel();
    }

    internal static int ResolveBallsCount(string text, out string message)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            message = $"No count provided — using the default value of {BallLogicApi.DefaultBallsCount}.";
            return BallLogicApi.DefaultBallsCount;
        }

        if (!int.TryParse(text.Trim(), out int parsed))
        {
            message = $"\"{text}\" is not a number — using the default value of {BallLogicApi.DefaultBallsCount}.";
            return BallLogicApi.DefaultBallsCount;
        }

        if (parsed < BallLogicApi.MinBallsCount)
        {
            message = $"Minimum is {BallLogicApi.MinBallsCount} balls — value was raised.";
            return BallLogicApi.MinBallsCount;
        }

        message = string.Empty;
        return parsed;
    }

    [RelayCommand]
    private Task StopAsync() => RunOnThreadAsync(() => _logic.Stop());

    [RelayCommand]
    private Task ResumeAsync() => RunOnThreadAsync(() => _logic.Resume());

    [RelayCommand(CanExecute = nameof(CanToggleMoveSimulation))]
    private async Task ToggleMoveSimulationAsync()
    {
        await RunOnThreadAsync(() => _logic.Toggle());
        UpdateToggleLabel();
    }

    private bool CanToggleMoveSimulation()
    {
        return Balls.Count > 0;
    }

    private void UpdateToggleLabel()
    {
        SimulationToggleLabel = _logic.IsRunning ? "Pause" : "Resume";
    }

    [RelayCommand]
    private void ShowBallDetails(BallListItem? ball)
    {
        SelectedBallDetails = ball;
    }

    private void OnBallsChanged(object? sender, IReadOnlyList<IBallStatus> snapshot)
    {
        _dispatch(() => Apply(snapshot));
    }

    private void Apply(IReadOnlyList<IBallStatus> snapshot)
    {
        _lastSnapshot = snapshot;
        double scale = Scale;

        double totalSpeed = 0;
        double totalEnergy = 0;
        double sumPx = 0;
        double sumPy = 0;

        bool isNewList = snapshot.Count > 0 && Balls.Count == 0;

        foreach (var status in snapshot)
        {
            if (!_byId.TryGetValue(status.Id, out var item))
            {
                item = new BallListItem();
                _byId[status.Id] = item;
                Balls.Add(item);
                
                OnPropertyChanged(nameof(BallsCount));
            }
            item.UpdateFrom(status, scale);

            double vx = status.VelocityX;
            double vy = status.VelocityY;
            double v2 = vx * vx + vy * vy;
            totalSpeed += Math.Sqrt(v2);
            totalEnergy += 0.5 * status.Mass * v2;
            sumPx += status.Mass * vx;
            sumPy += status.Mass * vy;
        }

        if (isNewList)
        {
            ToggleMoveSimulationCommand.NotifyCanExecuteChanged();
        }

        if (Balls.Count != _lastBallsCount)
        {
            _lastBallsCount = Balls.Count;
            OnPropertyChanged(nameof(BallsCount));
        }

        if (snapshot.Count > 0)
        {
            AverageSpeed = totalSpeed / snapshot.Count;
            TotalKineticEnergy = totalEnergy;
            TotalMomentum = Math.Sqrt(sumPx * sumPx + sumPy * sumPy);
        }
        else
        {
            AverageSpeed = 0;
            TotalKineticEnergy = 0;
            TotalMomentum = 0;
        }

        double tickMs = _logic.LastTickDurationMs;
        double budgetMs = _logic.PhysicsBudgetMs;
        double frameMs = _logic.LastFrameIntervalMs;
        double loadPct = budgetMs > 0 ? tickMs / budgetMs * 100.0 : 0;
        double fps = frameMs > 0 ? 1000.0 / frameMs : 0;

        PhysicsTickMs = tickMs;
        PhysicsLoadPercent = loadPct;
        ActualFps = fps;
        LoadBrush = PickLoadBrush(loadPct);
        FpsBrush = PickFpsBrush(fps);
    }

    private static IBrush PickLoadBrush(double loadPercent)
    {
        if (loadPercent >= 100) return RedBrush;
        if (loadPercent >= 70) return YellowBrush;
        return GreenBrush;
    }

    private static IBrush PickFpsBrush(double fps)
    {
        if (fps <= 0) return GreenBrush;
        if (fps < 30) return RedBrush;
        if (fps < 55) return YellowBrush;
        return GreenBrush;
    }

    private void ReapplySnapshot()
    {
        if (_lastSnapshot != null)
            Apply(_lastSnapshot);
    }

    private static Task RunOnThreadAsync(Action work)
    {
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                work();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }) { IsBackground = true };
        thread.Start();
        return tcs.Task;
    }

    public void StartDrag(int ballId, double logicX, double logicY)
    {
        _draggedBallId = ballId;
        _logic.DragBall(ballId, logicX, logicY);
    }

    public void UpdateDrag(double logicX, double logicY)
    {
        if (_draggedBallId < 0) return;
        _logic.DragBall(_draggedBallId, logicX, logicY);
    }

    public void StopDrag()
    {
        int id = _draggedBallId;
        _draggedBallId = -1;
        if (id >= 0) _logic.StopDragging(id);
    }

    public void Dispose()
    {
        _logic.BallsChanged -= OnBallsChanged;
        _logic.Dispose();
    }
}
