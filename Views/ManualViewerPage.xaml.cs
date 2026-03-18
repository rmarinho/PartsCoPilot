using PartsCopilot.ViewModels;

namespace PartsCopilot.Views;

public partial class ManualViewerPage : ContentPage
{
    private double _currentScale = 1;
    private double _startScale = 1;
    private double _xOffset = 0;
    private double _yOffset = 0;

    public ManualViewerPage(ManualViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (sender is not Image image) return;

        switch (e.Status)
        {
            case GestureStatus.Started:
                _startScale = image.Scale;
                image.AnchorX = e.ScaleOrigin.X;
                image.AnchorY = e.ScaleOrigin.Y;
                break;

            case GestureStatus.Running:
                _currentScale = Math.Clamp(_startScale * e.Scale, 1, 5);
                image.Scale = _currentScale;
                break;

            case GestureStatus.Completed:
                image.AnchorX = 0.5;
                image.AnchorY = 0.5;
                break;
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (sender is not Image image || _currentScale <= 1) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                image.TranslationX = _xOffset + e.TotalX;
                image.TranslationY = _yOffset + e.TotalY;
                break;

            case GestureStatus.Completed:
                _xOffset = image.TranslationX;
                _yOffset = image.TranslationY;
                break;
        }
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Image image) return;

        // Double-tap toggles between 1x and 2.5x zoom
        if (_currentScale > 1.1)
        {
            _ = image.ScaleToAsync(1, 250, Easing.CubicOut);
            _ = image.TranslateToAsync(0, 0, 250, Easing.CubicOut);
            _currentScale = 1;
            _xOffset = 0;
            _yOffset = 0;
        }
        else
        {
            _ = image.ScaleToAsync(2.5, 250, Easing.CubicOut);
            _currentScale = 2.5;
        }
    }
}
