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

    public ObservableCollection<BallListItem> Balls { get; } = new();

    public int LogicalBoardWidth => _logic.BoardWidth;
    public int LogicalBoardHeight => _logic.BoardHeight;

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
        foreach (var status in snapshot)
        {
            if (!_byId.TryGetValue(status.Id, out var item))
            {
                item = new BallListItem();
                item.UpdateFrom(status);
                _byId[status.Id] = item;
                Balls.Add(item);
            }
            else
            {
                item.UpdateFrom(status);
            }
        }
    }

    public void Dispose()
    {
        _logic.BallsChanged -= OnBallsChanged;
        _logic.Dispose();
    }
}