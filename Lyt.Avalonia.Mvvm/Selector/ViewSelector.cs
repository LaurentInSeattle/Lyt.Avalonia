namespace Lyt.Avalonia.Mvvm.Selector;

public sealed class ViewSelector<TViewEnum> where TViewEnum : Enum
{
    private readonly SelectionGroup? selector;
    private readonly IEnumerable<SelectableView<TViewEnum>> selectableViews;
    private readonly Action<TViewEnum>? onViewSelected;

    private ViewModel? activePrimaryViewModel;
    private ViewModel? activeSecondaryViewModel;

    public ViewSelector(
        IMessenger messenger,
        Panel primaryContainer,
        Panel? secondaryContainer,
        SelectionGroup? selector,
        IEnumerable<SelectableView<TViewEnum>> selectableViews,
        Action<TViewEnum>? onViewSelected)
    {
        this.selector = selector;
        this.selectableViews = selectableViews;
        this.onViewSelected = onViewSelected;

        foreach (var selectableView in selectableViews)
        {
            if (selectableView.PrimaryViewModel.ViewBase is Control primaryControl)
            {
                primaryControl.IsVisible = false;
                primaryContainer.Children.Add(primaryControl);
            }

            if (secondaryContainer is not null &&
                selectableView.SecondaryViewModel is not null &&
                selectableView.SecondaryViewModel.ViewBase is Control secondaryControl)
            {
                secondaryControl.IsVisible = false;
                secondaryContainer.Children.Add(secondaryControl);
            }
        }

        messenger.Subscribe<ViewSelectMessage>(this.OnSelect);
    }

    public static void Select(
        IMessenger messenger,
        TViewEnum viewEnum,
        object? activationParameter = null)
        => messenger.Publish(
            new ViewSelectMessage((int)(object)viewEnum, activationParameter));

    public ViewModel? CurrentPrimaryViewModel => this.activePrimaryViewModel;

    public ViewModel? CurrentSecondaryViewModel => this.activeSecondaryViewModel;

    private void OnSelect(ViewSelectMessage viewSelectMessage)
    {
        var viewEnum = (TViewEnum)(object)viewSelectMessage.ViewEnum;

        static void HideAndDeactivate(ViewModel? viewModel)
        {
            if (viewModel is null)
            {
                return;
            }

            if (viewModel.ViewBase is IView view)
            {
                view.IsVisible = false;
            }

            viewModel.Deactivate();
        }

        static void ActivateAndShow(ViewModel? viewModel, object? activationParameters)
        {
            if (viewModel is null)
            {
                return;
            }

            viewModel.Activate(activationParameters);
            if (viewModel.ViewBase is IView view)
            {
                view.IsVisible = true;
            }
        }

        // Deactivate current View models if present 
        HideAndDeactivate(this.activePrimaryViewModel);
        HideAndDeactivate(this.activeSecondaryViewModel);

        // Activate, show and flag as active the new view models
        ViewModel newPrimaryViewModel = this.PrimaryViewModelFrom(viewEnum);
        ViewModel? secondaryNewViewModel = this.SecondaryViewModelFrom(viewEnum);
        object? activationParameters = viewSelectMessage.ActivationParameter; 
        ActivateAndShow(newPrimaryViewModel, activationParameters);
        ActivateAndShow(secondaryNewViewModel, activationParameters);
        this.activePrimaryViewModel = newPrimaryViewModel;
        this.activeSecondaryViewModel = secondaryNewViewModel;

        // Reflect in the navigation toolbar the programmatic change 
        if (this.selector is not null)
        {
            Control? selectorControl = this.ControlFrom(viewEnum);
            if (selectorControl is not null &&
                selectorControl.GetType().Implements<ICanSelect>())
            {
                var canSelect = selectorControl as ICanSelect;
                this.selector.Select(canSelect!);
            }
        }

        // Invoke callback if present for potential extra processing on view change
        this.onViewSelected?.Invoke(viewEnum);
    }

    private ViewModel PrimaryViewModelFrom(TViewEnum viewEnum)
    {
        var viewModel =
            (from SelectableView<TViewEnum> selectable in this.selectableViews
             where selectable.ViewEnum.Equals(viewEnum)
             select selectable.PrimaryViewModel).FirstOrDefault();
        return viewModel is not null ?
            viewModel :
            throw new ArgumentException("No such view", nameof(viewEnum));
    }

    private ViewModel? SecondaryViewModelFrom(TViewEnum viewEnum)
    {
        var selectableView =
            (from SelectableView<TViewEnum> selectable in this.selectableViews
             where selectable.ViewEnum.Equals(viewEnum)
             select selectable).FirstOrDefault();
        return selectableView is not null ?
            selectableView.SecondaryViewModel :
            throw new ArgumentException("No such view", nameof(viewEnum));
    }

    private Control? ControlFrom(TViewEnum viewEnum)
    {
        var selectableView =
            (from SelectableView<TViewEnum> selectable in this.selectableViews
             where selectable.ViewEnum.Equals(viewEnum)
             select selectable).FirstOrDefault();
        return selectableView is not null ?
            selectableView.Button :
            throw new ArgumentException("No such view", nameof(viewEnum));
    }
}