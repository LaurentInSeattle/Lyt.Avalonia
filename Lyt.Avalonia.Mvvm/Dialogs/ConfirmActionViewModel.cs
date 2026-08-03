namespace Lyt.Avalonia.Mvvm.Dialogs; 

public sealed partial class ConfirmActionViewModel : ViewModel<ConfirmActionView>
{
    private readonly IDialogService dialogService;
    private readonly Action<bool> onConfirm;

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial string ActionVerb { get; set; }

    [ObservableProperty]
    public partial SolidColorBrush ColorLevel { get; set; }

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

    [RelayCommand]
    public void OnAction() => this.Dismiss(confirmed: true);

    [RelayCommand]
    public void OnDismiss() => this.Dismiss(confirmed: false);

    private void Dismiss(bool confirmed)
    {
        this.onConfirm(confirmed);
        this.dialogService.Dismiss();   
    }
}
