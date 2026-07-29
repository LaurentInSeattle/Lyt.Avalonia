using Avalonia.Controls.Metadata;
using Avalonia.Media.Imaging;
using Avalonia.Platform;


using System.Diagnostics.CodeAnalysis;
using System.Drawing;

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

    public static readonly DirectProperty<ZoomableImage, ZoomLevelCollection> ZoomLevelsProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, ZoomLevelCollection>(
            nameof(ZoomLevels),
            o => o.ZoomLevels,
            (o, v) => o.ZoomLevels = v);

    public static readonly StyledProperty<int> MinZoomProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(MinZoom), 10);

    /// <summary> Gets or sets the minimum possible zoom. </summary>
    /// <value>The zoom.</value>
    public int MinZoom
    {
        get => this.GetValue(MinZoomProperty);
        set => this.SetValue(MinZoomProperty, value);
    }

    public static readonly StyledProperty<int> MaxZoomProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(MaxZoom), 6400);

    /// <summary>
    /// Gets or sets the maximum possible zoom.
    /// </summary>
    /// <value>The zoom.</value>
    public int MaxZoom
    {
        get => this.GetValue(MaxZoomProperty);
        set => this.SetValue(MaxZoomProperty, value);
    }

    public static readonly StyledProperty<bool> ConstrainZoomOutToFitLevelProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(ConstrainZoomOutToFitLevel));

    /// <summary>
    /// Gets or sets if the zoom out should constrain to fit image as the lowest zoom level.
    /// </summary>
    public bool ConstrainZoomOutToFitLevel
    {
        get => this.GetValue(ConstrainZoomOutToFitLevelProperty);
        set => this.SetValue(ConstrainZoomOutToFitLevelProperty, value);
    }


    public static readonly DirectProperty<ZoomableImage, int> OldZoomProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, int>(
            nameof(OldZoom),
            o => o.OldZoom);

    /// <summary>
    /// Gets the previous zoom value
    /// </summary>
    /// <value>The zoom.</value>
    public int OldZoom
    {
        get => this._oldZoom;
        private set => this.SetAndRaise(OldZoomProperty, ref this._oldZoom, value);
    }

    public static readonly StyledProperty<int> ZoomProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(Zoom), 100);

    /// <summary>
    ///  Gets or sets the zoom.
    /// </summary>
    /// <value>The zoom.</value>
    public int Zoom
    {
        get => this.GetValue(ZoomProperty);
        set
        {
            int minZoom = this.MinZoom;
            if (this.ConstrainZoomOutToFitLevel)
            {
                minZoom = Math.Max(this.ZoomLevelToFit, minZoom);
            }

            int newZoom = Math.Clamp(value, minZoom, this.MaxZoom);

            int previousZoom = this.Zoom;
            if (previousZoom == newZoom)
            {
                return;
            }

            this.OldZoom = previousZoom;
            this.SetValue(ZoomProperty, newZoom);
        }
    }

    public static readonly DirectProperty<ZoomableImage, Point> PointerPositionProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, Point>(nameof(PointerPosition), o => o.PointerPosition);

    /// <summary> Gets the current pointer position </summary>
    public Point PointerPosition
    {
        get => this._pointerPosition;
        private set => this.SetAndRaise(PointerPositionProperty, ref this._pointerPosition, value);
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

}
