using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Logic;

namespace Presentation.Models;

public class BallListItem : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private double _diameter;
    private Color _color;

    public double X
    {
        get => _x;
        private set { if (_x != value) { _x = value; OnPropertyChanged(); } }
    }

    public double Y
    {
        get => _y;
        private set { if (_y != value) { _y = value; OnPropertyChanged(); } }
    }

    public double Diameter
    {
        get => _diameter;
        private set { if (_diameter != value) { _diameter = value; OnPropertyChanged(); } }
    }

    public Color Color
    {
        get => _color;
        set { if (_color != value) { _color = value; OnPropertyChanged(); } }
    }

    public void UpdateFrom(IBallStatus status)
    {
        X = status.X;
        Y = status.Y;
        Diameter = status.Diameter;
        Color = Color.FromRgb(status.R, status.G, status.B);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}