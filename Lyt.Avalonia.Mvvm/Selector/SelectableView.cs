namespace Lyt.Avalonia.Mvvm.Selector;

public sealed record SelectableView<TViewEnum>(
    TViewEnum ViewEnum, 
    ViewModel PrimaryViewModel, 
    Control? Button = null, 
    ViewModel? SecondaryViewModel = null,
    ViewModel? TernaryViewModel = null) 
    where TViewEnum : Enum
{
    public bool IsEnabled { get; set; } = true;
}
