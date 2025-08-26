namespace Lyt.Avalonia.Mvvm.Dialogs;

public sealed class DialogService(WeakReferenceMessenger messenger, ILogger logger) : IDialogService
{
    private readonly WeakReferenceMessenger messenger = messenger;
    private readonly ILogger logger = logger;

    private bool isClassHandlerRegistered;
    private bool modalHostPanelHitTestVisible;
    private Panel? modalHostPanel;
    private ModalHostControl? modalHostControl;
    private UserControl? modalUserControl;

    public bool IsModal
        => this.modalHostPanel is not null || this.modalUserControl is not null || this.modalHostControl is not null;

    public void Confirm(object maybePanel, ConfirmActionParameters parameters)
    {
        try
        {
            Panel panel = this.GuardPanel(maybePanel);
            var viewModel = new ConfirmActionViewModel(parameters);
            viewModel.CreateViewAndBind();
            this.ShowInternal(panel, viewModel.View);
        }
        catch (Exception ex)
        {
            this.logger.Error("Failed to launch dialog, exception thrown: \n" + ex.ToString());
            throw;
        }
    }

    public void Show<TDialog>(object maybePanel, TDialog dialog)
    {
        try
        {
            if (this.IsModal)
            {
                this.logger.Error("Already showing a modal");
                throw new InvalidOperationException("Already showing a modal");
            }

            if (maybePanel is not Panel panel)
            {
                this.logger.Error("Must provide a host panel");
                throw new InvalidOperationException("Must provide a host panel");
            }

            if (!typeof(TDialog).IsSubclassOf(typeof(UserControl)))
            {
                this.logger.Error("TDialog Must provide a type deriving from UserControl");
                throw new InvalidOperationException("TDialog Must provide a type deriving from UserControl");
            }

            if (dialog is UserControl userControl)
            {
                this.ShowInternal(panel, userControl);
            }
            else
            {
                this.logger.Error("TDialog Must provide a type deriving from UserControl");
                throw new InvalidOperationException("TDialog Must provide a type deriving from UserControl");
            }
        }
        catch (Exception ex)
        {
            this.logger.Error("Failed to launch dialog, exception thrown: \n" + ex.ToString());
            throw;
        }
    }

    /// <summary> Run a view/view model modally, when no other is doing so. </summary>
    public void RunViewModelModal<TDialog, TParameters>(
        object maybePanel,
        DialogViewModel<TDialog, TParameters> viewModel,
        Action<object, bool>? onClose = null,
        TParameters? parameters = null)
        where TDialog : UserControl, IView, new()
        where TParameters : class
    {
        try
        {
            Panel panel = this.GuardPanel(maybePanel, isReplace: false);
            viewModel.CreateViewAndBind();
            viewModel.Initialize(onClose, parameters);
            this.ShowInternal(panel, viewModel.View);
            if (!this.isClassHandlerRegistered)
            {
                ApplicationBase.MainWindow.AddHandler(
                    InputElement.KeyDownEvent, this.OnKeyDown, 
                    RoutingStrategies.Tunnel, handledEventsToo: true);
                this.isClassHandlerRegistered = true;
            }
        }
        catch (Exception ex)
        {
            this.logger.Error("Failed to launch dialog, exception thrown: \n" + ex.ToString());
            throw;
        }
    }

    /// <summary> Run a view/view model modally, when there is another doing so, closing it. </summary>
    public void ReplaceRunViewModelModal<TDialog, TParameters>(
        DialogViewModel<TDialog, TParameters> viewModel,
        Action<object, bool>? onClose = null,
        TParameters? parameters = null)
        where TDialog : UserControl, IView, new()
        where TParameters : class
    {
        try
        {
            if ((this.modalUserControl is not null) &&
                (this.modalUserControl.DataContext is ViewModel replacedViewModel))
            {
                // This will try to invoke an OnClose delegate if any is defined 
                replacedViewModel.CancelViewModel();
            }

            _ = this.GuardPanel(null, isReplace: true);
            viewModel.CreateViewAndBind();
            viewModel.Initialize(onClose, parameters);
            this.ShowInternalReplace(viewModel.View);
            if (!this.isClassHandlerRegistered)
            {
                ApplicationBase.MainWindow.AddHandler(
                    InputElement.KeyDownEvent, this.OnKeyDown, 
                    RoutingStrategies.Tunnel, handledEventsToo: true);
                this.isClassHandlerRegistered = true;
            }
        }
        catch (Exception ex)
        {
            this.logger.Error("Failed to launch dialog, exception thrown: \n" + ex.ToString());
            throw;
        }
    }


    private void OnKeyDown(object? _, KeyEventArgs args)
    {
        if ((this.IsModal) &&
            (this.modalUserControl is not null) &&
            (this.modalUserControl.DataContext is ViewModel viewModel))
        {
            if ((args.Key == Key.Escape) || (args.Key == Key.Enter))
            {
                if (viewModel.CanEscape && (args.Key == Key.Escape))
                {
                    args.Handled = true;
                    viewModel.Cancel();
                }
                else if (viewModel.CanEnter && (args.Key == Key.Enter))
                {
                    args.Handled = true;
                    viewModel.TrySaveAndClose();
                }

                // Here; We could have Key == Enter and CanEnter == false 
                // So dont move here: args.Handled = true; 
                // or else Enters are ignored 
            }
            // else : NOT handled or else cant type anything :( 
        }
    }

    public void Dismiss()
    {
        if (this.modalHostPanel is null && this.modalUserControl is null && this.modalHostControl is null)
        {
            this.logger.Warning("DialogService: Nothing to dismiss.");
            return;
        }

        if (this.modalHostPanel is null || this.modalUserControl is null || this.modalHostControl is null)
        {
            this.logger.Warning("DialogService: Inconsistent state, trying to recover and dismiss.");
        }

        try
        {
            // #1 - Remove user control from the modal host control 
            if (this.modalUserControl is not null && this.modalHostControl is not null)
            {
                bool removedDialog = this.modalHostControl.ContentGrid.Children.Remove(this.modalUserControl);
                if (!removedDialog)
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    this.logger.Warning("Failed to remove user dialog from modal host ");
                }
            }

            // #2 - Remove host control from the host panel 
            if (this.modalHostPanel is not null && this.modalHostControl is not null)
            {
                bool removedHost = this.modalHostPanel.Children.Remove(this.modalHostControl);
                if (!removedHost)
                {
                    if (Debugger.IsAttached) { Debugger.Break(); }
                    this.logger.Warning("Failed to remove modal host from host panel");
                }
            }

            // #3 - Restore host panel visibility state 
            if (this.modalHostPanel is not null)
            {
                this.modalHostPanel.IsHitTestVisible = this.modalHostPanelHitTestVisible;
            }

            // #4 - Unhook keyboard events 
            if (this.isClassHandlerRegistered)
            {
                ApplicationBase.MainWindow.RemoveHandler(InputElement.KeyDownEvent, this.OnKeyDown);
                this.isClassHandlerRegistered = false;
            }

            this.modalHostControl = null;
            this.modalUserControl = null;
            this.modalHostPanel = null;
        }
        catch (Exception ex)
        {
            this.logger.Error("Failed to dismiss dialog, exception thrown: \n" + ex.ToString());
            throw;
        }
        finally
        {
            new ModalMessage(ModalMessage.Modal.Leave).Publish();
        }
    }

    private void ShowInternal(Panel panel, UserControl dialog)
    {
        this.modalHostPanelHitTestVisible = panel.IsHitTestVisible;
        panel.IsHitTestVisible = true;
        var host = new ModalHostControl();
        panel.Children.Add(host);
        host.ContentGrid.Children.Add(dialog);
        new ModalMessage(ModalMessage.Modal.Enter).Publish();
        this.modalHostPanel = panel;
        this.modalHostControl = host;
        this.modalUserControl = dialog;
    }

    private void ShowInternalReplace(UserControl dialog)
    {
        if (this.modalUserControl is not null && this.modalHostControl is not null)
        {
            // #1 - Remove user control from the modal host control 
            bool removedDialog = this.modalHostControl.ContentGrid.Children.Remove(this.modalUserControl);
            if (!removedDialog)
            {
                if (Debugger.IsAttached) { Debugger.Break(); }
                this.logger.Warning("Failed to remove user dialog from modal host ");
            }

            // #2 - Replace with the provided user control
            this.modalHostControl.ContentGrid.Children.Add(dialog);
            this.modalUserControl = dialog;
        }
    }

    private Panel GuardPanel(object? maybePanel, bool isReplace = false)
    {
        Panel panel; 
        if (isReplace)
        {
            if (!this.IsModal)
            {
                // Nothing to replace: You should have checked if the service was modal
                this.logger.Error("Not showing a modal");
                throw new InvalidOperationException("Not showing a modal");
            }

            if ( this.modalHostPanel is not null )
            {
                panel = this.modalHostPanel;
            }
            else
            {
                this.logger.Error("No host panel");
                throw new InvalidOperationException("No host panel");
            }
        }
        else
        {
            if (this.IsModal)
            {
                // You should have checked if the service was modal, and if so, first
                // invoke 'Dismiss' before launching a new dialog 
                this.logger.Error("Already showing a modal");
                throw new InvalidOperationException("Already showing a modal");
            }

            if (maybePanel is Panel p)
            {
                panel = p;
            } 
            else
            {  
                this.logger.Error("Must provide a host panel");
                throw new InvalidOperationException("Must provide a host panel");
            }
        }

        return panel;
    }
}
