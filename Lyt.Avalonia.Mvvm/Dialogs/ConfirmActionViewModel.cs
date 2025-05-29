namespace Lyt.Avalonia.Mvvm.Dialogs; 

public sealed partial class ConfirmActionViewModel : ViewModel<ConfirmActionView>
{
    private readonly IDialogService dialogService;
    private readonly Action<bool> onConfirm;

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private string message;

    [ObservableProperty]
    private string actionVerb;

    [ObservableProperty]
    private SolidColorBrush colorLevel;

    public ConfirmActionViewModel(ConfirmActionParameters parameters)
    {
        this.dialogService = ApplicationBase.GetRequiredService<IDialogService>();
        this.Title = parameters.Title;
        this.Message = parameters.Message;
        this.ActionVerb = parameters.ActionVerb;
        this.ColorLevel = parameters.InformationLevel.ToBrush();
        if (parameters.OnConfirm is not null)
        {
            this.onConfirm = parameters.OnConfirm;
        }
        else
        {
            throw new ArgumentException("No callback delegate for confirming action"); 
        } 
    }

    //protected override void OnViewLoaded ( )
    //    // Need to figure out why we need to do this !!!
    //    => this.View.Icon.Foreground = this.ColorLevel;

    [RelayCommand]
    public void OnAction()
    {
        this.onConfirm(true);
        this.dialogService.Dismiss();   
    }

    [RelayCommand]
    public void OnDismiss()
    {
        this.onConfirm(false);
        this.dialogService.Dismiss();   
    }
}
