namespace Lyt.Avalonia.Mvvm.Behaviors.Modal;

public sealed class DisabledOnModal : BehaviorBase<ViewModel>, IRecipient<ModalMessage>
{
    protected override void OnAttached()
    {
        if (this.AssociatedObject is null)
        {
            return;
        }

        this.Subscribe<ModalMessage>();
    }

    protected override void OnDetaching()
    {
        if ((this.AssociatedObject is not null) &&
            (this.AssociatedObject.ViewBase is Control control))
        {
            this.Unregister<ModalMessage>();

            control.IsEnabled = true;
            control.Opacity = 1.0;
        }
    }

    public void Receive(ModalMessage message) 
    {
        if ((this.AssociatedObject is not null) &&
            (this.AssociatedObject.ViewBase is Control control))
        {
            control.IsEnabled = message.State == ModalMessage.Modal.Leave;
            control.Opacity = message.State == ModalMessage.Modal.Leave ? 1.0 : 0.5;
        }
    }
}