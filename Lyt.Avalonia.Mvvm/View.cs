namespace Lyt.Avalonia.Mvvm;

public class View : UserControl, IView
{
    public View()
    {
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
