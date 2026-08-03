namespace Lyt.Avalonia.Mvvm.Toasting;

public sealed partial class ToastViewModel(IToaster toaster) : ViewModel<ToastView> 
{
    private const int NoDelay = 0;
    private const int MinDelay = 1_000;
    private const int MaxDelay = 60_000;

    private readonly IToaster toaster = toaster;

    [ObservableProperty]
    public partial SolidColorBrush ColorLevel { get; set; } = InformationLevel.Info.ToBrush();

    [ObservableProperty]
    public partial StreamGeometry IconGeometry { get; set; } = InformationLevel.Info.ToIconGeometry();

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Message { get; set; }

    private DispatcherTimer? dismissTimer;

    public void Show(string title, string message, int dismissDelay, InformationLevel toastLevel)
    {
        this.Title = title;
        this.Message = message;
        this.IconGeometry = toastLevel.ToIconGeometry();
        this.ColorLevel = toastLevel.ToBrush();
        string loggedMessage =
            "Toast: " + toastLevel.ToString() + " - " + title + " - " + message;
        if (toastLevel == InformationLevel.Error)
        {
            this.Logger.Error(loggedMessage);
        }
        else if (toastLevel == InformationLevel.Warning)
        {
            this.Logger.Warning(loggedMessage);
        }
        else

        {
            this.Logger.Info(loggedMessage);
        }

        if (dismissDelay == ToastViewModel.NoDelay)
        {
            // dismiss on click or explicit request 
        }
        else
        {
            // auto dismiss after delay
            if (dismissDelay < ToastViewModel.MinDelay)
            {
                dismissDelay = ToastViewModel.MinDelay;
            }
            else if (dismissDelay > ToastViewModel.MaxDelay)
            {
                dismissDelay = ToastViewModel.MaxDelay;
            }

            this.StopTimer();
            this.dismissTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(dismissDelay),
                IsEnabled = true,
            };
            this.dismissTimer.Tick += this.DismissTimerTick;
        }
    }

    private void DismissTimerTick(object? _, EventArgs e) => this.Dismiss();

    [RelayCommand]
    public void OnDismiss() => this.Dismiss();

    public void Dismiss()
    {
        this.StopTimer();
        this.toaster.Dismiss();
    }

    private void StopTimer()
    {
        this.dismissTimer?.Stop();
        this.dismissTimer = null;
    }
}
