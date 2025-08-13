namespace Lyt.Avalonia.Controls.Toggle;

public partial class ToggleSwitch : UserControl
{
    private Rectangle rectangleBackground;
    private TextBlock trueTextBlock;
    private TextBlock falseTextBlock;
    private Ellipse switchEllipse;
    private Rectangle eventingRectangle;

    private bool isOver;
    private bool isPressed;

#pragma warning disable CS8618 
    // Non-nullable field must contain a non-null value when exiting constructor.
    public ToggleSwitch()
#pragma warning restore CS8618 
    {
        void CreateChildren()
        {
            // Rectangle used as background: MUST be below everything else
            this.rectangleBackground = new Rectangle
            {
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                RadiusX = 8,
                RadiusY = 8,
                StrokeThickness = 1.0,
                Fill = new SolidColorBrush(Colors.Transparent),
            };

            this.trueTextBlock = new TextBlock()
            {
                IsHitTestVisible = false,
                TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Colors.Transparent),
            };

            this.falseTextBlock = new TextBlock()
            {
                IsHitTestVisible = false,
                TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Colors.Transparent),
            };

            this.switchEllipse = new Ellipse()
            {
                Margin = new Thickness(4),
                Height = 16.0,
                Width = 16.0,
                IsHitTestVisible = false,
            };

            // Rectangle used for eventing MUST be above everything else 
            this.eventingRectangle = new Rectangle
            {
                IsHitTestVisible = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Fill = new SolidColorBrush(Colors.Transparent),
            };
        }

        this.InitializeComponent();

        CreateChildren();

        if (this.eventingRectangle is null)
        {
            throw new Exception("SNH"); 
        }

        this.eventingRectangle.PointerPressed += this.OnPointerPressed;
        this.eventingRectangle.PointerReleased += this.OnPointerReleased;
        this.eventingRectangle.PointerEntered += this.OnPointerEnter;
        this.eventingRectangle.PointerExited += this.OnPointerLeave;
        this.eventingRectangle.PointerMoved += this.OnPointerMoved;

        this.Orientation = Orientation.Vertical;
        this.SetupVerticalLayout();
        this.Loaded += this.OnLoaded;
    }

    ~ToggleSwitch()
    {
        this.eventingRectangle.PointerPressed -= this.OnPointerPressed;
        this.eventingRectangle.PointerReleased -= this.OnPointerReleased;
        this.eventingRectangle.PointerEntered -= this.OnPointerEnter;
        this.eventingRectangle.PointerExited -= this.OnPointerLeave;
        this.eventingRectangle.PointerMoved -= this.OnPointerMoved;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (this.Orientation == Orientation.Vertical)
        {
            this.SetupVerticalLayout();
        }
        else
        {
            this.SetupHorizontalLayout();
        }

        this.ChangeTypography(this.Typography);
        this.PositionEllipse();
        this.UpdateVisualState();
        this.InvalidateVisual();
    }

    private void PositionEllipse()
    {
        if (this.Orientation == Orientation.Vertical)
        {
            this.switchEllipse.HorizontalAlignment = HorizontalAlignment.Center;
            this.switchEllipse.VerticalAlignment =
                this.Value ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        }
        else
        {
            this.switchEllipse.VerticalAlignment = VerticalAlignment.Center;
            this.switchEllipse.HorizontalAlignment =
                this.Value ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        }
    }

    // According to Forum discussion:
    // this.textBlock.Theme = value;
    // Does not work, because TextBlock is not a TemplatedControl ??? 
    private void ChangeTypography(ControlTheme typography)
    {
        this.trueTextBlock.ApplyControlTheme(typography);
        this.trueTextBlock.Text = this.TrueText;
        this.falseTextBlock.ApplyControlTheme(typography);
        this.falseTextBlock.Text = this.FalseText;
    }

    private void SetupVerticalLayout()
    {
        this.rectangleBackground.SetValue(Grid.ColumnProperty, 0);
        this.rectangleBackground.SetValue(Grid.RowProperty, 1);

        this.trueTextBlock.Margin = new Thickness(8, 0, 0, 0);
        this.trueTextBlock.SetValue(Grid.RowProperty, 0);
        this.trueTextBlock.VerticalAlignment = VerticalAlignment.Top;
        this.trueTextBlock.HorizontalAlignment = HorizontalAlignment.Left;

        this.falseTextBlock.Margin = new Thickness(8, 0, 0, 0);
        this.falseTextBlock.SetValue(Grid.RowProperty, 1);
        this.falseTextBlock.VerticalAlignment = VerticalAlignment.Bottom;
        this.falseTextBlock.HorizontalAlignment = HorizontalAlignment.Left;

        this.switchEllipse.SetValue(Grid.ColumnProperty, 0);
        this.switchEllipse.SetValue(Grid.RowProperty, 1);
        this.switchEllipse.VerticalAlignment = VerticalAlignment.Top;
        this.switchEllipse.HorizontalAlignment = HorizontalAlignment.Center;

        this.eventingRectangle.SetValue(Grid.ColumnProperty, 0);
        this.eventingRectangle.SetValue(Grid.RowProperty, 1);

        this.mainGridHorizontal.IsVisible = false;
        this.mainGridHorizontal.Children.Clear();

        this.mainGridVertical.IsVisible = true;

        Grid? innerGridVertical = null;
        foreach (var child in this.mainGridVertical.Children)
        {
            if (child is Grid grid)
            {
                innerGridVertical = grid;
            }
        }

        this.mainGridVertical.Children.Clear();
        this.mainGridVertical.Children.Add(this.rectangleBackground);
        this.mainGridVertical.Children.Add(this.switchEllipse);
        this.mainGridVertical.Children.Add(this.eventingRectangle);

        if (innerGridVertical is not null)
        {
            this.mainGridVertical.Children.Add(innerGridVertical);
            this.innerGridVertical.Children.Clear();
            this.innerGridVertical.Children.Add(this.trueTextBlock);
            this.innerGridVertical.Children.Add(this.falseTextBlock);
        }
    }

    private void SetupHorizontalLayout()
    {
        this.rectangleBackground.SetValue(Grid.ColumnProperty, 1);

        this.trueTextBlock.Margin = new Thickness(0, 0, 8, 0);
        this.trueTextBlock.SetValue(Grid.ColumnProperty, 0);
        this.trueTextBlock.VerticalAlignment = VerticalAlignment.Center;
        this.trueTextBlock.HorizontalAlignment = HorizontalAlignment.Right;

        this.falseTextBlock.Margin = new Thickness(8, 0, 0, 0);
        this.falseTextBlock.SetValue(Grid.ColumnProperty, 2);
        this.falseTextBlock.VerticalAlignment = VerticalAlignment.Center;
        this.falseTextBlock.HorizontalAlignment = HorizontalAlignment.Left;

        this.switchEllipse.SetValue(Grid.ColumnProperty, 1);
        this.switchEllipse.VerticalAlignment = VerticalAlignment.Center;
        this.switchEllipse.HorizontalAlignment = HorizontalAlignment.Left;

        this.eventingRectangle.SetValue(Grid.ColumnProperty, 1);

        this.mainGridVertical.IsVisible = false;
        this.mainGridVertical.Children.Clear();

        this.mainGridHorizontal.IsVisible = true;
        this.mainGridHorizontal.Children.Clear();
        this.mainGridHorizontal.Children.Add(this.rectangleBackground);
        this.mainGridHorizontal.Children.Add(this.trueTextBlock);
        this.mainGridHorizontal.Children.Add(this.falseTextBlock);
        this.mainGridHorizontal.Children.Add(this.switchEllipse);
        this.mainGridHorizontal.Children.Add(this.eventingRectangle);
    }

    #region Visual States 

    private bool IsHot => !this.isPressed && this.isOver && !this.IsDisabled;

    private void UpdateVisualState()
    {
        if ((this.GeneralVisualState is null) ||
            (this.BackgroundVisualState is null) ||
            (this.BackgroundBorderVisualState is null))
        {
            return;
        }

        this.eventingRectangle.Fill = Brushes.Transparent;
        if (this.isPressed && !this.IsDisabled)
        {
            this.SetPressedVisualState();
        }
        else if (this.IsHot)
        {
            this.SetHotVisualState();
        }
        else
        {
            if (this.IsDisabled)
            {
                this.SetDisabledVisualState();
            }
            else
            {
                this.SetNormalVisualState();
            }
        }
    }

    private void SetPressedVisualState()
    {
        var pressedColor = this.GeneralVisualState.Pressed;
        var disabledColor = this.GeneralVisualState.Disabled;
        this.switchEllipse.Fill = pressedColor;
        if (this.Value)
        {
            this.trueTextBlock.Foreground = disabledColor;
            this.falseTextBlock.Foreground = pressedColor;
        }
        else
        {
            this.trueTextBlock.Foreground = pressedColor;
            this.falseTextBlock.Foreground = disabledColor;
        }

        this.rectangleBackground.Fill = this.BackgroundVisualState.Pressed;
        this.rectangleBackground.Stroke = this.BackgroundBorderVisualState.Pressed;
    }

    private void SetHotVisualState()
    {
        var hotColor = this.GeneralVisualState.Hot;
        var disabledColor = this.GeneralVisualState.Disabled;
        this.switchEllipse.Fill = hotColor;
        if (this.Value)
        {
            this.trueTextBlock.Foreground = hotColor;
            this.falseTextBlock.Foreground = disabledColor;
        }
        else
        {
            this.trueTextBlock.Foreground = disabledColor;
            this.falseTextBlock.Foreground = hotColor;
        }

        this.rectangleBackground.Fill = this.BackgroundVisualState.Hot;
        this.rectangleBackground.Stroke = this.BackgroundBorderVisualState.Hot;
    }

    private void SetNormalVisualState()
    {
        var normalColor = this.GeneralVisualState.Normal;
        var disabledColor = this.GeneralVisualState.Disabled;
        var selectedColor = this.GeneralVisualState.Selected;
        if (this.Value)
        {
            this.trueTextBlock.Foreground = selectedColor;
            this.falseTextBlock.Foreground = disabledColor;
            this.switchEllipse.Fill = selectedColor;
        }
        else
        {
            this.trueTextBlock.Foreground = disabledColor;
            this.falseTextBlock.Foreground = normalColor;
            this.switchEllipse.Fill = normalColor;
        }

        this.rectangleBackground.Fill = this.BackgroundVisualState.Normal;
        this.rectangleBackground.Stroke = this.BackgroundBorderVisualState.Normal;
    }

    private void SetDisabledVisualState()
    {
        var disabledColor = this.GeneralVisualState.Disabled;
        this.trueTextBlock.Foreground = disabledColor;
        this.falseTextBlock.Foreground = disabledColor;
        this.switchEllipse.Fill = disabledColor;
        this.rectangleBackground.Fill = this.BackgroundVisualState.Disabled;
        this.rectangleBackground.Stroke = this.BackgroundBorderVisualState.Disabled;
    }

    #endregion Visual States 

    #region Pointer Handling

    private void OnPointerEnter(object? sender, PointerEventArgs args)
    {
        // Debug.WriteLine("Pointer Enter");
        if (this.eventingRectangle.IsPointerOver)
        {
            this.Enter();
        }
    }

    private void OnPointerLeave(object? sender, PointerEventArgs args)
    {
        // Debug.WriteLine("Pointer Leave");
        // Debugger.Break();
        if (!this.eventingRectangle.IsPointerOver)
        {
            this.Leave();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs args)
    {
        // Debug.WriteLine("Pointer Pressed");
        if (this.eventingRectangle.IsPointerOver)
        {
            this.Down();
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        // Debug.WriteLine("Pointer Released");
        if (this.eventingRectangle.IsPointerOver)
        {
            this.Up(args);
        }
        else
        {
            this.Leave();
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs args)
    {
        // Debug.WriteLine("Pointer Moved");
        if (!this.isOver)
        {
            // Debug.WriteLine("Not Over: ignore");
            return;
        }

        if (!this.eventingRectangle.IsPointerInside(args))
        {
            // Debug.WriteLine("Pointer outside : leave");
            this.Leave();
        }

        // Debug.WriteLine("Pointer inside : ignore");
    }

    private void Enter()
    {
        // Debug.WriteLine("Enter");
        this.isOver = true;
        this.UpdateVisualState();
    }

    private void Leave()
    {
        bool needToLeave = this.isOver || this.isPressed;
        if (!needToLeave)
        {
            // Debug.WriteLine("No need to Leave, return");
            return;
        }

        // Debug.WriteLine("Leave");
        this.isOver = false;
        this.isPressed = false;
        this.UpdateVisualState();
    }

    private void Down()
    {
        // Debug.WriteLine("Down");
        this.isPressed = true;
        this.UpdateVisualState();
    }

    private void Up(PointerReleasedEventArgs args)
    {
        // Debug.WriteLine("Up");
        if (!this.eventingRectangle.IsPointerInside(args))
        {
            // Debug.WriteLine("Pointer outside : leave");
            this.Leave();
        }
        else
        {
            this.isPressed = false;
            this.UpdateVisualState();
            this.ActivateCommand(args);
        }
    }

    #endregion Pointer Handling

    #region Commanding 

    private void PreventMultipleClicks()
    {
        this.IsEnabled = false;
        Task.Run(async () =>
        {
            await Task.Delay(250);
            Dispatcher.UIThread.Post((Action)delegate { this.IsEnabled = true; });
        });
    }

    private void ActivateCommand(RoutedEventArgs rea)
    {
        if (this.IsDisabled)
        {
            // This should never happen
            if (Debugger.IsAttached) { Debugger.Break(); }
            return;
        }

        this.Value = !this.Value;
        this.UpdateVisualState();

        // Give precedence to the Click handler if present 
        if (this.Click != null)
        {
            this.PreventMultipleClicks();
            this.Click.Invoke(this, rea);
        }
        else if (this.Command != null)
        {
            object? tag = this.Value;
            if (this.Command.CanExecute(tag))
            {
                this.PreventMultipleClicks();
                this.Command.Execute(tag);
            }
        }
    }

    #endregion Commanding 
}
