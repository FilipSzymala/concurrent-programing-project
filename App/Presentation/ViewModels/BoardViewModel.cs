using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Logic;
using Presentation.Models;

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

    public ObservableCollection<BallListItem> Balls { get; } = new();

    public double AvailableWidth => _availableWidth;
    public double AvailableHeight => _availableHeight;

    public double ScaledBoardWidth => _logic.BoardWidth * Scale;
    public double ScaledBoardHeight => _logic.BoardHeight * Scale;

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

    public BoardViewModel(BallLogicApi logic)
    {
        _logic = logic;
        _logic.BallsChanged += OnBallsChanged;
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
        _lastSnapshot = null;
        _logic.Start(count);
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

    private void OnBallsChanged(object? sender, IReadOnlyList<IBallStatus> snapshot)
    {
        Dispatcher.UIThread.Post(() => Apply(snapshot));
    }

    private void Apply(IReadOnlyList<IBallStatus> snapshot)
    {
        _lastSnapshot = snapshot;
        double scale = Scale;

        foreach (var status in snapshot)
        {
            if (!_byId.TryGetValue(status.Id, out var item))
            {
                item = new BallListItem();
                _byId[status.Id] = item;
                Balls.Add(item);
            }
            item.UpdateFrom(status, scale);
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
