namespace Lyt.Avalonia.TestMvvm.Shell;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lyt.Mvvm; 

public sealed partial class ShellViewModel : ViewModel<ShellView>
{
    [ObservableProperty]
    public string? buttonText;

    [ObservableProperty]
    public string? tickCount;

    [ObservableProperty]
    public string? isTicking; 

    public ShellViewModel()
    {
        this.TickCount = "Hello Avalonia!";
        this.IsTicking = "string.Empty;";
        this.ButtonText = "Click Me!"; 
    }

    [RelayCommand]
    public void OnStartStop()
    {
        this.TickCount = "Hello Avalonia!";
        Debug.WriteLine(this.TickCount);
        this.Set("Invoked Set", "TickCount");
        string? value = this.Get<string?>(nameof(this.TickCount));
        Debug.WriteLine(value);
    }

    public override void OnViewLoaded()
    {
        base.OnViewLoaded();
        Debug.WriteLine("OnViewLoaded: " + this.ButtonText);
    }
}
