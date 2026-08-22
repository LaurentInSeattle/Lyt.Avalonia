namespace Lyt.Avalonia.Mvvm;


/// <summary> Strongly typed view model </summary>
public class ViewModel<TView> : ViewModel where TView : UserControl, IView, new()
{
    public ViewModel() : base() { }

    public ViewModel(TView view) : base() => this.Bind(view);

    public TView CreateViewAndBind()
    {
        var view = new TView();
        this.Bind(view);
        return view;
    }

    public bool IsBound => this.ViewBase as TView is not null;

    public TView View
    {
        get
        {
            if (this.ViewBase is null)
            {
                throw new InvalidOperationException("View is null");
            }

            if (this.ViewBase is TView view)
            {
                return view;
            }
            else
            {
                throw new InvalidOperationException("View is not of type TView");
            }
        }
    }
}
