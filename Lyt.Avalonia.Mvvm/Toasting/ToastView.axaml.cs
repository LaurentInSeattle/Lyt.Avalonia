namespace Lyt.Avalonia.Mvvm.Toasting;

public partial class ToastView : UserControl, IView
{
    public ToastView()
    {
        this.InitializeComponent();
        this.SetValue(Panel.ZIndexProperty, 999);
        this.Loaded += (_, _) => this.OuterGrid.Opacity = 1.0;
    }
}
