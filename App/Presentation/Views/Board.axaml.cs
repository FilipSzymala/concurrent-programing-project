using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Presentation.Models;
using Presentation.ViewModels;

namespace Presentation.Views;

public partial class Board : UserControl
{
    private ItemsControl? _boardControl;

    // Offset from cursor to ball top-left corner (screen coords) when drag started.
    private double _dragOffsetX;
    private double _dragOffsetY;
    private bool _isDragging;

    public Board()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _boardControl = this.FindControl<ItemsControl>("PART_Board");
    }

    private BoardViewModel? Vm => DataContext as BoardViewModel;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var vm = Vm;
        if (vm == null || _boardControl == null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        Point pos = e.GetPosition(_boardControl);
        BallListItem? ball = FindBallAt(pos, vm);
        if (ball == null) return;

        // Store cursor-to-ball-corner offset so the ball doesn't snap its corner to the cursor.
        _dragOffsetX = ball.X - pos.X;
        _dragOffsetY = ball.Y - pos.Y;
        _isDragging = true;

        double scale = vm.Scale;
        vm.StartDrag(ball.Id,
            (pos.X + _dragOffsetX) / scale,
            (pos.Y + _dragOffsetY) / scale);

        // Capture so we keep receiving Moved/Released even outside the control.
        e.Pointer.Capture((IInputElement?)sender);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;
        var vm = Vm;
        if (vm == null || _boardControl == null) return;

        Point pos = e.GetPosition(_boardControl);
        double scale = vm.Scale;
        vm.UpdateDrag(
            (pos.X + _dragOffsetX) / scale,
            (pos.Y + _dragOffsetY) / scale);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        e.Pointer.Capture(null);
        Vm?.StopDrag();
    }

    // find the topmost ball whose circle contains the given canvas-space point.
    private static BallListItem? FindBallAt(Point pos, BoardViewModel vm)
    {
        foreach (BallListItem ball in vm.Balls)
        {
            double r  = ball.Diameter / 2.0;
            double cx = ball.X + r;
            double cy = ball.Y + r;
            double dx = pos.X - cx;
            double dy = pos.Y - cy;
            if (dx * dx + dy * dy <= r * r)
                return ball;
        }
        return null;
    }
}