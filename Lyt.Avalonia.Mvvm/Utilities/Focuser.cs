namespace Lyt.Avalonia.Mvvm.Utilities;

public sealed class Focuser : IFocuser
{
    public bool SetFocus(IView view, string fieldName)
    {
        if (view is not Control control)
        {
            return false;
        }

        // Find child control with provided name 
        Control? child = control.FindControl<Control>(fieldName);
        if ((child is null) || !child.Focusable )
        {
            return false;
        }

        return child.Focus();
        
        // This should work, but does not :( 
        // return child.IsFocused;
    }
}