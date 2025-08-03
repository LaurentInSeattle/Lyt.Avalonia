namespace Lyt.Avalonia.Mvvm.Behaviors.Modal;

public sealed class DisabledOnModal : BehaviorBase<ViewModel>
{
    protected override void OnAttached()
    {
        if (this.AssociatedObject is null)
        {
            return;
        }

        var messenger = this.AssociatedObject.Messenger;
        messenger.Subscribe<ModalMessage>(this.OnModalChanged);
    }

    protected override void OnDetaching()
    {
        if ((this.AssociatedObject is not null) &&
            (this.AssociatedObject.ViewBase is Control control))
        {
            var messenger = this.AssociatedObject.Messenger;
            messenger?.Unregister(this);

            control.IsEnabled = true;
            control.Opacity = 1.0;
        }
    }

    private void OnModalChanged(ModalMessage message)
    {
        if ((this.AssociatedObject is not null) &&
            (this.AssociatedObject.ViewBase is Control control))
        {
            control.IsEnabled = message.State == ModalMessage.Modal.Leave;
            control.Opacity = message.State == ModalMessage.Modal.Leave ? 1.0 : 0.5;
        }
    }
}