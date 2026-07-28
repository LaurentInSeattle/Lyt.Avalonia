/*
*                               The MIT License (MIT)
* Permission is hereby granted, free of charge, to any person obtaining a copy of
* this software and associated documentation files (the "Software"), to deal in
* the Software without restriction, including without limitation the rights to
* use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
* the Software, and to permit persons to whom the Software is furnished to do so.
*/

// Port from: https://github.com/cyotek/Cyotek.Windows.Forms.ImageBox to AvaloniaUI

using Avalonia.Controls.Metadata;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using System.Diagnostics.CodeAnalysis;
using System.Drawing;

using Key = Avalonia.Input.Key;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Color = Avalonia.Media.Color;
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
    /// <summary> Multicast event for property change notifications. </summary>
    private PropertyChangedEventHandler? propertyChanged;

    public new event PropertyChangedEventHandler? PropertyChanged
    {
        add { this.propertyChanged -= value; this.propertyChanged += value; }
        remove => this.propertyChanged -= value;
    }

    protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            this.RaisePropertyChanged(propertyName);
            return false;
        } 

        return true;
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)  { }

    /// <summary> Notifies listeners that a property value has changed. </summary>
    /// <param name="propertyName">
    ///     Name of the property used to notify listeners.  This
    ///     value is optional and can be provided automatically when invoked from compilers
    ///     that support <see cref="CallerMemberNameAttribute" />.
    /// </param>
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var e = new PropertyChangedEventArgs(propertyName);
        this.OnPropertyChanged(e);
        this.propertyChanged?.Invoke(this, e);
    }

    protected internal ScrollContentPresenter? ViewPort;
    protected internal ScrollBar? HorizontalScrollBar;
    protected internal ScrollBar? VerticalScrollBar;

    private bool _imageNeedsDisposal;
    private Point _startMousePosition;
    private Vector _startScrollPosition;
    private bool _isPanning;
    private bool _isSelecting;
    private Bitmap? _trackerImage;
    private bool _canRender = true;
    private Point _pointerPosition;
    ZoomLevelCollection _zoomLevels = ZoomLevelCollection.Default;
    private int _oldZoom = 100;

    //private DrawingBrush? _gridBrush;
    
    private Pen? _pixelGridPen;
    private Pen? _selectionBorderPen;

    /// <summary> Gets the image as a writeable bitmap </summary>
    public WriteableBitmap? ImageAsWriteableBitmap => this.Image as WriteableBitmap;

    /// <summary> Returns true if image is loaded, otherwise false. </summary>
    [MemberNotNullWhen(true, nameof(Image))]
    public bool IsImageLoaded => this.Image is not null;

    public static readonly DirectProperty<ZoomableImage, Bitmap?> TrackerImageProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, Bitmap?>(
            nameof(TrackerImage),
            o => o.TrackerImage,
            (o, v) => o.TrackerImage = v);

    /// <summary> Gets or sets an image to follow the mouse pointer </summary>
    public Bitmap? TrackerImage
    {
        get => this._trackerImage;
        set
        {
            if (!this.SetAndRaise(TrackerImageProperty, ref this._trackerImage, value))
            {
                return;
            }

            this.TriggerRender();
            this.RaisePropertyChanged(nameof(this.HaveTrackerImage));
        }
    }

    [MemberNotNullWhen(true, nameof(TrackerImage))]
    [MemberNotNullWhen(true, nameof(_trackerImage))]
    public bool HaveTrackerImage => this._trackerImage is not null;

    public static readonly StyledProperty<bool> TrackerImageAutoZoomProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(TrackerImageAutoZoom), true);

    /// <summary>
    /// Gets or sets if the tracker image will be scaled to the current zoom
    /// </summary>
    public bool TrackerImageAutoZoom
    {
        get => this.GetValue(TrackerImageAutoZoomProperty);
        set => this.SetValue(TrackerImageAutoZoomProperty, value);
    }

    public bool IsHorizontalBarVisible
    {
        get
        {
            if (!this.IsImageLoaded)
            {
                return false;
            }

            if (this.SizeMode != SizeModes.Normal)
            {
                return false;
            }

            return this.ScaledImageWidth > this.Viewport.Width;
        }
    }

    public bool IsVerticalBarVisible
    {
        get
        {
            if (this.Image is null)
            {
                return false;
            }

            if (this.SizeMode != SizeModes.Normal)
            {
                return false;
            }

            return this.ScaledImageHeight > this.Viewport.Height;
        }
    }

    public static readonly DirectProperty<ZoomableImage, Point> PointerPositionProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, Point>(
            nameof(PointerPosition),
            o => o.PointerPosition);

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

    /// <summary> Gets the center point of the viewport </summary>
    public Point CenterPoint
    {
        get
        {
            var viewport = this.GetImageViewPort();
            return new(viewport.Width / 2, viewport.Height / 2);
        }
    }

    /// <summary>
    /// Gets or sets if the control can pan with the keyboard arrows
    /// </summary>
    public bool PanWithArrows
    {
        get => this.GetValue(PanWithArrowsProperty);
        set => this.SetValue(PanWithArrowsProperty, value);
    }


    public static readonly StyledProperty<Key?> PanLeftKeyProperty =
        AvaloniaProperty.Register<ZoomableImage, Key?>(nameof(PanLeftKey));

    /// <summary>
    /// Gets or sets the key to pan left
    /// </summary>
    public Key? PanLeftKey
    {
        get => this.GetValue(PanLeftKeyProperty);
        set => this.SetValue(PanLeftKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanUpKeyProperty =
        AvaloniaProperty.Register<ZoomableImage, Key?>(nameof(PanUpKey));

    /// <summary>
    /// Gets or sets the key to pan up
    /// </summary>
    public Key? PanUpKey
    {
        get => this.GetValue(PanUpKeyProperty);
        set => this.SetValue(PanUpKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanRightKeyProperty =
        AvaloniaProperty.Register<ZoomableImage, Key?>(nameof(PanRightKey));

    /// <summary>
    /// Gets or sets the key to pan right
    /// </summary>
    public Key? PanRightKey
    {
        get => this.GetValue(PanRightKeyProperty);
        set => this.SetValue(PanRightKeyProperty, value);
    }

    public static readonly StyledProperty<Key?> PanDownKeyProperty =
        AvaloniaProperty.Register<ZoomableImage, Key?>(nameof(PanDownKey));

    /// <summary>
    /// Gets or sets the key to pan down
    /// </summary>
    public Key? PanDownKey
    {
        get => this.GetValue(PanDownKeyProperty);
        set => this.SetValue(PanDownKeyProperty, value);
    }

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

    /// <summary>
    /// Gets or sets if mouse pan is inverted
    /// </summary>
    public bool InvertMousePan
    {
        get => this.GetValue(InvertMousePanProperty);
        set => this.SetValue(InvertMousePanProperty, value);
    }

    public static readonly StyledProperty<bool> AutoCenterProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(AutoCenter), true);

    /// <summary>
    /// Gets or sets if image is auto centered
    /// </summary>
    public bool AutoCenter
    {
        get => this.GetValue(AutoCenterProperty);
        set => this.SetValue(AutoCenterProperty, value);
    }

    public static readonly StyledProperty<SizeModes> SizeModeProperty =
        AvaloniaProperty.Register<ZoomableImage, SizeModes>(nameof(SizeMode), SizeModes.Normal);

    /// <summary>
    /// Gets or sets the image size mode
    /// </summary>
    public SizeModes SizeMode
    {
        get => this.GetValue(SizeModeProperty);
        set => this.SetValue(SizeModeProperty, value);
    }

    private void SizeModeChanged()
    {
        var horizontalScrollBar = this.HorizontalScrollBar;
        if (horizontalScrollBar is null)
        {
            return;
        }

        var verticalScrollBar = this.VerticalScrollBar;
        if (verticalScrollBar is null)
        {
            return;
        }

        switch (this.SizeMode)
        {
            case SizeModes.Normal:
                horizontalScrollBar.Visibility = ScrollBarVisibility.Auto;
                verticalScrollBar.Visibility = ScrollBarVisibility.Auto;
                break;
            case SizeModes.Stretch:
            case SizeModes.Fit:
                horizontalScrollBar.Visibility = ScrollBarVisibility.Hidden;
                verticalScrollBar.Visibility = ScrollBarVisibility.Hidden;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(this.SizeMode), this.SizeMode, null);
        }
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

    /// <summary>
    /// Gets or sets the debounce milliseconds to perform zoom with mouse wheel
    /// </summary>
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

    /// <summary>
    ///   Gets or sets the zoom levels.
    /// </summary>
    /// <value>The zoom levels.</value>
    public ZoomLevelCollection ZoomLevels
    {
        get => this._zoomLevels;
        set => this.SetAndRaise(ZoomLevelsProperty, ref this._zoomLevels, value);
    }

    public static readonly StyledProperty<int> MinZoomProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(MinZoom), 10);

    /// <summary>
    /// Gets or sets the minimum possible zoom.
    /// </summary>
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
            var minZoom = this.MinZoom;
            if (this.ConstrainZoomOutToFitLevel)
            {
                minZoom = Math.Max(this.ZoomLevelToFit, minZoom);
            }

            var newZoom = Math.Clamp(value, minZoom, this.MaxZoom);

            var previousZoom = this.Zoom;
            if (previousZoom == newZoom)
            {
                return;
            }

            this.OldZoom = previousZoom;
            this.SetValue(ZoomProperty, newZoom);
        }
    }

    /// <summary>
    /// <para>Gets if the image have zoom.</para>
    /// <para>True if zoomed in or out</para>
    /// <para>False if no zoom applied</para>
    /// </summary>
    public bool IsActualSize => this.Zoom == 100;

    /// <summary>
    /// Gets the zoom factor, the zoom / 100.0
    /// </summary>
    public double ZoomFactor => this.Zoom / 100.0;

    /// <summary>
    /// Gets the zoom to fit level which shows all the image
    /// </summary>
    public int ZoomLevelToFit
    {
        get
        {
            var image = this.Image;
            if (image is null)
            {
                return 100;
            }

            double zoom = Math.Min(this.Bounds.Width / image.Size.Width, this.Bounds.Height / image.Size.Height) * 100.0;
            return zoom <= 0 ? 100 : (int)zoom;
        }
    }

    public static readonly StyledProperty<bool> AutoZoomToFitProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(AutoZoomToFit));

    /// <summary>
    /// Gets or sets if the zoom level should be auto set to fit when loading a new image.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the hot key to zoom in
    /// </summary>
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

    /// <summary>
    /// Gets or sets the hot key to zoom out
    /// </summary>
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

    /// <summary>
    /// Gets or sets the hot key to zoom to 100%
    /// </summary>
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

    /// <summary>
    /// Gets or sets the hot key to zoom to fit
    /// </summary>
    public KeyGesture[]? ZoomToFitKeyGestures
    {
        get => this.GetValue(ZoomToFitKeyGesturesProperty);
        set => this.SetValue(ZoomToFitKeyGesturesProperty, value);
    }


    /// <summary>
    /// Gets the size of the scaled image.
    /// </summary>
    /// <value>The size of the scaled image.</value>
    public Size ScaledImageSize => new(this.ScaledImageWidth, this.ScaledImageHeight);

    /// <summary>
    /// Gets the width of the scaled image.
    /// </summary>
    /// <value>The width of the scaled image.</value>
    public double ScaledImageWidth => this.Image?.Size.Width * this.ZoomFactor ?? 0;

    /// <summary>
    /// Gets the height of the scaled image.
    /// </summary>
    /// <value>The height of the scaled image.</value>
    public double ScaledImageHeight => this.Image?.Size.Height * this.ZoomFactor ?? 0;

    public static readonly StyledProperty<ISolidColorBrush> PixelGridColorProperty =
        AvaloniaProperty.Register<ZoomableImage, ISolidColorBrush>(nameof(PixelGridColor), Brushes.DimGray);

    /// <summary>
    /// Gets or sets the color of the pixel grid.
    /// </summary>
    /// <value>The color of the pixel grid.</value>
    public ISolidColorBrush PixelGridColor
    {
        get => this.GetValue(PixelGridColorProperty);
        set => this.SetValue(PixelGridColorProperty, value);
    }

    public static readonly StyledProperty<int> PixelGridZoomThresholdProperty =
        AvaloniaProperty.Register<ZoomableImage, int>(nameof(PixelGridZoomThreshold), 5);

    /// <summary>
    /// Gets or sets the minimum size of zoomed pixel's before the pixel grid will be drawn
    /// </summary>
    /// <value>The pixel grid threshold.</value>

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
            //if (!RaiseAndSetIfChanged(ref _selectionRegion, value)) return;
            this.TriggerRender();
            this.RaisePropertyChanged(nameof(this.HaveSelection));
            this.RaisePropertyChanged(nameof(this.SelectionRegionNet));
            this.RaisePropertyChanged(nameof(this.SelectionRegionPixel));
        }
    }

    public System.Drawing.Rectangle SelectionRegionNet
    {
        get
        {
            var rect = this.SelectionRegion;
            return new((int)Math.Ceiling(rect.X), (int)Math.Ceiling(rect.Y), (int)rect.Width, (int)rect.Height);
        }
    }

    public PixelRect SelectionRegionPixel
    {
        get
        {
            var rect = this.SelectionRegion;
            return new ((int)Math.Ceiling(rect.X), (int)Math.Ceiling(rect.Y), (int)rect.Width, (int)rect.Height);
        }
    }

    public bool HaveSelection => this.SelectionRegion != default;


    static ZoomableImage()
    {
        FocusableProperty.OverrideDefaultValue(typeof(ZoomableImage), true);
        AffectsRender<ZoomableImage>(
            //ShowGridProperty,
            //GridCellSizeProperty,
            //GridColorProperty,
            //GridColorAlternateProperty,
            PixelGridColorProperty,
            //ImageProperty,
            SelectionColorProperty,
            SelectionRegionProperty
            );
    }

    public ZoomableImage()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (this.ViewPort is not null)
        {
            this.ViewPort.PointerPressed -= this.ViewPortOnPointerPressed;
            this.ViewPort.PointerExited -= this.ViewPortOnPointerExited;
            this.ViewPort.PointerMoved -= this.ViewPortOnPointerMoved;
            this.ViewPort.PointerWheelChanged -= this.ViewPortOnPointerWheelChanged;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (this.HorizontalScrollBar is not null)
        {
            this.HorizontalScrollBar.Scroll -= this.ScrollBarOnScroll;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (this.VerticalScrollBar is not null)
        {
            this.VerticalScrollBar.Scroll -= this.ScrollBarOnScroll;
        }

        // ! Will find it by design 
        this.ViewPort = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter")!;
        // ! Will find it by design 
        this.HorizontalScrollBar = e.NameScope.Find<ScrollBar>("PART_HorizontalScrollBar")!;
        // ! Will find it by design 
        this.VerticalScrollBar = e.NameScope.Find<ScrollBar>("PART_VerticalScrollBar")!;

        this.SizeModeChanged();

        this.ViewPort.PointerPressed += this.ViewPortOnPointerPressed;
        this.ViewPort.PointerExited += this.ViewPortOnPointerExited;
        this.ViewPort.PointerMoved += this.ViewPortOnPointerMoved;
        this.ViewPort.PointerWheelChanged += this.ViewPortOnPointerWheelChanged;

        this.HorizontalScrollBar.Scroll += this.ScrollBarOnScroll;
        this.VerticalScrollBar.Scroll += this.ScrollBarOnScroll;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.UpdateViewPort();
        if (this.IsImageLoaded)
        {
            if (this.AutoZoomToFit)
            {
                this.Zoom = this.ZoomLevelToFit;
            }
            else if (this.ConstrainZoomOutToFitLevel)
            {
                var zoomLevelToFit = this.ZoomLevelToFit;
                if (this.Zoom < zoomLevelToFit)
                {
                    this.Zoom = zoomLevelToFit;
                }
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        this.UpdateViewPort();
        e.Handled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (!this.IsLoaded)
        {
            return;
        }

        if (ReferenceEquals(e.Property, ImageProperty))
        {
            this.UpdateViewPort();

            if (!this.IsImageLoaded)
            {
                this.SelectNone();
            }
            else
            {
                if (this.AutoZoomToFit)
                {
                    this.Zoom = this.ZoomLevelToFit;
                }
                else if (this.ConstrainZoomOutToFitLevel)
                {
                    var zoomLevelToFit = this.ZoomLevelToFit;
                    if (this.Zoom < zoomLevelToFit)
                    {
                        this.Zoom = zoomLevelToFit;
                    }
                }
            }

            this.TriggerRender();

            this.RaisePropertyChanged(nameof(this.ImageAsWriteableBitmap));
            this.RaisePropertyChanged(nameof(this.IsImageLoaded));
            this.RaisePropertyChanged(nameof(this.ScaledImageWidth));
            this.RaisePropertyChanged(nameof(this.ScaledImageHeight));
            this.RaisePropertyChanged(nameof(this.ScaledImageSize));
            this.RaisePropertyChanged(nameof(this.Extent));
            this.RaisePropertyChanged(nameof(this.ZoomLevelToFit));
        }
        else if (ReferenceEquals(e.Property, SizeModeProperty))
        {
            this.SizeModeChanged();
            this.TriggerRender();
            this.RaisePropertyChanged(nameof(this.IsHorizontalBarVisible));
            this.RaisePropertyChanged(nameof(this.IsVerticalBarVisible));
        }
        else if (ReferenceEquals(e.Property, ZoomProperty))
        {
            this.UpdateViewPort();
            this.TriggerRender();
            this.RaisePropertyChanged(nameof(this.IsHorizontalBarVisible));
            this.RaisePropertyChanged(nameof(this.IsVerticalBarVisible));
            this.RaisePropertyChanged(nameof(this.IsActualSize));
            this.RaisePropertyChanged(nameof(this.ZoomFactor));
            this.RaisePropertyChanged(nameof(this.ScaledImageWidth));
            this.RaisePropertyChanged(nameof(this.ScaledImageHeight));
            this.RaisePropertyChanged(nameof(this.ScaledImageSize));
            this.RaisePropertyChanged(nameof(this.Extent));
        }
        else if(ReferenceEquals(e.Property, PaddingProperty))
        {
            this.UpdateViewPort();
            this.TriggerRender();
        }
        //else if (ReferenceEquals(e.Property, GridCellSizeProperty)
        //         || ReferenceEquals(e.Property, GridColorProperty)
        //         || ReferenceEquals(e.Property, GridColorAlternateProperty))
        //{
        //    this.RebuildGridBrush();
        //    this.TriggerRender();
        //}
        //else if (ReferenceEquals(e.Property, PixelGridColorProperty))
        //{
        //    this._pixelGridPen = null;
        //}
        else if (ReferenceEquals(e.Property, SelectionColorProperty))
        {
            this._selectionBorderPen = null;
        }
    }


    //private void RebuildGridBrush()
    //{
    //    this._gridBrush = null;
    //}

    private Pen EnsurePixelGridPen()
    {
        return this._pixelGridPen ??= new Pen(this.PixelGridColor);
    }

    private Pen EnsureSelectionBorderPen()
    {
        if (this._selectionBorderPen is not null)
        {
            return this._selectionBorderPen;
        }

        var color = this.SelectionColor.Color;
        return this._selectionBorderPen = new Pen(Color.FromArgb(255, color.R, color.G, color.B).ToUInt32());
    }

    //private DrawingBrush EnsureGridBrush()
    //{
    //    if (this._gridBrush != null)
    //    {
    //        return this._gridBrush;
    //    }

    //    var gridCellSize = Math.Max((byte)1, this.GridCellSize); // guard

    //    // Build a 2s x 2s checkerboard tile (top-left and bottom-right = GridColor)
    //    var group = new DrawingGroup
    //    {
    //        Children =
    //        {
    //            new GeometryDrawing { Brush = this.GridColor, Geometry = new RectangleGeometry(new Rect(0, 0, gridCellSize, gridCellSize)) },
    //            new GeometryDrawing { Brush = this.GridColorAlternate, Geometry = new RectangleGeometry(new Rect(gridCellSize, 0, gridCellSize, gridCellSize)) },
    //            new GeometryDrawing { Brush = this.GridColorAlternate, Geometry = new RectangleGeometry(new Rect(0, gridCellSize, gridCellSize, gridCellSize)) },
    //            new GeometryDrawing { Brush = this.GridColor, Geometry = new RectangleGeometry(new Rect(gridCellSize, gridCellSize, gridCellSize, gridCellSize)) },
    //        }
    //    };

    //    this._gridBrush = new DrawingBrush
    //    {
    //        Drawing = group,
    //        Stretch = Stretch.None,
    //        TileMode = TileMode.Tile,
    //        // DestinationRect in ABSOLUTE units defines the tile size in device-independent px
    //        DestinationRect = new RelativeRect(0, 0, 2 * gridCellSize, 2 * gridCellSize, RelativeUnit.Absolute),
    //        AlignmentX = AlignmentX.Left,
    //        AlignmentY = AlignmentY.Top
    //    };
    //    return this._gridBrush;
    //}


    public void TriggerRender(bool renderOnlyCursorTracker = false)
    {
        if (!this._canRender)
        {
            return;
        }

        if (renderOnlyCursorTracker && this._trackerImage is null)
        {
            return;
        }

        this.InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        //Debug.WriteLine($"Render: {DateTime.Now.Ticks}");
        base.Render(context);

        var bounds = this.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        // Draw Grid
        //if (this.ShowGrid && this.GridCellSize > 0)
        //{
        //    var brush = this.EnsureGridBrush();
        //    // One call fills everything with the tiled checkerboard
        //    context.FillRectangle(brush, new Rect(bounds.Size));
        //}
        /*else
        {
            context.FillRectangle(Background, new Rect(0, 0, Viewport.Width, Viewport.Height));
        }*/

        var image = this.Image;
        if (image is null)
        {
            return;
        }

        var imageViewPort = this.GetImageViewPort();

        // Draw image
        context.DrawImage(image,
            this.GetSourceImageRegion(),
            imageViewPort
        );

        var zoomFactor = this.ZoomFactor;

        if (this.HaveTrackerImage && this._pointerPosition is {X: >= 0, Y: >= 0})
        {
            var destSize = this.TrackerImageAutoZoom
                ? new Size(this._trackerImage.Size.Width * zoomFactor, this._trackerImage.Size.Height * zoomFactor)
                : image.Size;

            var destPos = new Point(
                this._pointerPosition.X - destSize.Width / 2,
                this._pointerPosition.Y - destSize.Height / 2
            );

            // ! Verified by HaveTrackerImage above 
            context.DrawImage(this._trackerImage!, new Rect(destPos, destSize));
        }

        //SkiaContext.SkCanvas.dr
        // Draw pixel grid
        if (this.SizeMode == SizeModes.Normal && zoomFactor > this.PixelGridZoomThreshold)
        {
            var offsetX = this.Offset.X % zoomFactor;
            var offsetY = this.Offset.Y % zoomFactor;

            var left = imageViewPort.X;
            var top = imageViewPort.Y;
            var right = imageViewPort.Right;
            var bottom = imageViewPort.Bottom;

            var pixelGridPen = this.EnsurePixelGridPen();

            // First vertical line position aligned to zoom steps
            var startX = left + zoomFactor - offsetX;
            for (double x = startX; x < right; x += zoomFactor)
            {
                context.DrawLine(pixelGridPen, new Point(x, top), new Point(x, bottom));
            }

            // First horizontal line position aligned to zoom steps
            var startY = top + zoomFactor - offsetY;
            for (double y = startY; y < bottom; y += zoomFactor)
            {
                context.DrawLine(pixelGridPen, new Point(left, y), new Point(right, y));
            }

            context.DrawRectangle(pixelGridPen, imageViewPort);
        }

        var selectionRegion = this.SelectionRegion;
        if (selectionRegion != default)
        {
            var rect = this.GetOffsetRectangle(selectionRegion, imageViewPort);
            var selectionColor = this.SelectionColor;
            context.FillRectangle(selectionColor, rect);
            context.DrawRectangle(this.EnsureSelectionBorderPen(), rect);
        }
    }

    private bool UpdateViewPort()
    {
        var horizontalScrollBar = this.HorizontalScrollBar;
        if (horizontalScrollBar is null)
        {
            return false;
        }

        var verticalScrollBar = this.VerticalScrollBar;
        if (verticalScrollBar is null)
        {
            return false;
        }

        if (!this.IsImageLoaded || this.SizeMode != SizeModes.Normal)
        {
            horizontalScrollBar.Maximum = 0;
            verticalScrollBar.Maximum = 0;
            return true;
        }

        var scaledImageWidth = this.ScaledImageWidth;
        var scaledImageHeight = this.ScaledImageHeight;
        var width = Math.Max(0, scaledImageWidth - horizontalScrollBar.ViewportSize);
        var height = Math.Max(0, scaledImageHeight - verticalScrollBar.ViewportSize);
        //var width = scaledImageWidth <= Viewport.Width ? Viewport.Width : scaledImageWidth;
        //var height = scaledImageHeight <= Viewport.Height ? Viewport.Height : scaledImageHeight;

        bool changed = false;
        if (Math.Abs(horizontalScrollBar.Maximum - width) > 0.01)
        {
            horizontalScrollBar.Maximum = width;
            changed = true;
        }

        if (Math.Abs(verticalScrollBar.Maximum - height) > 0.01)
        {
            verticalScrollBar.Maximum = height;
            changed = true;
        }

        /*if (changed)
        {
            var newContainer = new ContentControl
            {
                Width = width,
                Height = height
            };
            FillContainer.Content = SizedContainer = newContainer;
            Debug.WriteLine($"Updated ViewPort: {DateTime.Now.Ticks}");
            //TriggerRender();
        }*/

        return changed;
    }


    private void ScrollBarOnScroll(object? sender, ScrollEventArgs e)
    {
        this.TriggerRender();
    }

    /*protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        Debug.WriteLine($"ViewportDelta: {e.ViewportDelta} | OffsetDelta: {e.OffsetDelta} | ExtentDelta: {e.ExtentDelta}");
        if (!e.ViewportDelta.IsDefault)
        {
            UpdateViewPort();
        }

        TriggerRender();

        base.OnScrollChanged(e);
    }*/

    private void ViewPortOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!this.IsImageLoaded || this.SizeMode != SizeModes.Normal)
        {
            return;
        }

        // Process horizontal scroll
        if (e.Delta.X != 0 && this.IsHorizontalBarVisible)
        {
            var factor = (e.KeyModifiers & KeyModifiers.Alt) != 0 ? this.HorizontalScrollWithMouseAlternativeFactor : this.HorizontalScrollWithMouseFactor;
            if (factor != 0)
            {
                this.Offset = this.Offset.WithX(this.Offset.X - e.Delta.X * factor);
                e.Handled = true;
            }
        }

        // Process vertical scroll
        if (e.Delta.Y == 0)
        {
            return;
        }

        var verticalScrollWithMouseWheelKeyModifier = this.VerticalScrollWithMouseWheelKeyModifier;
        if (verticalScrollWithMouseWheelKeyModifier.HasValue && (e.KeyModifiers & verticalScrollWithMouseWheelKeyModifier) == verticalScrollWithMouseWheelKeyModifier)
        {
            if (!this.IsVerticalBarVisible)
            {
                return;
            }

            var factor = (e.KeyModifiers & KeyModifiers.Alt) != 0 ? this.VerticalScrollWithMouseAlternativeFactor : this.VerticalScrollWithMouseFactor;
            if (factor != 0)
            {
                this.Offset = this.Offset.WithY(this.Offset.Y - e.Delta.Y * factor);
            }

            e.Handled = true;
            return;
        }


        var mouseWheelBehaviour = this.ZoomWithMouseWheelBehaviour;
        if (mouseWheelBehaviour == MouseWheelZoomBehaviours.None)
        {
            return;
        }

        /*
#if DEBUG
        //File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "UVtoolsScroll.txt")
        //, $"WheelEvent {{Timestamp: {e.Timestamp}, Handled: {e.Handled}, Delta: {e.Delta}}}{Environment.NewLine}");
#endif
        */

        var zoomWithMouseWheelKeyModifier = this.ZoomWithMouseWheelKeyModifier;
        var canZoom = this.ZoomWithMouseWheelStrictKeyModifier switch
        {
            false => (e.KeyModifiers & zoomWithMouseWheelKeyModifier) == zoomWithMouseWheelKeyModifier,
            true => e.KeyModifiers == zoomWithMouseWheelKeyModifier
        };

        if (!canZoom)
        {
            return;
        }

        e.Handled = true;

        // Debounce for sensitive touchpads
        var zoomWithMouseWheelDebounceMilliseconds = this.ZoomWithMouseWheelDebounceMilliseconds;
        if (zoomWithMouseWheelDebounceMilliseconds > 0 && e.Timestamp - this._lastZoomWithMouseWheelTimestamp < (ulong)zoomWithMouseWheelDebounceMilliseconds)
        {
            return;
        }

        // The MouseWheel event can contain multiple "spins" of the wheel so we need to adjust accordingly
        //double spins = Math.Abs(e.Delta.Y);
        //Debug.WriteLine(e.GetPosition(this));
        // TODO: Really should update the source method to handle multiple increments rather than calling it multiple times
        /*for (int i = 0; i < spins; i++)
        {*/


        switch (mouseWheelBehaviour)
        {
            case MouseWheelZoomBehaviours.ZoomNative:
                this.SetZoom(this.Zoom + (int)(e.Delta.Y * 100), e.GetPosition(this.ViewPort));
                break;
            case MouseWheelZoomBehaviours.ZoomNativeAltLevels:
                if ((e.KeyModifiers & KeyModifiers.Alt) == 0)
                {
                    this.SetZoom(this.Zoom + (int)(e.Delta.Y * 100), e.GetPosition(this.ViewPort));
                }
                else
                {
                    this.PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, e.GetPosition(this.ViewPort));
                }
                break;
            case MouseWheelZoomBehaviours.ZoomLevels:
                this.PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, e.GetPosition(this.ViewPort));
                break;
            case MouseWheelZoomBehaviours.ZoomLevelsAltNative:
                if ((e.KeyModifiers & KeyModifiers.Alt) == 0)
                {
                    this.PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, e.GetPosition(this.ViewPort));
                }
                else
                {
                    this.SetZoom(this.Zoom + (int)(e.Delta.Y * 100), e.GetPosition(this.ViewPort));
                }
                break;
        }

        this._lastZoomWithMouseWheelTimestamp = e.Timestamp;
        //}
    }

    private void ViewPortOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled
            || this._isPanning
            || this._isSelecting
            || this.Image is null)
        {
            return;
        }

        var pointer = e.GetCurrentPoint(this);

        if (this.SelectionMode != SelectionModes.None)
        {
            if (!(
                    pointer.Properties.IsLeftButtonPressed && (this.SelectWithMouseButtons & MouseButtons.LeftButton) != 0 ||
                    pointer.Properties.IsMiddleButtonPressed && (this.SelectWithMouseButtons & MouseButtons.MiddleButton) != 0 ||
                    pointer.Properties.IsRightButtonPressed && (this.SelectWithMouseButtons & MouseButtons.RightButton) != 0
                )
               )
            {
                return;
            }

            this.IsSelecting = true;
        }
        else
        {
            if (!(
                    pointer.Properties.IsLeftButtonPressed && (this.PanWithMouseButtons & MouseButtons.LeftButton) != 0 ||
                    pointer.Properties.IsMiddleButtonPressed && (this.PanWithMouseButtons & MouseButtons.MiddleButton) != 0 ||
                    pointer.Properties.IsRightButtonPressed && (this.PanWithMouseButtons & MouseButtons.RightButton) != 0
                )
                || !this.AutoPan
                || this.SizeMode != SizeModes.Normal

               )
            {
                return;
            }

            this.IsPanning = true;
        }

        var location = pointer.Position;

        if (location.X > this.Viewport.Width)
        {
            return;
        }

        if (location.Y > this.Viewport.Height)
        {
            return;
        }

        this._startMousePosition = location;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Handled)
        {
            return;
        }

        this.IsPanning = false;
        this.IsSelecting = false;
    }

    private void ViewPortOnPointerExited(object? sender, PointerEventArgs e)
    {
        this.PointerPosition = new Point(-1, -1);
        this.TriggerRender(true);
        e.Handled = true;
    }

    /*protected override void OnPointerLeave(PointerEventArgs e)
    {
        base.OnPointerLeave(e);
        PointerPosition = new Point(-1, -1);
        TriggerRender(true);
        e.Handled = true;
    }*/

    private void ViewPortOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        var viewPort = this.ViewPort;
        if (viewPort is null)
        {
            e.Handled = true;
            return;
        }

        var pointer = e.GetCurrentPoint(viewPort);
        this.PointerPosition = pointer.Position;

        if (!this._isPanning && !this._isSelecting)
        {
            this.TriggerRender(true);
            return;
        }

        if (this._isPanning)
        {
            double x;
            double y;

            if (!this.InvertMousePan)
            {
                x = this._startScrollPosition.X + (this._startMousePosition.X - this._pointerPosition.X);
                y = this._startScrollPosition.Y + (this._startMousePosition.Y - this._pointerPosition.Y);
            }
            else
            {
                x = (this._startScrollPosition.X - (this._startMousePosition.X - this._pointerPosition.X));
                y = (this._startScrollPosition.Y - (this._startMousePosition.Y - this._pointerPosition.Y));
            }

            this.Offset = new Vector(x, y);
        }
        else if (this._isSelecting)
        {
            var viewPortPoint = new Point(
                Math.Min(this._pointerPosition.X, viewPort.Bounds.Right),
                Math.Min(this._pointerPosition.Y, viewPort.Bounds.Bottom));

            double x;
            double y;
            double w;
            double h;

            var imageOffset = this.GetImageViewPort().Position;

            if (viewPortPoint.X < this._startMousePosition.X)
            {
                x = viewPortPoint.X;
                w = this._startMousePosition.X - viewPortPoint.X;
            }
            else
            {
                x = this._startMousePosition.X;
                w = viewPortPoint.X - this._startMousePosition.X;
            }

            if (viewPortPoint.Y < this._startMousePosition.Y)
            {
                y = viewPortPoint.Y;
                h = this._startMousePosition.Y - viewPortPoint.Y;
            }
            else
            {
                y = this._startMousePosition.Y;
                h = viewPortPoint.Y - this._startMousePosition.Y;
            }

            x -= imageOffset.X - this.Offset.X;
            y -= imageOffset.Y - this.Offset.Y;

            var zoomFactor = this.ZoomFactor;
            x /= zoomFactor;
            y /= zoomFactor;
            w /= zoomFactor;
            h /= zoomFactor;

            if (w > 0 && h > 0)
            {
                this.SelectionRegion = this.FitRectangle(new Rect(x, y, w, h));
            }
        }

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!this.IsImageLoaded || this.SizeMode != SizeModes.Normal)
        {
            return;
        }

        var zoomInKeyGestures = this.ZoomInKeyGestures;
        if (zoomInKeyGestures is not null)
        {
            foreach (var zoomInKeyGesture in zoomInKeyGestures)
            {
                if (e.KeyModifiers == zoomInKeyGesture.KeyModifiers && e.Key == zoomInKeyGesture.Key)
                {
                    this.ZoomIn();
                    e.Handled = true;
                    return;
                }
            }
        }

        var zoomOutKeyGestures = this.ZoomOutKeyGestures;
        if (zoomOutKeyGestures is not null)
        {
            foreach (var zoomOutKeyGesture in zoomOutKeyGestures)
            {
                if (e.KeyModifiers == zoomOutKeyGesture.KeyModifiers && e.Key == zoomOutKeyGesture.Key)
                {
                    this.ZoomOut();
                    e.Handled = true;
                    return;
                }
            }
        }

        if (e.KeyModifiers != KeyModifiers.None)
        {
            return;
        }

        var panLeft = false;
        var panUp = false;
        var panRight = false;
        var panDown = false;

        if (this.PanWithArrows)
        {
            switch (e.Key)
            {
                case Key.Left:
                    panLeft = true;
                    break;
                case Key.Up:
                    panUp = true;
                    break;
                case Key.Right:
                    panRight = true;
                    break;
                case Key.Down:
                    panDown = true;
                    break;
            }
        }

        if (e.Key == this.PanLeftKey)
        {
            panLeft = true;
        }
        else if (e.Key == this.PanUpKey)
        {
            panUp = true;
        }
        else if (e.Key == this.PanRightKey)
        {
            panRight = true;
        }
        else if (e.Key == this.PanDownKey)
        {
            panDown = true;
        }

        if (panLeft)
        {
            this.Offset = new Vector(this.Offset.X - this.PanOffset * this.ZoomFactor, this.Offset.Y);
            e.Handled = true;
            return;
        }

        if (panUp)
        {
            this.Offset = new Vector(this.Offset.X, this.Offset.Y - this.PanOffset * this.ZoomFactor);
            e.Handled = true;
            return;
        }

        if (panRight)
        {
            this.Offset = new Vector(this.Offset.X + this.PanOffset * this.ZoomFactor, this.Offset.Y);
            e.Handled = true;
            return;
        }

        if (panDown)
        {
            this.Offset = new Vector(this.Offset.X, this.Offset.Y + this.PanOffset * this.ZoomFactor);
            e.Handled = true;
            return;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (!this.IsImageLoaded || this.SizeMode != SizeModes.Normal)
        {
            return;
        }

        var zoomTo100KeyGestures = this.ZoomTo100KeyGestures;
        if (zoomTo100KeyGestures is not null && this.Zoom != 100)
        {
            foreach (var zoomTo100KeyGesture in zoomTo100KeyGestures)
            {
                if (e.KeyModifiers == zoomTo100KeyGesture.KeyModifiers && e.Key == zoomTo100KeyGesture.Key)
                {
                    this.Zoom = 100;
                    e.Handled = true;
                    return;
                }
            }
        }

        var zoomToFitKeyGestures = this.ZoomToFitKeyGestures;
        if (zoomToFitKeyGestures is not null)
        {
            foreach (var zoomToFitKeyGesture in zoomToFitKeyGestures)
            {
                if (e.KeyModifiers == zoomToFitKeyGesture.KeyModifiers && e.Key == zoomToFitKeyGesture.Key)
                {
                    this.Zoom = this.ZoomLevelToFit;
                    e.Handled = true;
                    return;
                }
            }
        }
    }


    /*protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Handled || !ViewPort.IsPointerOver) return;

        var pointer = e.GetCurrentPoint(ViewPort);
        PointerPosition = pointer.Position;

        if (!_isPanning && !_isSelecting)
        {
            TriggerRender(true);
            return;
        }

        if (_isPanning)
        {
            double x;
            double y;

            if (!InvertMousePan)
            {
                x = _startScrollPosition.X + (_startMousePosition.X - _pointerPosition.X);
                y = _startScrollPosition.Y + (_startMousePosition.Y - _pointerPosition.Y);
            }
            else
            {
                x = (_startScrollPosition.X - (_startMousePosition.X - _pointerPosition.X));
                y = (_startScrollPosition.Y - (_startMousePosition.Y - _pointerPosition.Y));
            }

            Offset = new Vector(x, y);
        }
        else if (_isSelecting)
        {
            double x;
            double y;
            double w;
            double h;

            var imageOffset = GetImageViewPort().Position;

            if (_pointerPosition.X < _startMousePosition.X)
            {
                x = _pointerPosition.X;
                w = _startMousePosition.X - _pointerPosition.X;
            }
            else
            {
                x = _startMousePosition.X;
                w = _pointerPosition.X - _startMousePosition.X;
            }

            if (_pointerPosition.Y < _startMousePosition.Y)
            {
                y = _pointerPosition.Y;
                h = _startMousePosition.Y - _pointerPosition.Y;
            }
            else
            {
                y = _startMousePosition.Y;
                h = _pointerPosition.Y - _startMousePosition.Y;
            }

            x -= imageOffset.X - Offset.X;
            y -= imageOffset.Y - Offset.Y;

            var zoomFactor = ZoomFactor;
            x /= zoomFactor;
            y /= zoomFactor;
            w /= zoomFactor;
            h /= zoomFactor;

            if (w > 0 && h > 0)
            {

                SelectionRegion = FitRectangle(new Rect(x, y, w, h));
            }
        }

        e.Handled = true;
    }*/

    /// <summary>
    /// Resets the <see cref="SizeModes"/> property whilsts retaining the original <see cref="Zoom"/>.
    /// </summary>
    protected void RestoreSizeMode()
    {
        if (this.SizeMode != SizeModes.Normal)
        {
            var previousZoom = this.Zoom;
            this.SizeMode = SizeModes.Normal;
            this.Zoom = previousZoom; // Stop the zoom getting reset to 100% before calculating the new zoom
        }
    }

    /// <summary>
    /// Returns an appropriate zoom level based on the specified action, relative to the current zoom level.
    /// </summary>
    /// <param name="action">The action to determine the zoom level.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if an unsupported action is specified.</exception>
    public int GetZoomLevel(ZoomActions action)
    {
        var result = action switch
        {
            ZoomActions.None => this.Zoom,
            ZoomActions.ZoomIn => this._zoomLevels.NextZoom(this.Zoom),
            ZoomActions.ZoomOut => this._zoomLevels.PreviousZoom(this.Zoom),
            ZoomActions.ActualSize => 100,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };
        return result;
    }

    /// <summary>
    ///  Performs the specified zoom action.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void PerformZoom(ZoomActions action, Point? relativePoint)
    {
        this.SetZoom(this.GetZoomLevel(action), true, relativePoint);
    }

    /// <summary>
    /// Performs the specified zoom action.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void PerformZoom(ZoomActions action, bool preservePosition = true, Point? relativePoint = null)
    {
        this.SetZoom(this.GetZoomLevel(action), preservePosition, relativePoint);
    }

    /// <summary>
    /// Sets the zoom level to the specified value.
    /// </summary>
    /// <param name="zoom"></param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void SetZoom(int zoom, Point? relativePoint)
    {
        this.SetZoom(zoom, true, relativePoint);
    }

    /// <summary>
    /// Sets the zoom level to the specified value.
    /// </summary>
    /// <param name="zoom"></param>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void SetZoom(int zoom, bool preservePosition = true, Point? relativePoint = null)
    {
        relativePoint ??= this.CenterPoint;
        int currentZoom = this.Zoom;
        Point currentPixel = this.PointToImage(relativePoint.Value);

        this.RestoreSizeMode();
        this.Zoom = zoom;

        if (preservePosition && this.Zoom != currentZoom)
        {
            this.ScrollTo(currentPixel, relativePoint.Value);
        }
    }

    /// <summary>
    ///   Zooms into the image
    /// </summary>
    public void ZoomIn()
    {
        this.ZoomIn(true);
    }

    /// <summary>
    ///   Zooms into the image
    /// </summary>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    public void ZoomIn(bool preservePosition)
    {
        this.PerformZoom(ZoomActions.ZoomIn, preservePosition);
    }

    /// <summary>
    ///   Zooms out of the image
    /// </summary>
    public void ZoomOut()
    {
        this.ZoomOut(true);
    }

    /// <summary>
    ///   Zooms out of the image
    /// </summary>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    public void ZoomOut(bool preservePosition)
    {
        this.PerformZoom(ZoomActions.ZoomOut, preservePosition);
    }

    /// <summary>
    /// Zooms to the maximum size for displaying the entire image within the bounds of the control.
    /// </summary>
    public void ZoomToFit()
    {
        this.Zoom = this.ZoomLevelToFit;
    }

    /// <summary>
    ///   Adjusts the view port to fit the given region
    /// </summary>
    /// <param name="x">The X co-ordinate of the selection region.</param>
    /// <param name="y">The Y co-ordinate of the selection region.</param>
    /// <param name="width">The width of the selection region.</param>
    /// <param name="height">The height of the selection region.</param>
    /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
    public void ZoomToRegion(double x, double y, double width, double height, double margin = 0)
    {
        this.ZoomToRegion(new Rect(x, y, width, height), margin);
    }

    /// <summary>
    ///   Adjusts the view port to fit the given region
    /// </summary>
    /// <param name="x">The X co-ordinate of the selection region.</param>
    /// <param name="y">The Y co-ordinate of the selection region.</param>
    /// <param name="width">The width of the selection region.</param>
    /// <param name="height">The height of the selection region.</param>
    /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
    public void ZoomToRegion(int x, int y, int width, int height, double margin = 0)
    {
        this.ZoomToRegion(new Rect(x, y, width, height), margin);
    }

    /// <summary>
    ///   Adjusts the view port to fit the given region
    /// </summary>
    /// <param name="rectangle">The rectangle to fit the view port to.</param>
    /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
    public void ZoomToRegion(System.Drawing.Rectangle rectangle, double margin = 0)
    {
        this.ZoomToRegion(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, margin);
    }

    /// <summary>
    ///   Adjusts the view port to fit the given region
    /// </summary>
    /// <param name="rectangle">The rectangle to fit the view port to.</param>
    /// <param name="margin">Give a margin to rectangle by a value to zoom-out that pixel value</param>
    public void ZoomToRegion(Rect rectangle, double margin = 0)
    {
        if (!this.IsImageLoaded)
        {
            return;
        }

        if (margin > 0)
        {
            rectangle = rectangle.Inflate(margin);
        }

        var ratioX = this.Viewport.Width / rectangle.Width;
        var ratioY = this.Viewport.Height / rectangle.Height;
        var zoomFactor = Math.Min(ratioX, ratioY);
        var cx = rectangle.X + rectangle.Width / 2;
        var cy = rectangle.Y + rectangle.Height / 2;

        this.CanRender = false;
        this.Zoom = (int)(zoomFactor * 100); // This function sets the zoom so viewport will change

        //Dispatcher.UIThread.Post(() => CenterAt(new Point(cx, cy)));
        this.CenterAt(new Point(cx, cy)); // If I call this here, it will move to the wrong position due wrong viewport, dispatcher would solve but slower?
    }

    /// <summary>
    /// Zooms to current selection region
    /// </summary>
    public void ZoomToSelectionRegion(double margin = 0)
    {
        if (!this.HaveSelection)
        {
            return;
        }

        this.ZoomToRegion(this.SelectionRegion, margin);
    }

    /// <summary>
    /// Resets the zoom to 100%.
    /// </summary>
    public void PerformActualSize()
    {
        this.SizeMode = SizeModes.Normal;
        //SetZoom(100, ImageZoomActions.ActualSize | (Zoom < 100 ? ImageZoomActions.ZoomIn : ImageZoomActions.ZoomOut));
        this.Zoom = 100;
    }

    /// <summary>
    ///   Determines whether the specified point is located within the image view port
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>
    ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPointInImage(Point point)
    {
        return this.GetImageViewPort().Contains(point);
    }

    /// <summary>
    ///   Determines whether the specified point is located within the image view port
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to check.</param>
    /// <param name="y">The Y co-ordinate of the point to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPointInImage(int x, int y)
    {
        return this.IsPointInImage(new Point(x, y));
    }

    /// <summary>
    ///   Determines whether the specified point is located within the image view port
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to check.</param>
    /// <param name="y">The Y co-ordinate of the point to check.</param>
    /// <returns>
    ///   <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPointInImage(double x, double y)
    {
        return this.IsPointInImage(new Point(x, y));
    }

    /// <summary>
    ///   Converts the given client size point to represent a coordinate on the source image.
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to convert.</param>
    /// <param name="y">The Y co-ordinate of the point to convert.</param>
    /// <param name="fitToBounds">
    ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
    /// </param>
    /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
    public Point PointToImage(double x, double y, bool fitToBounds = true)
    {
        return this.PointToImage(new Point(x, y), fitToBounds);
    }

    /// <summary>
    ///   Converts the given client size point to represent a coordinate on the source image.
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to convert.</param>
    /// <param name="y">The Y co-ordinate of the point to convert.</param>
    /// <param name="fitToBounds">
    ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
    /// </param>
    /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
    public Point PointToImage(int x, int y, bool fitToBounds = true)
    {
        return this.PointToImage(new Point(x, y), fitToBounds);
    }

    /// <summary>
    ///   Converts the given client size point to represent a coordinate on the source image.
    /// </summary>
    /// <param name="point">The source point.</param>
    /// <param name="fitToBounds">
    ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
    /// </param>
    /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
    public Point PointToImage(Point point, bool fitToBounds = true)
    {
        if (this.Image is null) 
        {
            return new(0,0);
        }
        
        double x;
        double y;

        var viewport = this.GetImageViewPort();

        if (!fitToBounds || viewport.Contains(point))
        {
            x = (point.X + this.Offset.X - viewport.X) / this.ZoomFactor;
            y = (point.Y + this.Offset.Y - viewport.Y) / this.ZoomFactor;

            var image = this.Image;
            if (fitToBounds)
            {
                x = Math.Clamp(x, 0, image.Size.Width-1);
                y = Math.Clamp(y, 0, image.Size.Height-1);
            }
        }
        else
        {
            x = 0; // Return Point.Empty if we couldn't match
            y = 0;
        }

        return new(x, y);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Point" /> repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="Point"/> to offset.</param>
    /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
    public Point GetOffsetPoint(System.Drawing.Point source)
    {
        var offset = this.GetOffsetPoint(new Point(source.X, source.Y));

        return new((int)offset.X, (int)offset.Y);
    }

    /// <summary>
    ///   Returns the source co-ordinates repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="x">The source X co-ordinate.</param>
    /// <param name="y">The source Y co-ordinate.</param>
    /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
    public Point GetOffsetPoint(int x, int y)
    {
        return this.GetOffsetPoint(new Point(x, y));
    }

    /// <summary>
    ///   Returns the source co-ordinates repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="x">The source X co-ordinate.</param>
    /// <param name="y">The source Y co-ordinate.</param>
    /// <returns>A <see cref="Point"/> which has been repositioned to match the current zoom level and image offset</returns>
    public Point GetOffsetPoint(double x, double y)
    {
        return this.GetOffsetPoint(new Point(x, y));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.PointF" /> repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="PointF"/> to offset.</param>
    /// <returns>A <see cref="PointF"/> which has been repositioned to match the current zoom level and image offset</returns>
    public Point GetOffsetPoint(Point source)
    {
        Rect viewport = this.GetImageViewPort();
        var scaled = this.GetScaledPoint(source);
        var offsetX = viewport.Left - this.Offset.X;
        var offsetY = viewport.Top - this.Offset.Y;

        return new(scaled.X + offsetX, scaled.Y + offsetY);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="source">The source <see cref="RectangleF"/> to offset.</param>
    /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    public Rect GetOffsetRectangle(Rect source)
    {
        return this.GetOffsetRectangle(source, this.GetImageViewPort());
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="source">The source <see cref="RectangleF"/> to offset.</param>
    /// <param name="imageViewPort">The image viewport to use for the offset calculation.</param>
    /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    private Rect GetOffsetRectangle(Rect source, Rect imageViewPort)
    {
        var viewport = imageViewPort;
        var scaled = this.GetScaledRectangle(source);
        var offsetX = viewport.Left - this.Offset.X;
        var offsetY = viewport.Top - this.Offset.Y;

        return new(new Point(scaled.Left + offsetX, scaled.Top + offsetY), scaled.Size);
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="x">The X co-ordinate of the source rectangle.</param>
    /// <param name="y">The Y co-ordinate of the source rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <returns>A <see cref="Rectangle"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    public System.Drawing.Rectangle GetOffsetRectangle(int x, int y, int width, int height)
    {
        return this.GetOffsetRectangle(new System.Drawing.Rectangle(x, y, width, height));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="x">The X co-ordinate of the source rectangle.</param>
    /// <param name="y">The Y co-ordinate of the source rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    public Rect GetOffsetRectangle(double x, double y, double width, double height)
    {
        return this.GetOffsetRectangle(new Rect(x, y, width, height));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Rectangle" /> scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="source">The source <see cref="Rectangle"/> to offset.</param>
    /// <returns>A <see cref="Rectangle"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    public System.Drawing.Rectangle GetOffsetRectangle(System.Drawing.Rectangle source)
    {
        var viewport = this.GetImageViewPort();
        var scaled = this.GetScaledRectangle(source);
        var offsetX = viewport.Left - this.Offset.X;
        var offsetY = viewport.Top - this.Offset.Y;

        return new(new System.Drawing.Point((int)(scaled.Left + offsetX), (int)(scaled.Top + offsetY)), new System.Drawing.Size((int)scaled.Size.Width, (int)scaled.Size.Height));
    }

    /// <summary>
    ///   Fits a given <see cref="T:System.Drawing.Rectangle" /> to match image boundaries
    /// </summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns>
    ///   A <see cref="T:System.Drawing.Rectangle" /> structure remapped to fit the image boundaries
    /// </returns>
    public System.Drawing.Rectangle FitRectangle(System.Drawing.Rectangle rectangle)
    {
        var image = this.Image;
        if (image is null)
        {
            return System.Drawing.Rectangle.Empty;
        }

        var x = rectangle.X;
        var y = rectangle.Y;
        var w = rectangle.Width;
        var h = rectangle.Height;

        if (x < 0)
        {
            x = 0;
        }

        if (y < 0)
        {
            y = 0;
        }

        if (x + w > image.Size.Width)
        {
            w = (int)(image.Size.Width - x);
        }

        if (y + h > image.Size.Height)
        {
            h = (int)(image.Size.Height - y);
        }

        return new(x, y, w, h);
    }

    /// <summary>
    ///   Fits a given <see cref="T:System.Drawing.RectangleF" /> to match image boundaries
    /// </summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns>
    ///   A <see cref="T:System.Drawing.RectangleF" /> structure remapped to fit the image boundaries
    /// </returns>
    public Rect FitRectangle(Rect rectangle)
    {
        var image = this.Image;
        if (image is null)
        {
            return default;
        }

        var x = rectangle.X;
        var y = rectangle.Y;
        var w = rectangle.Width;
        var h = rectangle.Height;

        if (x < 0)
        {
            w -= -x;
            x = 0;
        }

        if (y < 0)
        {
            h -= -y;
            y = 0;
        }

        if (x + w > image.Size.Width)
        {
            w = image.Size.Width - x;
        }

        if (y + h > image.Size.Height)
        {
            h = image.Size.Height - y;
        }

        return new(x, y, w, h);
    }

    /// <summary>
    ///   Scrolls the control to the given point in the image, offset at the specified display point
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to scroll to.</param>
    /// <param name="y">The Y co-ordinate of the point to scroll to.</param>
    /// <param name="relativeX">The X co-ordinate relative to the <c>x</c> parameter.</param>
    /// <param name="relativeY">The Y co-ordinate relative to the <c>y</c> parameter.</param>
    public void ScrollTo(double x, double y, double relativeX, double relativeY)
    {
        this.ScrollTo(new Point(x, y), new Point(relativeX, relativeY));
    }

    /// <summary>
    ///   Scrolls the control to the given point in the image, offset at the specified display point
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to scroll to.</param>
    /// <param name="y">The Y co-ordinate of the point to scroll to.</param>
    /// <param name="relativeX">The X co-ordinate relative to the <c>x</c> parameter.</param>
    /// <param name="relativeY">The Y co-ordinate relative to the <c>y</c> parameter.</param>
    public void ScrollTo(int x, int y, int relativeX, int relativeY)
    {
        this.ScrollTo(new Point(x, y), new Point(relativeX, relativeY));
    }

    /// <summary>
    ///   Scrolls the control to the given point in the image, offset at the specified display point
    /// </summary>
    /// <param name="imageLocation">The point of the image to attempt to scroll to.</param>
    /// <param name="relativeDisplayPoint">The relative display point to offset scrolling by.</param>
    public void ScrollTo(Point imageLocation, Point relativeDisplayPoint)
    {
        var zoomFactor = this.ZoomFactor;
        var x = imageLocation.X * zoomFactor - relativeDisplayPoint.X;
        var y = imageLocation.Y * zoomFactor - relativeDisplayPoint.Y;


        this.Offset = new Vector(x, y);
        this.CanRender = true;

        /*Debug.WriteLine(
            $"X/Y: {x},{y} | \n" +
            $"Offset: {Offset} | \n" +
            $"ZoomFactor: {ZoomFactor} | \n" +
            $"Image Location: {imageLocation}\n" +
            $"MAX: {HorizontalScrollBar.Maximum},{VerticalScrollBar.Maximum} \n" +
            $"ViewPort: {Viewport.Width},{Viewport.Height} \n" +
            $"Container: {HorizontalScrollBar.ViewportSize},{VerticalScrollBar.ViewportSize} \n" +
            $"Relative: {relativeDisplayPoint}");*/
    }

    /// <summary>
    ///   Centers the given point in the image in the center of the control
    /// </summary>
    /// <param name="imageLocation">The point of the image to attempt to center.</param>
    public void CenterAt(System.Drawing.Point imageLocation)
    {
        this.ScrollTo(new Point(imageLocation.X, imageLocation.Y), new Point(this.Viewport.Width / 2, this.Viewport.Height / 2));
    }

    /// <summary>
    ///   Centers the given point in the image in the center of the control
    /// </summary>
    /// <param name="imageLocation">The point of the image to attempt to center.</param>
    public void CenterAt(Point imageLocation)
    {
        this.ScrollTo(imageLocation, new Point(this.Viewport.Width / 2, this.Viewport.Height / 2));
    }

    /// <summary>
    ///   Centers the given point in the image in the center of the control
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to center.</param>
    /// <param name="y">The Y co-ordinate of the point to center.</param>
    public void CenterAt(int x, int y)
    {
        this.CenterAt(new Point(x, y));
    }

    /// <summary>
    ///   Centers the given point in the image in the center of the control
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to center.</param>
    /// <param name="y">The Y co-ordinate of the point to center.</param>
    public void CenterAt(double x, double y)
    {
        this.CenterAt(new Point(x, y));
    }

    /// <summary>
    /// Resets the viewport to show the center of the image.
    /// </summary>
    public void CenterToImage()
    {
        var horizontalScrollBar = this.HorizontalScrollBar;
        if (horizontalScrollBar is null)
        {
            return;
        }

        var verticalScrollBar = this.VerticalScrollBar;
        if (verticalScrollBar is null)
        {
            return;
        }

        this.Offset = new Vector(horizontalScrollBar.Maximum / 2.0, verticalScrollBar.Maximum / 2.0);
    }


    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to scale.</param>
    /// <param name="y">The Y co-ordinate of the point to scale.</param>
    /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
    public Point GetScaledPoint(int x, int y)
    {
        return this.GetScaledPoint(new Point(x, y));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="x">The X co-ordinate of the point to scale.</param>
    /// <param name="y">The Y co-ordinate of the point to scale.</param>
    /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
    public PointF GetScaledPoint(float x, float y)
    {
        return this.GetScaledPoint(new PointF(x, y));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Point" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="Point"/> to scale.</param>
    /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
    public Point GetScaledPoint(Point source)
    {
        return new(source.X * this.ZoomFactor, source.Y * this.ZoomFactor);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.PointF" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="PointF"/> to scale.</param>
    /// <returns>A <see cref="PointF"/> which has been scaled to match the current zoom level</returns>
    public PointF GetScaledPoint(PointF source)
    {
        return new((float)(source.X * this.ZoomFactor), (float)(source.Y * this.ZoomFactor));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level
    /// </summary>
    /// <param name="x">The X co-ordinate of the source rectangle.</param>
    /// <param name="y">The Y co-ordinate of the source rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
    public Rect GetScaledRectangle(int x, int y, int width, int height)
    {
        return this.GetScaledRectangle(new Rect(x, y, width, height));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level
    /// </summary>
    /// <param name="x">The X co-ordinate of the source rectangle.</param>
    /// <param name="y">The Y co-ordinate of the source rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <returns>A <see cref="RectangleF"/> which has been scaled to match the current zoom level</returns>
    public RectangleF GetScaledRectangle(float x, float y, float width, float height)
    {
        return this.GetScaledRectangle(new RectangleF(x, y, width, height));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level
    /// </summary>
    /// <param name="location">The location of the source rectangle.</param>
    /// <param name="size">The size of the source rectangle.</param>
    /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
    public Rect GetScaledRectangle(Point location, Size size)
    {
        return this.GetScaledRectangle(new Rect(location, size));
    }

    /// <summary>
    ///   Returns the source rectangle scaled according to the current zoom level
    /// </summary>
    /// <param name="location">The location of the source rectangle.</param>
    /// <param name="size">The size of the source rectangle.</param>
    /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
    public RectangleF GetScaledRectangle(PointF location, SizeF size)
    {
        return this.GetScaledRectangle(new RectangleF(location, size));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Rectangle" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="Rectangle"/> to scale.</param>
    /// <returns>A <see cref="Rectangle"/> which has been scaled to match the current zoom level</returns>
    public Rect GetScaledRectangle(Rect source)
    {
        return new(source.Left * this.ZoomFactor, source.Top * this.ZoomFactor, source.Width * this.ZoomFactor, source.Height * this.ZoomFactor);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="RectangleF"/> to scale.</param>
    /// <returns>A <see cref="RectangleF"/> which has been scaled to match the current zoom level</returns>
    public RectangleF GetScaledRectangle(RectangleF source)
    {
        return new((float)(source.Left * this.ZoomFactor), (float)(source.Top * this.ZoomFactor), (float)(source.Width * this.ZoomFactor), (float)(source.Height * this.ZoomFactor));
    }

    /// <summary>
    ///   Returns the source size scaled according to the current zoom level
    /// </summary>
    /// <param name="width">The width of the size to scale.</param>
    /// <param name="height">The height of the size to scale.</param>
    /// <returns>A <see cref="SizeF"/> which has been resized to match the current zoom level</returns>
    public SizeF GetScaledSize(float width, float height)
    {
        return this.GetScaledSize(new SizeF(width, height));
    }

    /// <summary>
    ///   Returns the source size scaled according to the current zoom level
    /// </summary>
    /// <param name="width">The width of the size to scale.</param>
    /// <param name="height">The height of the size to scale.</param>
    /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
    public Size GetScaledSize(int width, int height)
    {
        return this.GetScaledSize(new Size(width, height));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.SizeF" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="SizeF"/> to scale.</param>
    /// <returns>A <see cref="SizeF"/> which has been resized to match the current zoom level</returns>
    public SizeF GetScaledSize(SizeF source)
    {
        return new((float)(source.Width * this.ZoomFactor), (float)(source.Height * this.ZoomFactor));
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.Size" /> scaled according to the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="Size"/> to scale.</param>
    /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
    public Size GetScaledSize(Size source)
    {
        return new(source.Width * this.ZoomFactor, source.Height * this.ZoomFactor);
    }

    /// <summary>
    ///   Creates a selection region which encompasses the entire image
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown if no image is currently set</exception>
    public void SelectAll()
    {
        var image = this.Image;
        if (image is null)
        {
            return;
        }

        this.SelectionRegion = new Rect(0, 0, image.Size.Width, image.Size.Height);
    }

    /// <summary>
    /// Clears any existing selection region
    /// </summary>
    public void SelectNone()
    {
        this.SelectionRegion = default;
    }


    /// <summary>
    ///   Gets the source image region.
    /// </summary>
    /// <returns></returns>
    public Rect GetSourceImageRegion()
    {
        var image = this.Image;
        if (image is null)
        {
            return default;
        }

        switch (this.SizeMode)
        {
            case SizeModes.Normal:
                var offset = this.Offset;
                var viewPort = this.GetImageViewPort();
                var zoomFactor = this.ZoomFactor;
                double sourceLeft = (offset.X / zoomFactor);
                double sourceTop = (offset.Y / zoomFactor);
                double sourceWidth = (viewPort.Width / zoomFactor);
                double sourceHeight = (viewPort.Height / zoomFactor);

                return new(sourceLeft, sourceTop, sourceWidth, sourceHeight);
        }

        return new(0, 0, image.Size.Width, image.Size.Height);

    }

    /// <summary>
    /// Gets the image view port.
    /// </summary>
    /// <returns>The image viewport rectangle.</returns>
    public Rect GetImageViewPort()
    {
        var image = this.Image;
        if (image is null)
        {
            return default;
        }

        var viewPortSize = this.Viewport;
        if (viewPortSize is {Width: 0, Height: 0})
        {
            return default;
        }

        double xOffset = 0.0;
        double yOffset = 0.0;
        double width = 0.0;
        double height = 0.0;

        var padding = this.Padding;

        switch (this.SizeMode)
        {
            case SizeModes.Normal:
                if (this.AutoCenter)
                {
                    xOffset = (!this.IsHorizontalBarVisible ? (viewPortSize.Width - this.ScaledImageWidth) / 2.0 : 0.0);
                    yOffset = (!this.IsVerticalBarVisible ? (viewPortSize.Height - this.ScaledImageHeight) / 2.0 : 0.0);
                }

                width = Math.Min(this.ScaledImageWidth - Math.Abs(this.Offset.X), viewPortSize.Width);
                height = Math.Min(this.ScaledImageHeight - Math.Abs(this.Offset.Y), viewPortSize.Height);
                break;
            case SizeModes.Stretch:
                width = viewPortSize.Width - padding.Left - padding.Right;
                if (width <= 0)
                {
                    return new Rect();
                }

                height = viewPortSize.Height - padding.Top - padding.Bottom;
                if (height <= 0)
                {
                    return new Rect();
                }

                xOffset = padding.Left;
                yOffset = padding.Top;
                break;
            case SizeModes.Fit:
                double scaleFactor = Math.Min((viewPortSize.Width - padding.Left - padding.Right) / image.Size.Width, (viewPortSize.Height - padding.Top - padding.Bottom) / image.Size.Height);

                if (scaleFactor <= 0)
                {
                    return new Rect();
                }

                width = Math.Floor(image.Size.Width * scaleFactor);
                height = Math.Floor(image.Size.Height * scaleFactor);

                if (this.AutoCenter)
                {
                    xOffset = (viewPortSize.Width - width) / 2.0;
                    yOffset = (viewPortSize.Height - height) / 2.0;
                }
                else
                {
                    xOffset = padding.Left;
                    yOffset = padding.Top;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(this.SizeMode), this.SizeMode, null);
        }

        return new Rect(xOffset, yOffset, width, height);
    }

    /// <summary>
    /// Loads the image from the specified path.
    /// </summary>
    /// <param name="path">Image path from disk</param>
    public void LoadImage(string path)
    {
        this.Image = new Bitmap(path);
        this._imageNeedsDisposal = true;
    }

    public Bitmap? GetSelectedBitmap()
    {
        var image = this.ImageAsWriteableBitmap;
        if (image is null || !this.HaveSelection)
        {
            return null;
        }

        var selection = this.SelectionRegionPixel;

        using var srcBuffer = image.Lock();

        // Clamp selection to actual image bounds to prevent buffer overread in unsafe copy.
        // Math.Ceiling on X/Y in SelectionRegionPixel can push Right/Bottom one pixel past the image edge.
        var clampedX = Math.Max(0, selection.X);
        var clampedY = Math.Max(0, selection.Y);
        var clampedWidth = Math.Min(selection.Right, srcBuffer.Size.Width) - clampedX;
        var clampedHeight = Math.Min(selection.Bottom, srcBuffer.Size.Height) - clampedY;
        if (clampedWidth <= 0 || clampedHeight <= 0)
        {
            return null;
        }

        var clampedSelection = new PixelRect(clampedX, clampedY, clampedWidth, clampedHeight);

        var cropBitmap = new WriteableBitmap(clampedSelection.Size, image.Dpi, srcBuffer.Format, AlphaFormat.Unpremul);
        using var dstBuffer = cropBitmap.Lock();

        unsafe
        {
            var ySrc = srcBuffer.Address + srcBuffer.RowBytes * clampedSelection.Y + clampedSelection.X * (srcBuffer.Format.BitsPerPixel / 8);
            var yDst = dstBuffer.Address;

            for (int y = clampedSelection.Y; y < clampedSelection.Bottom; y++)
            {
                Buffer.MemoryCopy(
                    ySrc.ToPointer(),
                    yDst.ToPointer(),
                    dstBuffer.RowBytes,
                    dstBuffer.RowBytes);

                ySrc += srcBuffer.RowBytes;
                yDst += dstBuffer.RowBytes;
            }
        }

        return cropBitmap;
    }

}