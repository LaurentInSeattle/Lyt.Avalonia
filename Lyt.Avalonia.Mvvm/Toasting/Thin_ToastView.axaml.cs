namespace Lyt.Avalonia.Mvvm.Toasting;

public partial class Thin_ToastView : UserControl, IView
{
    public Thin_ToastView()
    {
        this.InitializeComponent();
        this.SetValue(Panel.ZIndexProperty, 999);
        this.Loaded += (_, _) => this.OuterGrid.Opacity = 1.0;
    }
}
