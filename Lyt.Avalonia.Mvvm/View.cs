namespace Lyt.Avalonia.Mvvm;

public class View : UserControl, IView, ISupportBehaviors
{
    public List<object> Behaviors { get; private set; } = [];

    public View()
    {
        this.DataContextChanged += this.OnDataContextChanged;
        this.Loaded += this.OnLoaded;
    }

    protected virtual void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is ViewModel viewModel)
        {
            viewModel.OnViewLoaded();
        }
    }

    protected virtual void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (this.DataContext is ViewModel viewModel)
        {
            viewModel.BindOnDataContextChanged(this);
        }
    }
}
