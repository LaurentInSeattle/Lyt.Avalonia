namespace Lyt.Avalonia.Mvvm.Behaviors.DragDrop;

/// <summary> 
/// Behaviour for controls and views that should support visualising a potential drop location 
/// and actual dropping of the 'DragAble' objects that are dragged around. 
/// </summary>
public class DropAble(Action<IDropTarget?>? hideDropTarget = null) : BehaviorBase<View>
{
    private readonly Action<IDropTarget?>? hideDropTarget = hideDropTarget;

    protected override void OnAttached()
    {
        View view = this.GuardAssociatedObject();
        // Debug.WriteLine("DropAble | Attached to: " + this.AssociatedObject!.GetType().Name);
        global::Avalonia.Input.DragDrop.SetAllowDrop(view, true);
        view.AddHandler(global::Avalonia.Input.DragDrop.DropEvent, this.OnDrop);
    }

    protected override void OnDetaching()
    {
        if (this.AssociatedObject is View view)
        {
            global::Avalonia.Input.DragDrop.SetAllowDrop(view, false);
            view.RemoveHandler(global::Avalonia.Input.DragDrop.DropEvent, this.OnDrop);
        }
    }

    private void OnDrop(object? sender, DragEventArgs dragEventArgs)
    {
        if (this.AssociatedObject is not View view)
        {
            return;
        }

        IDropTarget? target = null;

        var dndDataTransfer = dragEventArgs.DataTransfer;
        if (dndDataTransfer is InProcessDataTransfer inProcessDataTransfer)
        {
            object dragDropObject = inProcessDataTransfer.InProcessData;
            if (dragDropObject is IDragAbleViewModel draggableBindable)
            {
                DragAble? draggable = draggableBindable.DragAble;
                if (draggable is not null)
                {
                    if (view.DataContext is IDropTarget dropTarget)
                    {
                        target = dropTarget;
                        dropTarget.OnDrop(dragEventArgs.GetPosition(view), dragDropObject);
                    }
                }
            }
        }

        this.hideDropTarget?.Invoke(target);
        dragEventArgs.Handled = true;
    }
}
