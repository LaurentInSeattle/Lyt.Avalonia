namespace Lyt.Avalonia.Mvvm.Toasting;

public sealed class Toaster : IToaster, IRecipient<ToastMessage.Show>, IRecipient<ToastMessage.Dismiss>
{
    public Panel? HostPanel;

    private ToastViewModel? current;
    private bool hostPanelHitTestVisibility;

    public Toaster()
    {
        this.Subscribe<ToastMessage.Show>();
        this.Subscribe<ToastMessage.Dismiss>();
    }

    public bool BreakOnError { get; set; } = true;

    // The host panel that will show the toasts 
    public object? Host
    {
        get => this.HostPanel;
        set
        {
            if (value is not Panel panel)
            {
                throw new Exception("The Toaster Host must be a panel.");
            }

            this.HostPanel = panel;
        }
    }

    // Provide access to the View so that it can eventually moved around, re-aligned, etc
    public object? View
    {
        get
        {
            if (this.current is ToastViewModel viewModel)
            {
                return viewModel.View;
            }

            return null;
        }
    }

    public void Show(string title, string message, int dismissDelay = 10, InformationLevel toastLevel = InformationLevel.Info)
    {
        if (this.BreakOnError && toastLevel == InformationLevel.Error && Debugger.IsAttached)
        {
            Debugger.Break();
        }
        
        new ToastMessage.Show { Title = title, Message = message, Delay = dismissDelay, Level = toastLevel }.Publish();
    }

    public void Dismiss()
    {
        if (this.Host == null)
        {
            // No content control to host the toast, that could be problematic...
            if (Debugger.IsAttached) { Debugger.Break(); }
            return;
        }

        if (this.current == null)
        {
            // Nothing to dismiss, usually not really an issue
            // if (Debugger.IsAttached) { Debugger.Break(); }
            return;
        }

        if (this.Host is not Panel panel)
        {
            throw new Exception("The Toaster Host Panel is not initialized.");
        }

        // Restore the hit test status of the host panel 
        panel.IsHitTestVisible = this.hostPanelHitTestVisibility;

        ToastView? view = this.current.View;
        if (view is not null)
        {
            panel.Children.Remove(view);
        }

        new ToastMessage.OnDismiss().Publish();
    }

    public void Receive(ToastMessage.Dismiss message) => this.Dismiss();

    public void Receive(ToastMessage.Show message)
    {
        if (this.Host is not Panel panel)
        {
            // No content control to host the toast
            if (Debugger.IsAttached) { Debugger.Break(); }
            throw new Exception("The Toaster Host Panel is not initialized.");
        }

        if (this.current is null)
        {
            this.current = new ToastViewModel(this);
            _ = this.current.CreateViewAndBind();
        }

        ToastView? view = this.current.View;
        if (view is not null)
        {
            this.current.Show(message.Title, message.Message, message.Delay, message.Level);
            if (!panel.Children.Contains(view))
            {
                // Save hit test status and make the panel clickable so that we can 
                // dismiss toasts if needed.
                this.hostPanelHitTestVisibility = panel.IsHitTestVisible;
                panel.IsHitTestVisible = true;
                panel.Children.Add(view);
            }
        }
    }
}
