using System.ComponentModel;
using Avalonia.Media;
using System.Runtime.CompilerServices;

namespace Presentation.Models;

public class BallListItem : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private double _velocityX;
    private double _velocityY;
    private int _diameter;
    private Color _color;

    public double X
    {
        get => _x;
        set
        {
            if (_x.Equals(value)) return;
            
            _x = value;
            OnPropertyChanged();
        }
    }

    public double Y
    {
        get => _y;
        set
        {
            if (_y.Equals(value)) return;
            
            _y = value;
            OnPropertyChanged();
        }
    }

    public double VelocityX
    {
        get => _velocityX;
        set
        {
            if (_velocityX.Equals(value)) return;

            _velocityX = value;
            OnPropertyChanged();
        }
    }
    
    public double VelocityY
    {
        get => _velocityY;
        set
        {
            if (_velocityY.Equals(value)) return;

            _velocityY = value;
            OnPropertyChanged();
        }
    }
    

    public int Diameter
    {
        get => _diameter;
        set 
        {
            if (_diameter == value) return;
            
            _diameter = value;
            OnPropertyChanged();
        }
    }
    
    public Color Color
    {
        get => _color;
        
        set
        {
            if (_color == value) return;
            
            _color = value;
            OnPropertyChanged();
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}