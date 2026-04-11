using Avalonia;
using Avalonia.Controls;
using Presentation.ViewModels;

namespace Presentation.Helpers;

public static class SizeObserver
{
    public static readonly AttachedProperty<bool> ObserveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Observe", typeof(SizeObserver));

    static SizeObserver()
    {
        ObserveProperty.Changed.AddClassHandler<Control>(OnObserveChanged);
    }

    public static bool GetObserve(Control c) => c.GetValue(ObserveProperty);
    public static void SetObserve(Control c, bool v) => c.SetValue(ObserveProperty, v);

    private static void OnObserveChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.GetNewValue<bool>())
        {
            control.PropertyChanged += OnControlPropertyChanged;
            UpdateSize(control);
        }
        else
        {
            control.PropertyChanged -= OnControlPropertyChanged;
        }
    }

    private static void OnControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is Control c &&
            (e.Property == Visual.BoundsProperty || e.Property == StyledElement.DataContextProperty))
            UpdateSize(c);
    }

    private static void UpdateSize(Control control)
    {
        if (control.DataContext is BoardViewModel vm)
            vm.SetAvailableSize(control.Bounds.Width, control.Bounds.Height);
    }
}