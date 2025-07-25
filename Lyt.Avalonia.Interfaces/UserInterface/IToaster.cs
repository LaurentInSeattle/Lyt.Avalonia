﻿namespace Lyt.Avalonia.Interfaces.UserInterface;

using Lyt.Framework.Interfaces.Binding;

public interface IToaster
{
    bool BreakOnError { get; set; } 

    object? Host { get; set; }

    object? View { get; }

    void Show(
        string title, string message, int dismissDelay = 10, 
        InformationLevel toastLevel = InformationLevel.Info);

    void Dismiss();
}
