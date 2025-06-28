namespace Lyt.Avalonia.Mvvm.Selector;

public sealed class ViewSelector<TViewEnum> where TViewEnum : Enum
{
    private readonly ContentControl primaryContainer;
    private readonly ContentControl secondaryContainer;
    private readonly SelectionGroup selector;
    private readonly IEnumerable<SelectableView<TViewEnum>> selectableViews;
    private readonly Action<TViewEnum>? onViewSelected;

    public ViewSelector(
        IMessenger messenger,
        ContentControl primaryContainer,
        ContentControl secondaryContainer,
        SelectionGroup selector,
        IEnumerable<SelectableView<TViewEnum>> selectableViews,
        Action<TViewEnum> onViewSelected)
    {
        this.primaryContainer = primaryContainer;
        this.secondaryContainer = secondaryContainer;
        this.selector = selector;
        this.selectableViews = selectableViews;
        this.onViewSelected = onViewSelected;

        messenger.Subscribe<ViewSelectMessage>(this.OnSelect);
    }

    public static void Select(
        IMessenger messenger,
        TViewEnum viewEnum,
        object? activationParameter = null)
        => messenger.Publish(
            new ViewSelectMessage((int)(object)viewEnum, activationParameter));

    public ViewModel? CurrentViewModel ()
    {
        object? currentView = this.primaryContainer.Content;
        if (currentView is Control control && control.DataContext is ViewModel currentViewModel)
        {
            return currentViewModel;
        }

        return null;
    }

    private void OnSelect(ViewSelectMessage viewSelectMessage)
    {
        var viewEnum = (TViewEnum)(object)viewSelectMessage.ViewEnum;

        // Deactivate current VM if present 
        var newViewModel = this.PrimaryViewModelFrom(viewEnum);
        object? currentView = this.primaryContainer.Content;
        if (currentView is Control control && control.DataContext is ViewModel currentViewModel)
        {
            if (newViewModel == currentViewModel)
            {
                return;
            }

            currentViewModel.Deactivate();
        }

        // Setup secondary content if present 
        ViewModel? secondaryViewModel = this.SecondaryViewModelFrom(viewEnum);
        this.secondaryContainer.Content =
            secondaryViewModel is null ? null : secondaryViewModel.ViewBase;

        // Setup primary content 
        newViewModel.Activate(viewSelectMessage.ActivationParameter);
        this.primaryContainer.Content = newViewModel.ViewBase;

        // Reflect in the navigation toolbar the programmatic change 
        Control? selectorControl = this.ControlFrom(viewEnum);
        if (selectorControl is not null &&
            selectorControl.GetType().Implements<ICanSelect>())
        {
            var canSelect = selectorControl as ICanSelect;
            this.selector.Select(canSelect!);
        }

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