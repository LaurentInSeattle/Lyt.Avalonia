using Avalonia.Controls.Metadata;

using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Color = Avalonia.Media.Color;
using Key = Avalonia.Input.Key;
using Pen = Avalonia.Media.Pen;
using Point = Avalonia.Point;
using Size = Avalonia.Size;

namespace Lyt.Avalonia.Controls.Images;

[TemplatePart("PART_MainGrid", typeof(Grid))]
[TemplatePart("PART_ContentPresenter", typeof(ScrollContentPresenter))] // ViewPort
[TemplatePart("PART_HorizontalScrollBar", typeof(ScrollBar))]
[TemplatePart("PART_VerticalScrollBar", typeof(ScrollBar))]
[TemplatePart("PART_ScrollBarsSeparator", typeof(Panel))]
public partial class ZoomableImage : TemplatedControl, IScrollable
{
    public static readonly StyledProperty<Bitmap?> ImageProperty =
        AvaloniaProperty.Register<ZoomableImage, Bitmap?>(nameof(Image));

    /// <summary> Gets or sets the image to be displayed </summary>
    public Bitmap? Image
    {
        get => this.GetValue(ImageProperty);
        set => this.SetValue(ImageProperty, value);
    }

    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(ShowGrid), true);

    /// <summary> Gets or sets the pixel grid visibility when reach high zoom levels </summary>
    public bool ShowGrid
    {
        get => this.GetValue(ShowGridProperty);
        set => this.SetValue(ShowGridProperty, value);
    }

    public static readonly StyledProperty<bool> AutoPanProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(AutoPan), true);

    /// <summary> Gets or sets if the control can pan with the mouse </summary>
    public bool AutoPan
    {
        get => this.GetValue(AutoPanProperty);
        set => this.SetValue(AutoPanProperty, value);
    }

    public static readonly StyledProperty<MouseButtons> PanWithMouseButtonsProperty =
        AvaloniaProperty.Register<ZoomableImage, MouseButtons>(nameof(PanWithMouseButtons), MouseButtons.LeftButton | MouseButtons.MiddleButton | MouseButtons.RightButton);

    /// <summary> Gets or sets the mouse buttons to pan the image </summary>
    public MouseButtons PanWithMouseButtons
    {
        get => this.GetValue(PanWithMouseButtonsProperty);
        set => this.SetValue(PanWithMouseButtonsProperty, value);
    }

    public static readonly StyledProperty<int> PanOffsetProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(PanOffset), 20);

    /// <summary> Gets or sets the pan offset to displace everytime a key is pressed </summary>
    public int PanOffset
    {
        get => this.GetValue(PanOffsetProperty);
        set => this.SetValue(PanOffsetProperty, value);
    }

    public static readonly StyledProperty<bool> PanWithArrowsProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(PanWithArrows), true);


    public static readonly StyledProperty<MouseButtons> SelectWithMouseButtonsProperty =
        AvaloniaProperty.Register<ZoomableImage, MouseButtons>(nameof(SelectWithMouseButtons), MouseButtons.LeftButton | MouseButtons.RightButton);

    /// <summary>
    /// Gets or sets the mouse buttons to select a region on image
    /// </summary>
    public MouseButtons SelectWithMouseButtons
    {
        get => this.GetValue(SelectWithMouseButtonsProperty);
        set => this.SetValue(SelectWithMouseButtonsProperty, value);
    }

    public static readonly StyledProperty<bool> InvertMousePanProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(InvertMousePan), false);

    /// <summary> Gets or sets if mouse pan is inverted </summary>
    public bool InvertMousePan
    {
        get => this.GetValue(InvertMousePanProperty);
        set => this.SetValue(InvertMousePanProperty, value);
    }

    public static readonly StyledProperty<bool> AutoCenterProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(AutoCenter), true);

    /// <summary> Gets or sets if image is auto centered </summary>
    public bool AutoCenter
    {
        get => this.GetValue(AutoCenterProperty);
        set => this.SetValue(AutoCenterProperty, value);
    }

    public static readonly StyledProperty<SizeModes> SizeModeProperty =
        AvaloniaProperty.Register<ZoomableImage, SizeModes>(nameof(SizeMode), SizeModes.Normal);

    /// <summary> Gets or sets the image size mode </summary>
    public SizeModes SizeMode
    {
        get => this.GetValue(SizeModeProperty);
        set => this.SetValue(SizeModeProperty, value);
    }

    public static readonly StyledProperty<int> HorizontalScrollWithMouseFactorProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(HorizontalScrollWithMouseFactor), 100);

    /// <summary>
    /// Gets or sets the factor over the delta to scroll horizontally with the mouse (Left and right button on supported mice).
    /// </summary>
    /// <remarks>Set to 0 to disable horizontal scroll with mouse buttons.</remarks>
    public int HorizontalScrollWithMouseFactor
    {
        get => this.GetValue(HorizontalScrollWithMouseFactorProperty);
        set => this.SetValue(HorizontalScrollWithMouseFactorProperty, value);
    }

    public static readonly StyledProperty<int> HorizontalScrollWithMouseAlternativeFactorProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(HorizontalScrollWithMouseAlternativeFactor), 50);

    /// <summary>
    /// Gets or sets the alternative (ALT modifier) factor over the delta to scroll horizontally with the mouse (Left and right button on supported mice).
    /// </summary>
    /// <remarks>Set to 0 to disable the alternative horizontal scroll with mouse buttons.</remarks>
    public int HorizontalScrollWithMouseAlternativeFactor
    {
        get => this.GetValue(HorizontalScrollWithMouseAlternativeFactorProperty);
        set => this.SetValue(HorizontalScrollWithMouseAlternativeFactorProperty, value);
    }


    public static readonly StyledProperty<int> VerticalScrollWithMouseFactorProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(VerticalScrollWithMouseFactor), 100);

    public static readonly StyledProperty<KeyModifiers?> VerticalScrollWithMouseWheelKeyModifierProperty =
        AvaloniaProperty.Register<ZoomableImage, KeyModifiers?>(nameof(VerticalScrollWithMouseWheelKeyModifier), KeyModifiers.Control);

    /// <summary>
    /// Gets or sets the required <see cref="KeyModifiers"/> to enable the vertical scroll with the mouse wheel.
    /// </summary>
    /// <remarks>Set <c>null</c> to disable vertical scroll with the mouse wheel.</remarks>
    public KeyModifiers? VerticalScrollWithMouseWheelKeyModifier
    {
        get => this.GetValue(VerticalScrollWithMouseWheelKeyModifierProperty);
        set => this.SetValue(VerticalScrollWithMouseWheelKeyModifierProperty, value);
    }

    /// <summary>
    /// Gets or sets the factor over the delta to scroll vertically with the mouse wheel.
    /// </summary>
    /// <remarks>Set to 0 to disable horizontal scroll with mouse buttons.</remarks>
    public int VerticalScrollWithMouseFactor
    {
        get => this.GetValue(VerticalScrollWithMouseFactorProperty);
        set => this.SetValue(VerticalScrollWithMouseFactorProperty, value);
    }

    public static readonly StyledProperty<int> VerticalScrollWithMouseAlternativeFactorProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(VerticalScrollWithMouseAlternativeFactor), 50);

    /// <summary>
    /// Gets or sets the alternative (ALT modifier) factor over the delta to scroll vertically with the mouse wheel.
    /// </summary>
    /// <remarks>Set to 0 to disable the alternative horizontal scroll with mouse buttons.</remarks>
    public int VerticalScrollWithMouseAlternativeFactor
    {
        get => this.GetValue(VerticalScrollWithMouseAlternativeFactorProperty);
        set => this.SetValue(VerticalScrollWithMouseAlternativeFactorProperty, value);
    }

    public static readonly StyledProperty<MouseWheelZoomBehaviours> ZoomWithMouseWheelBehaviourProperty =
        AvaloniaProperty.Register<ZoomableImage, MouseWheelZoomBehaviours>(nameof(ZoomWithMouseWheelBehaviour), MouseWheelZoomBehaviours.ZoomNativeAltLevels);

    /// <summary>
    /// Gets or sets the mouse wheel behaviour.
    /// </summary>
    public MouseWheelZoomBehaviours ZoomWithMouseWheelBehaviour
    {
        get => this.GetValue(ZoomWithMouseWheelBehaviourProperty);
        set => this.SetValue(ZoomWithMouseWheelBehaviourProperty, value);
    }

    public static readonly StyledProperty<KeyModifiers> ZoomWithMouseWheelKeyModifierProperty =
        AvaloniaProperty.Register<ZoomableImage, KeyModifiers>(nameof(ZoomWithMouseWheelKeyModifier));

    /// <summary>
    /// Gets or sets the required <see cref="KeyModifiers"/> to work with any of <see cref="ZoomWithMouseWheelBehaviour"/> zoom behaviours.
    /// </summary>
    /// <remarks>Set <para>null</para> to ignore modifiers.</remarks>
    public KeyModifiers ZoomWithMouseWheelKeyModifier
    {
        get => this.GetValue(ZoomWithMouseWheelKeyModifierProperty);
        set => this.SetValue(ZoomWithMouseWheelKeyModifierProperty, value);
    }

    public static readonly StyledProperty<bool> ZoomWithMouseWheelStrictKeyModifierProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(ZoomWithMouseWheelStrictKeyModifier));

    /// <summary>
    /// Gets or sets to use strict key modifier to work with <see cref="ZoomWithMouseWheelKeyModifier"/>.
    /// When true it will check if modifiers exactly match the required modifier,
    /// otherwise it will perform a bitwise inclusion check.
    /// </summary>
    public bool ZoomWithMouseWheelStrictKeyModifier
    {
        get => this.GetValue(ZoomWithMouseWheelStrictKeyModifierProperty);
        set => this.SetValue(ZoomWithMouseWheelStrictKeyModifierProperty, value);
    }

    public static readonly StyledProperty<int> ZoomWithMouseWheelDebounceMillisecondsProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(ZoomWithMouseWheelDebounceMilliseconds), 20);

    /// <summary> Gets or sets the debounce milliseconds to perform zoom with mouse wheel </summary>
    public int ZoomWithMouseWheelDebounceMilliseconds
    {
        get => this.GetValue(ZoomWithMouseWheelDebounceMillisecondsProperty);
        set => this.SetValue(ZoomWithMouseWheelDebounceMillisecondsProperty, value);
    }

    private ulong _lastZoomWithMouseWheelTimestamp;

    //public static readonly DirectProperty<ZoomableImage, ZoomLevelCollection> ZoomLevelsProperty =
    //    AvaloniaProperty.RegisterDirect<ZoomableImage, ZoomLevelCollection>(
    //        nameof(ZoomLevels),
    //        o => o.ZoomLevels,
    //        (o, v) => o.ZoomLevels = v);

    public static readonly StyledProperty<double> MinZoomProperty =
        AvaloniaProperty.Register<ZoomableImage, double>(nameof(MinZoom), 10.0);

    /// <summary> Gets or sets the minimum possible zoom. </summary>
    public double MinZoom
    {
        get => this.GetValue(MinZoomProperty);
        set => this.SetValue(MinZoomProperty, value);
    }

    public static readonly StyledProperty<double> MaxZoomProperty =
        AvaloniaProperty.Register<ZoomableImage, double>(nameof(MaxZoom), 6400.0);

    /// <summary> Gets or sets the maximum possible zoom. </summary>
    public double MaxZoom
    {
        get => this.GetValue(MaxZoomProperty);
        set => this.SetValue(MaxZoomProperty, value);
    }

    public static readonly StyledProperty<bool> ConstrainZoomOutToFitLevelProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(ConstrainZoomOutToFitLevel));

    /// <summary> Gets or sets if the zoom out should constrain to fit image as the lowest zoom level. </summary>
    public bool ConstrainZoomOutToFitLevel
    {
        get => this.GetValue(ConstrainZoomOutToFitLevelProperty);
        set => this.SetValue(ConstrainZoomOutToFitLevelProperty, value);
    }

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<ZoomableImage, double>(nameof(Zoom), 100.0);

    /// <summary> Gets or sets the zoom. </summary>
    public double Zoom
    {
        get => this.GetValue(ZoomProperty);
        set
        {
            double minZoom = this.MinZoom;
            if (this.ConstrainZoomOutToFitLevel)
            {
                minZoom = Math.Max(this.ZoomLevelToFit, minZoom);
            }

            double newZoom = Math.Clamp(value, minZoom, this.MaxZoom);
            if (Math.Abs(this.Zoom - newZoom) < 0.001) 
            {
                return;
            }

            this.SetValue(ZoomProperty, newZoom);
        }
    }

    public static readonly StyledProperty<bool> AutoZoomToFitProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(AutoZoomToFit));

    /// <summary> Gets or sets if the zoom level should be auto set to fit when loading a new image. </summary>
    /// <remarks>Requires <see cref="SizeMode"/> to be <see cref="SizeModes.Normal"/>.</remarks>
    public bool AutoZoomToFit
    {
        get => this.GetValue(AutoZoomToFitProperty);
        set => this.SetValue(AutoZoomToFitProperty, value);
    }

    public static readonly StyledProperty<KeyGesture[]?> ZoomInKeyGesturesProperty =
        AvaloniaProperty.Register<ZoomableImage, KeyGesture[]?>(nameof(ZoomInKeyGestures),
            OperatingSystem.IsMacOS()
                ? [new KeyGesture(Key.Add, KeyModifiers.Meta), new KeyGesture(Key.OemPlus, KeyModifiers.Meta)]
                : [new KeyGesture(Key.Add, KeyModifiers.Control), new KeyGesture(Key.OemPlus, KeyModifiers.Control)]
            );

    /// <summary> Gets or sets the hot key to zoom in </summary>
    public KeyGesture[]? ZoomInKeyGestures
    {
        get => this.GetValue(ZoomInKeyGesturesProperty);
        set => this.SetValue(ZoomInKeyGesturesProperty, value);
    }

    public static readonly StyledProperty<KeyGesture[]?> ZoomOutKeyGesturesProperty =
        AvaloniaProperty.Register<ZoomableImage, KeyGesture[]?>(nameof(ZoomOutKeyGestures),
            OperatingSystem.IsMacOS()
                ? [new KeyGesture(Key.Subtract, KeyModifiers.Meta), new KeyGesture(Key.OemMinus, KeyModifiers.Meta)]
                : [new KeyGesture(Key.Subtract, KeyModifiers.Control), new KeyGesture(Key.OemMinus, KeyModifiers.Control)]
            );

    /// <summary> Gets or sets the hot key to zoom out </summary>
    public KeyGesture[]? ZoomOutKeyGestures
    {
        get => this.GetValue(ZoomOutKeyGesturesProperty);
        set => this.SetValue(ZoomOutKeyGesturesProperty, value);
    }

    public static readonly StyledProperty<KeyGesture[]?> ZoomTo100KeyGesturesProperty =
        AvaloniaProperty.Register<ZoomableImage, KeyGesture[]?>(nameof(ZoomTo100KeyGestures),
            OperatingSystem.IsMacOS()
                ? [new KeyGesture(Key.D0, KeyModifiers.Meta), new KeyGesture(Key.NumPad0, KeyModifiers.Meta)]
                : [new KeyGesture(Key.D0, KeyModifiers.Control), new KeyGesture(Key.NumPad0, KeyModifiers.Control)]
            );

    /// <summary> Gets or sets the hot key to zoom to 100% </summary>
    public KeyGesture[]? ZoomTo100KeyGestures
    {
        get => this.GetValue(ZoomTo100KeyGesturesProperty);
        set => this.SetValue(ZoomTo100KeyGesturesProperty, value);
    }

    public static readonly StyledProperty<KeyGesture[]?> ZoomToFitKeyGesturesProperty =
        AvaloniaProperty.Register<ZoomableImage, KeyGesture[]?>(nameof(ZoomToFitKeyGestures),
            OperatingSystem.IsMacOS()
                ? [new KeyGesture(Key.D0, KeyModifiers.Meta | KeyModifiers.Alt), new KeyGesture(Key.NumPad0, KeyModifiers.Meta | KeyModifiers.Alt)]
                : [new KeyGesture(Key.D0, KeyModifiers.Control | KeyModifiers.Alt), new KeyGesture(Key.NumPad0, KeyModifiers.Control | KeyModifiers.Alt)]
        );

    /// <summary> Gets or sets the hot key to zoom to fit </summary>
    public KeyGesture[]? ZoomToFitKeyGestures
    {
        get => this.GetValue(ZoomToFitKeyGesturesProperty);
        set => this.SetValue(ZoomToFitKeyGesturesProperty, value);
    }

    public static readonly DirectProperty<ZoomableImage, bool> IsPanningProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, bool>(
            nameof(IsPanning),
            o => o.IsPanning);

    /// <summary> Gets if control is currently panning </summary>
    public bool IsPanning
    {
        get => this._isPanning;
        protected set
        {
            if (!this.SetAndRaise(IsPanningProperty, ref this._isPanning, value))
            {
                return;
            }

            this._startScrollPosition = this.Offset;

            if (value)
            {
                this.Cursor = new Cursor(StandardCursorType.SizeAll);
                //this.OnPanStart(EventArgs.Empty);
            }
            else
            {
                this.Cursor = Cursor.Default;
                //this.OnPanEnd(EventArgs.Empty);
            }

            this.PseudoClasses.Set(":panning", value);
        }
    }

    /// <summary> Gets or sets if the control can pan with the keyboard arrows </summary>
    public bool PanWithArrows
    {
        get => this.GetValue(PanWithArrowsProperty);
        set => this.SetValue(PanWithArrowsProperty, value);
    }

    public static readonly StyledProperty<Key?> PanLeftKeyProperty =
        AvaloniaProperty.Register<ZoomableImage, Key?>(nameof(PanLeftKey));

    /// <summary> Gets or sets the key to pan left </summary>
    public Key? PanLeftKey
    {
        get => this.GetValue(PanLeftKeyProperty);
        set => this.SetValue(PanLeftKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanUpKeyProperty =
        AvaloniaProperty.Register<ZoomableImage, Key?>(nameof(PanUpKey));

    /// <summary> Gets or sets the key to pan up </summary>
    public Key? PanUpKey
    {
        get => this.GetValue(PanUpKeyProperty);
        set => this.SetValue(PanUpKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanRightKeyProperty =
        AvaloniaProperty.Register<ZoomableImage, Key?>(nameof(PanRightKey));

    /// <summary> Gets or sets the key to pan right </summary>
    public Key? PanRightKey
    {
        get => this.GetValue(PanRightKeyProperty);
        set => this.SetValue(PanRightKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanDownKeyProperty =
        AvaloniaProperty.Register<ZoomableImage, Key?>(nameof(PanDownKey));

    /// <summary> Gets or sets the key to pan down </summary>
    public Key? PanDownKey
    {
        get => this.GetValue(PanDownKeyProperty);
        set => this.SetValue(PanDownKeyProperty, value);
    }

    public static readonly DirectProperty<ZoomableImage, bool> IsSelectingProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, bool>(
            nameof(IsSelecting),
            o => o.IsSelecting);

    /// <summary> Gets if control is currently selecting a ROI </summary>
    public bool IsSelecting
    {
        get => this._isSelecting;
        protected set
        {
            if (!this.SetAndRaise(IsSelectingProperty, ref this._isSelecting, value))
            {
                return;
            }

            this.PseudoClasses.Set(":selecting", value);
        }
    }

    public static readonly StyledProperty<ISolidColorBrush> PixelGridColorProperty =
        AvaloniaProperty.Register<ZoomableImage, ISolidColorBrush>(nameof(PixelGridColor), Brushes.DimGray);

    /// <summary> Gets or sets the color of the pixel grid. </summary>
    public ISolidColorBrush PixelGridColor
    {
        get => this.GetValue(PixelGridColorProperty);
        set => this.SetValue(PixelGridColorProperty, value);
    }

    public static readonly StyledProperty<int> PixelGridZoomThresholdProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(PixelGridZoomThreshold), 5);

    /// <summary> Gets or sets the minimum size of zoomed pixel's before the pixel grid will be drawn </summary>
    public int PixelGridZoomThreshold
    {
        get => this.GetValue(PixelGridZoomThresholdProperty);
        set => this.SetValue(PixelGridZoomThresholdProperty, value);
    }

    public static readonly StyledProperty<SelectionModes> SelectionModeProperty =
        AvaloniaProperty.Register<ZoomableImage, SelectionModes>(nameof(SelectionMode), SelectionModes.None);

    public SelectionModes SelectionMode
    {
        get => this.GetValue(SelectionModeProperty);
        set => this.SetValue(SelectionModeProperty, value);
    }

    public static readonly StyledProperty<ISolidColorBrush> SelectionColorProperty =
        AvaloniaProperty.Register<ZoomableImage, ISolidColorBrush>(nameof(SelectionColor), new SolidColorBrush(new Color(127, 0, 128, 255)));

    public ISolidColorBrush SelectionColor
    {
        get => this.GetValue(SelectionColorProperty);
        set => this.SetValue(SelectionColorProperty, value);
    }

    public static readonly StyledProperty<Rect> SelectionRegionProperty =
        AvaloniaProperty.Register<ZoomableImage, Rect>(nameof(SelectionRegion));

    public Rect SelectionRegion
    {
        get => this.GetValue(SelectionRegionProperty);
        set
        {
            this.SetValue(SelectionRegionProperty, value);
            // this.RaisePropertyChanged(nameof(this.HasSelection));
            this.InvalidateVisual();
        }
    }
}