namespace Lyt.Avalonia.Mvvm.Input;

public sealed class Mouse
{
    private Window? window;

    public bool IsLeftButtonPressed { get; private set; }

    public bool IsRightButtonPressed { get; private set; }

    public bool IsStarted { get; private set; }

    public void Start(Window window)
    {
        if (this.IsStarted)
        {
            Debug.WriteLine("Keyboard monitor already started");
            return;
        }

        this.window = window;
        this.window.AddHandler(
            InputElement.PointerPressedEvent, this.OnPointerPressedEvent, RoutingStrategies.Tunnel, handledEventsToo: true);
        this.window.AddHandler(
            InputElement.PointerMovedEvent, this.OnPointerMovedEvent, RoutingStrategies.Tunnel, handledEventsToo: true);
        this.window.AddHandler(
            InputElement.PointerReleasedEvent, this.OnPointerReleasedEvent, RoutingStrategies.Tunnel, handledEventsToo: true);
        this.IsStarted = true;
    }

    public void Stop()
    {
        if (!this.IsStarted || this.window is null)
        {
            Debug.WriteLine("Mouse monitor has no window or is not started, cannot be stopped.");
            return;
        }

        this.window.RemoveHandler(InputElement.PointerPressedEvent, this.OnPointerPressedEvent);
        this.window.RemoveHandler(InputElement.PointerMovedEvent, this.OnPointerMovedEvent);
        this.window.RemoveHandler(InputElement.PointerReleasedEvent, this.OnPointerReleasedEvent);
        this.IsStarted = false;
        this.window = null;
    }

    // NOT handled or else cant type anything :( 
    // DONT => args.Handled = true;
    private void OnPointerPressedEvent(object? _, PointerPressedEventArgs args) => this.UpdateButtonStates(args);
    private void OnPointerMovedEvent(object? _, PointerEventArgs args)  => this.UpdateButtonStates(args);    
    private void OnPointerReleasedEvent(object? _, PointerReleasedEventArgs args) => this.UpdateButtonStates(args);

    private void UpdateButtonStates(PointerEventArgs args)
    {
        var properties = args.GetCurrentPoint(this.window).Properties;
        this.IsLeftButtonPressed = properties.IsLeftButtonPressed;
        this.IsRightButtonPressed = properties.IsRightButtonPressed;
    }
}