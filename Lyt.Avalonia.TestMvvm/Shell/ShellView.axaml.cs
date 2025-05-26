namespace Lyt.Avalonia.TestMvvm.Shell;

using Lyt.Mvvm;

public partial class ShellView : UserControl, IView
{
    public ShellView()
    {
        this.InitializeComponent();
        this.Loaded += (s, e) =>
        {
            if (this.DataContext is ViewModel bindable)
            {
                bindable.OnViewLoaded();
            }
        };
    }
}
