namespace Lyt.Avalonia.Controls.Orchestrator;

public sealed class OrchestratorHostControl : ContentControl, IOrchestratorHostControl
{
    private readonly List<IView> retainedViews = [];

    public void Initialize(IEnumerable<IView> views)
    {
        this.retainedViews.Clear();
        this.retainedViews.AddRange(views);
    } 

    public void Activate(IView view)
    {
        if (view is Control control)
        {
            this.Content = control;
            control.IsVisible = true;
        }
    }

    public void Deactivate(IView view)
    {
        if (view is Control control)
        {
            control.IsVisible = false;
        }
    }

}
