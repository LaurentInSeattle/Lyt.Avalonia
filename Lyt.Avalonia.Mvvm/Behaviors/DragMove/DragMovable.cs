namespace Lyt.Avalonia.Mvvm.Behaviors.DragMove;

using global::Avalonia.Input;

/// <summary> Behaviour for objects that are dragged around. </summary>
public sealed class DragMovable(Canvas canvas) : BehaviorBase<View>
{
    /// <summary> Delay triggering the Long Press event on the view model.</summary>
    private const int LongPressDelay = 500; // milliseconds

    /// <summary> Minimal drag distance triggering the drag abd drop operation.</summary>
    private const double MinimalDragDistance = 4.5; // pixels

    private readonly Canvas dragCanvas = canvas;

    private bool isPointerPressed;
    private bool isDragging;
    private PointerPoint pointerPressedPoint;
    private Point startPosition; 
    private IDragMovableViewModel? dragMovableViewModel;
    private DispatcherTimer? timer;

    protected override void OnAttached()
    {
        _ = this.Guard();
        // Debug.WriteLine("DragAble | Attached to: " + this.AssociatedObject!.GetType().Name);
        this.HookPointerEvents();
    }

    protected override void OnDetaching() => this.UnhookPointerEvents();

    public View View => this.GuardAssociatedObject();

    public IDragMovableViewModel DragMovableViewModel
    {
        get => this.dragMovableViewModel is not null ?
            this.dragMovableViewModel :
            throw new InvalidOperationException("Not attached or invalid asociated object.");
        private set => this.dragMovableViewModel = value;
    }

    private View Guard()
    {
        var view = base.GuardAssociatedObject();
        if ((view.DataContext is null) ||
            (view.DataContext is not IDragMovableViewModel idragMovableViewModel))
        {
            throw new InvalidOperationException("Not attached or invalid asociated object.");
        }

        this.DragMovableViewModel = idragMovableViewModel;
        return view;
    }

    private void HookPointerEvents()
    {
        View view = this.View;
        view.PointerPressed += this.OnPointerPressed;
        view.PointerReleased += this.OnPointerReleased;
        view.PointerMoved += this.OnPointerMoved;
        view.PointerEntered += this.OnPointerEntered;
        view.PointerExited += this.OnPointerExited;
    }

    private void UnhookPointerEvents()
    {
        View view = this.View;
        view.PointerPressed -= this.OnPointerPressed;
        view.PointerReleased -= this.OnPointerReleased;
        view.PointerMoved -= this.OnPointerMoved;
        view.PointerEntered -= this.OnPointerEntered;
        view.PointerExited -= this.OnPointerExited;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs pointerEventArgs)
        => this.DragMovableViewModel.OnEntered();

    private void OnPointerExited(object? sender, PointerEventArgs pointerEventArgs)
        => this.DragMovableViewModel.OnExited();

    private void OnPointerPressed(object? sender, PointerPressedEventArgs pointerPressedEventArgs)
    {
        // Debug.WriteLine("Pressed");
        View view = this.View;
        this.isPointerPressed = true;
        this.pointerPressedPoint = pointerPressedEventArgs.GetCurrentPoint(view);
        this.StartTimer();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs pointerEventArgs)
    {
        if (!this.isPointerPressed)
        {
            this.isPointerPressed = false;
            return;
        }

        if (this.isDragging)
        {
            this.StopTimer();

            // Debug.WriteLine("Dragging...");
            this.AdjustPosition(pointerEventArgs);
            return;
        }
        else
        {
            // Debug.WriteLine("Moving...");
            View view = this.View;
            Point currentPosition = pointerEventArgs.GetPosition(view);
            double distance = Point.Distance(currentPosition, pointerPressedPoint.Position);
            if (distance <= MinimalDragDistance)
            {
                // Debug.WriteLine("Too close.");
                return;
            }

            // Drag move begins, it's not going to be a long press, therefore stop the timer.
            this.StopTimer();
            this.BeginMove(pointerEventArgs, currentPosition);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        // Debug.WriteLine("Released");
        this.StopTimer();
        this.isPointerPressed = false;
        if (this.isDragging)
        {
            // It's a Move
            this.EndMove(args);
        }
        else
        {
            // It's a Click 
            bool isRightClick = args.InitialPressMouseButton == MouseButton.Right;
            this.DragMovableViewModel.OnClicked(isRightClick);
        } 
    }

    private void BeginMove(PointerEventArgs pointerEventArgs, Point startPosition)
    {
        // Debug.WriteLine("Try Begin Drag");
        if (this.isDragging)
        {
            return;
        }

        _ = this.GuardAssociatedObject();
        bool allowDrag = this.DragMovableViewModel.OnBeginMove(this.startPosition);
        if (!allowDrag)
        {
            // Debug.WriteLine("Dragging rejected");
            return;
        }

        // Debug.WriteLine("Drag == true ");
        this.isDragging = true;
        this.startPosition = startPosition;
        this.AdjustPosition(pointerEventArgs);

        //// Launch the DragDrop task, fire and forget 
        //this.DoDragDrop(pointerEventArgs, this.dragCanvas);
    }

    private void EndMove(PointerEventArgs pointerEventArgs)
    {
        // Debug.WriteLine("Drag == false");
        View view = this.View;
        Point endPosition = pointerEventArgs.GetPosition(view);
        this.isPointerPressed = false;
        this.isDragging = false;
        this.DragMovableViewModel.OnEndMove(this.startPosition, endPosition);
    }

    private void AdjustPosition(PointerEventArgs pointerEventArgs)
    {
        // Debug.WriteLine("AdjustPosition from pointer");
        Point position = pointerEventArgs.GetPosition(this.dragCanvas);
        View view = this.View;
        Point newPosition = new(position.X + 4, position.Y + 4);
        view.SetValue(Canvas.LeftProperty, newPosition.X);
        view.SetValue(Canvas.TopProperty, newPosition.Y);
    }

    #region Timer

    private void StartTimer()
    {
        this.StopTimer();
        this.timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(LongPressDelay),
            IsEnabled = true,
        };
        this.timer.Tick += this.OnTimerTick;
    }

    private void StopTimer()
    {
        if (this.timer is not null)
        {
            this.timer.IsEnabled = false;
            this.timer.Stop();
            this.timer.Tick -= this.OnTimerTick;
            this.timer = null;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        this.StopTimer();
        if ((this.dragMovableViewModel is not null) &&
            (this.isPointerPressed) &&
            (!this.isDragging))
        {
            this.dragMovableViewModel.OnLongPress();
        }
    }

    #endregion Timer
}