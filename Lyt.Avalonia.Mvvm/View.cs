namespace Lyt.Avalonia.Mvvm;

public class View : UserControl, IView, ISupportBehaviors
{
    public List<object> Behaviors { get; private set; } = [];

    public View()
    {
        var methodInfo = this.GetType().GetMethod("InitializeComponent");
        if (methodInfo is not null)
        {
            _ = methodInfo.Invoke(this, [true]);
        }

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
