namespace Lyt.Avalonia.Mvvm.Behaviors.DragMove;

using global::Avalonia.Input;

/// <summary> Behaviour for objects that are dragged around, but dropped. </summary>
public sealed class DragMovable(Canvas canvas) : BehaviorBase<View>
{
    /// <summary> Delay triggering the Long Press event on the view model.</summary>
    private const int LongPressDelay = 500; // milliseconds

    /// <summary> Minimal drag distance triggering the dragging operation.</summary>
    private const double MinimalDragDistance = 4.5; // pixels

    private readonly Canvas dragCanvas = canvas;

    private bool isPointerPressed;
    private bool isDragging;
    private PointerPoint pointerPressedPoint;
    private Point pointerStartPosition;
    private Point viewStartPosition;
    private IDragMovableViewModel? dragMovableViewModel;
    private DispatcherTimer? timer;
    private int zindex; 

    protected override void OnAttached()
    {
        var view = this.Guard();
        this.HookPointerEvents();
        this.zindex = view.GetValue<int>(Canvas.ZIndexProperty);
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
        // Debug.WriteLine("Moved");
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
            this.BeginMove(pointerEventArgs);
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

    private void BeginMove(PointerEventArgs pointerEventArgs)
    {
        // Debug.WriteLine("Try Begin Drag");
        if (this.isDragging)
        {
            return;
        }

        View view = this.GuardAssociatedObject();
        view.SetValue<int>(Canvas.ZIndexProperty, 999_999);
        this.pointerStartPosition = pointerEventArgs.GetPosition(this.dragCanvas);
        double x = view.GetValue(Canvas.LeftProperty);
        double y = view.GetValue(Canvas.TopProperty);
        // Debug.WriteLine("X " + x.ToString("F2") + " Y " + y.ToString("F2"));
        this.viewStartPosition = new Point(x, y);
        bool allowDrag = this.DragMovableViewModel.OnBeginMove(this.pointerStartPosition);
        if (!allowDrag)
        {
            // Debug.WriteLine("Dragging rejected");
            return;
        }

        // Debug.WriteLine("Drag == true ");
        this.isDragging = true;
        this.AdjustPosition(pointerEventArgs);
    }

    private void EndMove(PointerEventArgs pointerEventArgs)
    {
        // Debug.WriteLine("Drag == false");
        Point endPosition = pointerEventArgs.GetPosition(this.dragCanvas);
        this.isPointerPressed = false;
        this.isDragging = false;
        View view = this.GuardAssociatedObject();
        view.SetValue<int>(Canvas.ZIndexProperty, this.zindex);
        this.DragMovableViewModel.OnEndMove(this.pointerStartPosition, endPosition);
    }

    private void AdjustPosition(PointerEventArgs pointerEventArgs)
    {
        // Debug.WriteLine("AdjustPosition");
        Point position = pointerEventArgs.GetPosition(this.dragCanvas);
        double deltaX = position.X - this.pointerStartPosition.X;
        double deltaY = position.Y - this.pointerStartPosition.Y;

        //Debug.WriteLine("Start position: " + this.pointerStartPosition);
        //Debug.WriteLine("Pointer position: " + position);
        //Debug.WriteLine("Delta: X " + deltaX.ToString("F2") + " Y " + deltaY.ToString("F2"));

        View view = this.View;
        double x = this.viewStartPosition.X;
        double y = this.viewStartPosition.Y;
        view.SetValue(Canvas.LeftProperty, x + deltaX);
        view.SetValue(Canvas.TopProperty, y + deltaY);

        //x = view.GetValue(Canvas.LeftProperty);
        //y = view.GetValue(Canvas.TopProperty);
        //Debug.WriteLine("X " + x.ToString("F2") + " Y " + y.ToString("F2"));
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