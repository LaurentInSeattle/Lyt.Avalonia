namespace Lyt.Avalonia.Mvvm;

public class View : UserControl, IView
{
    public View()
    {
        var methodInfo = this.GetType().GetMethod("InitializeComponent");
        if (methodInfo is not null)
        {
            _ = methodInfo.Invoke(this, [true]);
        } 

        this.DataContextChanged += (s, e) =>
        {
            if (this.DataContext is not null && this.DataContext is ViewModel viewModel)
            {
                viewModel.BindOnDataContextChanged(this); 
            }
        };

        this.Loaded += (s, e) =>
        {
            if (this.DataContext is not null && this.DataContext is ViewModel viewModel)
            {
                viewModel.OnViewLoaded();
            }
        };
    }
}
