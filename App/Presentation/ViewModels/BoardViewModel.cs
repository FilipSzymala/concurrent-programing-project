using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Logic;
using Presentation.Models;
using Presentation.Views;

namespace Presentation.ViewModels;

public partial class BoardViewModel : ViewModelBase, IDisposable
{
    private readonly BallLogicApi _logic;
    private readonly Dictionary<int, BallListItem> _byId = new();

    private string _ballsCountText = BallLogicApi.DefaultBallsCount.ToString();
    private string _statusMessage = string.Empty;
    private double _availableWidth;
    private double _availableHeight;
    private IReadOnlyList<IBallStatus>? _lastSnapshot;
    private BallListItem? _selectedBallDetails;
    private double _averageSpeed;
    private string _simulationToggleLabel = "Resume";

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
    {
        _logic = logic;
        _logic.BallsChanged += OnBallsChanged;
        
        ToggleMoveSimulationCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Start()
    {
        int count = ResolveBallsCount(_ballsCountText, out string message);
        StatusMessage = message;
        BallsCountText = count.ToString();

        _logic.Stop();
        _byId.Clear();
        Balls.Clear();
        
        ToggleMoveSimulationCommand.NotifyCanExecuteChanged();
        
        SelectedBallDetails = null;
        _lastSnapshot = null;
        _logic.Start(count);
        UpdateToggleLabel();
        OnPropertyChanged(nameof(BallsCount));
    }

    internal static int ResolveBallsCount(string text, out string message)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            message = $"Brak liczby kulek — użyto wartości domyślnej {BallLogicApi.DefaultBallsCount}.";
            return BallLogicApi.DefaultBallsCount;
        }

        if (!int.TryParse(text.Trim(), out int parsed))
        {
            message = $"\"{text}\" to nie liczba — użyto wartości domyślnej {BallLogicApi.DefaultBallsCount}.";
            return BallLogicApi.DefaultBallsCount;
        }

        if (parsed < BallLogicApi.MinBallsCount)
        {
            message = $"Minimalna liczba kulek to {BallLogicApi.MinBallsCount} — wartość została podniesiona.";
            return BallLogicApi.MinBallsCount;
        }

        message = string.Empty;
        return parsed;
    }

    [RelayCommand]
    private void Stop() => _logic.Stop();

    [RelayCommand]
    private void Resume() => _logic.Resume();

    [RelayCommand(CanExecute = nameof(CanToggleMoveSimulation))]
    private void ToggleMoveSimulation()
    {
        _logic.Toggle();
        UpdateToggleLabel();
    }
    
    private bool CanToggleMoveSimulation()
    {
        return Balls.Count > 0;
    }
    
    private void UpdateToggleLabel()
    {
        SimulationToggleLabel = _logic.IsRunning ? "Stop" : "Resume";
    }

    [RelayCommand]
    private void ShowBallDetails(BallListItem? ball)
    {
        SelectedBallDetails = ball;
    }

    private void OnBallsChanged(object? sender, IReadOnlyList<IBallStatus> snapshot)
    {
        Dispatcher.UIThread.Post(() => Apply(snapshot));
    }

    private void Apply(IReadOnlyList<IBallStatus> snapshot)
    {
        _lastSnapshot = snapshot;
        double scale = Scale;
        
        double totalSpeed = 0;
        
        bool isNewList = snapshot.Count > 0 && Balls.Count == 0;

        foreach (var status in snapshot)
        {
            if (!_byId.TryGetValue(status.Id, out var item))
            {
                item = new BallListItem();
                _byId[status.Id] = item;
                Balls.Add(item);
            }
            item.UpdateFrom(status, scale);
            
            double speed = Math.Sqrt(status.VelocityX * status.VelocityX + status.VelocityY * status.VelocityY);
            totalSpeed += speed;
        }
        
        if (isNewList)
        {
            ToggleMoveSimulationCommand.NotifyCanExecuteChanged();
        }

        if (snapshot.Count > 0)
        {
            AverageSpeed = totalSpeed / snapshot.Count;
        }
        else
        {
            AverageSpeed = 0;
        }
    }

    private void ReapplySnapshot()
    {
        if (_lastSnapshot != null)
            Apply(_lastSnapshot);
    }

    public void Dispose()
    {
        _logic.BallsChanged -= OnBallsChanged;
        _logic.Dispose();
    }
}
