using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Logic;

namespace Presentation.Models;

public class BallListItem : INotifyPropertyChanged
{
    private int _id;
    private double _x;
    private double _y;
    private double _diameter;
    private Color _color;
    private double _velocityX;
    private double _velocityY;

    public int Id
    {
        get => _id;
        private set { if (_id != value) { _id = value; OnPropertyChanged(); } }
    }

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
        private set { if (_color != value) { _color = value; OnPropertyChanged(); } }
    }
    
    public double VelocityX
    {
        get => _velocityX;
        private set { if (_velocityX != value) { _velocityX = value; OnPropertyChanged(); } }
    }

    public double VelocityY
    {
        get => _velocityY;
        private set { if (_velocityY != value) { _velocityY = value; OnPropertyChanged(); } }
    }

    public void UpdateFrom(IBallStatus status, double scale)
    {
        Id = status.Id;
        X = status.X * scale;
        Y = status.Y * scale;
        Diameter = status.Diameter * scale;
        Color = Color.FromRgb(status.R, status.G, status.B);
        VelocityX = status.VelocityX;
        VelocityY = status.VelocityY;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}