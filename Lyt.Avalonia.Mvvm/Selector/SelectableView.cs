namespace Lyt.Avalonia.Mvvm.Selector;

public sealed record SelectableView<TViewEnum>(
    TViewEnum ViewEnum, ViewModel PrimaryViewModel, Control? Button = null, ViewModel? SecondaryViewModel = null) 
    where TViewEnum : Enum;
