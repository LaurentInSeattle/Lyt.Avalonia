namespace Lyt.Avalonia.Mvvm.Selector;

public sealed record class ViewSelectMessage(int ViewEnum, object? ActivationParameter = null);
