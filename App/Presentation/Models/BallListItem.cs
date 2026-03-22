using System.ComponentModel;
using Avalonia.Media;
using System.Runtime.CompilerServices;

namespace Presentation.Models;

public class BallListItem :INotifyPropertyChanged
{
    private int _x;
    private int _y;
    private int _diameter;
    private Color _color;

    public int X
    {
        get => _x;
        set
        {
            if (_x == value) return;
            
            _x = value;
            OnPropertyChanged();
        }
    }

    public int Y
    {
        get => _y;
        set
        {
            if (_y == value) return;
            
            _y = value;
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