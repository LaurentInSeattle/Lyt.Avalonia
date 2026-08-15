/*
*                               The MIT License (MIT)
* Permission is hereby granted, free of charge, to any person obtaining a copy of
* this software and associated documentation files (the "Software"), to deal in
* the Software without restriction, including without limitation the rights to
* use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
* the Software, and to permit persons to whom the Software is furnished to do so.
*/

// Port from: https://github.com/cyotek/Cyotek.Windows.Forms.ImageBox to AvaloniaUI
// Port from: https://github.com/sn4k3/UVtools/tree/master/UVtools.AvaloniaControls by Tiago Conceição 

using Avalonia.Controls.Metadata;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using Key = Avalonia.Input.Key;
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
    protected internal ScrollContentPresenter? ViewPort;
    protected internal ScrollBar? HorizontalScrollBar;
    protected internal ScrollBar? VerticalScrollBar;

    private Point _startMousePosition;
    private Vector _startScrollPosition;
    private bool _isPanning;
    private bool _isSelecting;
    private Pen? _pixelGridPen;
    private Pen? _selectionBorderPen;

    static ZoomableImage()
    {
        InputElement.FocusableProperty.OverrideDefaultValue<ZoomableImage>(true);
        Visual.AffectsRender<ZoomableImage>(
            PixelGridColorProperty,
            SelectionColorProperty,
            SelectionRegionProperty,
            ImageProperty,
            ZoomProperty
            );
    }

    public ZoomableImage() => RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

    /// <summary> Returns true if image is loaded, otherwise false. </summary>
    public bool IsImageLoaded => this.Image is not null;

    private bool IsHorizontalBarVisible
    {
        get
        {
            if (!this.IsImageLoaded || (this.SizeMode != SizeModes.Normal))
            {
                return false;
            }

            return this.ScaledImageWidth > this.Viewport.Width;
        }
    }

    private bool IsVerticalBarVisible
    {
        get
        {
            if ((this.Image is null) || (this.SizeMode != SizeModes.Normal))
            {
                return false;
            }

            return this.ScaledImageHeight > this.Viewport.Height;
        }
    }

    /// <summary> Gets the center point of the viewport </summary>
    private Point CenterPoint
    {
        get
        {
            var viewport = this.GetImageViewPort();
            return new(viewport.Width / 2, viewport.Height / 2);
        }
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

    /// <summary> Gets the zoom factor, the zoom divided by 100.0 </summary>
    public double ZoomFactor => this.Zoom / 100.0;

    /// <summary> Gets the integer zoom to fit level which shows all the image </summary>
    public double ZoomLevelToFit
    {
        get
        {
            var image = this.Image;
            if (image is null)
            {
                return 100;
            }

            double zoom = Math.Min(this.Bounds.Width / image.Size.Width, this.Bounds.Height / image.Size.Height) * 100.0;
            return zoom <= 0.1 ? 100.0 : zoom;
        }
    }

    /// <summary> Gets the size of the scaled image. </summary>
    public Size ScaledImageSize => new(this.ScaledImageWidth, this.ScaledImageHeight);

    /// <summary> Gets the width of the scaled image. </summary>
    public double ScaledImageWidth => this.Image?.Size.Width * this.ZoomFactor ?? 0;

    /// <summary> Gets the height of the scaled image. </summary>
    public double ScaledImageHeight => this.Image?.Size.Height * this.ZoomFactor ?? 0;

    public bool HasSelection => this.SelectionRegion != default;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (this.ViewPort is not null)
        {
            this.ViewPort.PointerPressed -= this.ViewPortOnPointerPressed;
            this.ViewPort.PointerExited -= this.ViewPortOnPointerExited;
            this.ViewPort.PointerMoved -= this.ViewPortOnPointerMoved;
            this.ViewPort.PointerWheelChanged -= this.ViewPortOnPointerWheelChanged;
        }

        this.HorizontalScrollBar?.Scroll -= this.ScrollBarOnScroll;
        this.VerticalScrollBar?.Scroll -= this.ScrollBarOnScroll;

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
                double zoomLevelToFit = this.ZoomLevelToFit;
                if (this.Zoom < zoomLevelToFit)
                {
                    this.Zoom = zoomLevelToFit;
                }
            }

            this.InvalidateVisual();
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
                    double zoomLevelToFit = this.ZoomLevelToFit;
                    if (this.Zoom < zoomLevelToFit)
                    {
                        this.Zoom = zoomLevelToFit;
                    }
                }
            }

            this.InvalidateVisual();
        }
        else if (ReferenceEquals(e.Property, SizeModeProperty))
        {
            this.SizeModeChanged();
            this.InvalidateVisual();
        }
        else if (ReferenceEquals(e.Property, ZoomProperty))
        {
            this.UpdateViewPort();
            this.InvalidateVisual();
        }
        else if (ReferenceEquals(e.Property, PaddingProperty))
        {
            this.UpdateViewPort();
            this.InvalidateVisual();
        }
        else if (ReferenceEquals(e.Property, SelectionColorProperty))
        {
            this._selectionBorderPen = null;
        }
    }

    private Pen EnsurePixelGridPen() => this._pixelGridPen ??= new Pen(this.PixelGridColor);

    private Pen EnsureSelectionBorderPen()
    {
        if (this._selectionBorderPen is not null)
        {
            return this._selectionBorderPen;
        }

        var color = this.SelectionColor.Color;
        return this._selectionBorderPen = new Pen(Color.FromArgb(255, color.R, color.G, color.B).ToUInt32());
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = this.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var image = this.Image;
        if (image is null)
        {
            return;
        }

        // No way to check if Bitmap is disposed ? 

        try
        {
            // Draw image
            Rect imageViewPort = this.GetImageViewPort();
            if (imageViewPort == default)
            {
                return; 
            } 

            context.DrawImage(image, this.GetSourceImageRegion(), imageViewPort);

            // Draw pixel grid
            double zoomFactor = this.ZoomFactor;
            if (this.SizeMode == SizeModes.Normal && zoomFactor > this.PixelGridZoomThreshold)
            {
                double offsetX = this.Offset.X % zoomFactor;
                double offsetY = this.Offset.Y % zoomFactor;
                double left = imageViewPort.X;
                double top = imageViewPort.Y;
                double right = imageViewPort.Right;
                double bottom = imageViewPort.Bottom;

                var pixelGridPen = this.EnsurePixelGridPen();

                // First vertical line position aligned to zoom steps
                double startX = left + zoomFactor - offsetX;
                for (double x = startX; x < right; x += zoomFactor)
                {
                    context.DrawLine(pixelGridPen, new Point(x, top), new Point(x, bottom));
                }

                // First horizontal line position aligned to zoom steps
                double startY = top + zoomFactor - offsetY;
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
        catch (Exception ex)
        {
            // We got so far:  System.ObjectDisposedException: Cannot access a disposed object.
            Debug.WriteLine(ex);
            if ( Debugger.IsAttached) {  Debugger.Break(); }
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

        double scaledImageWidth = this.ScaledImageWidth;
        double scaledImageHeight = this.ScaledImageHeight;
        double width = Math.Max(0, scaledImageWidth - horizontalScrollBar.ViewportSize);
        double height = Math.Max(0, scaledImageHeight - verticalScrollBar.ViewportSize);

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

        return changed;
    }


    private void ScrollBarOnScroll(object? sender, ScrollEventArgs e) => this.InvalidateVisual();

    private void ViewPortOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!this.IsImageLoaded || this.SizeMode != SizeModes.Normal)
        {
            return;
        }

        // Process horizontal scroll
        if (e.Delta.X != 0 && this.IsHorizontalBarVisible)
        {
            int factor = (e.KeyModifiers & KeyModifiers.Alt) != 0 ? this.HorizontalScrollWithMouseAlternativeFactor : this.HorizontalScrollWithMouseFactor;
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

            int factor = (e.KeyModifiers & KeyModifiers.Alt) != 0 ? this.VerticalScrollWithMouseAlternativeFactor : this.VerticalScrollWithMouseFactor;
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

        var zoomWithMouseWheelKeyModifier = this.ZoomWithMouseWheelKeyModifier;
        bool canZoom = this.ZoomWithMouseWheelStrictKeyModifier switch
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
        int zoomWithMouseWheelDebounceMilliseconds = this.ZoomWithMouseWheelDebounceMilliseconds;
        if (zoomWithMouseWheelDebounceMilliseconds > 0 && e.Timestamp - this._lastZoomWithMouseWheelTimestamp < (ulong)zoomWithMouseWheelDebounceMilliseconds)
        {
            return;
        }

        // TODO ?
        // The MouseWheel event can contain multiple "spins" of the wheel so we need to adjust accordingly

        // TODO => Fix this ugly hack 
        double mouseWheelSensitivityCorrection = Math.Sqrt(Math.Sqrt(this.Zoom));
        if (this.Zoom > 800)
        {
            mouseWheelSensitivityCorrection *= 12.0;
        }
        else if (this.Zoom > 500)
        {
            mouseWheelSensitivityCorrection *= 7.0;
        }
        else if (this.Zoom > 200)
        {
            mouseWheelSensitivityCorrection *= 5.0;
        }
        else if (this.Zoom > 100)
        {
            mouseWheelSensitivityCorrection *= 2.5;
        }

        double mouseWheelSensitivity = 2.0 + mouseWheelSensitivityCorrection;
        Point relativePoint = e.GetPosition(this.ViewPort);
        switch (mouseWheelBehaviour)
        {
            case MouseWheelZoomBehaviours.ZoomNative:
                this.SetZoom(this.Zoom + (e.Delta.Y * mouseWheelSensitivity), true, relativePoint);
                break;
            case MouseWheelZoomBehaviours.ZoomNativeAltLevels:
                if ((e.KeyModifiers & KeyModifiers.Alt) == 0)
                {
                    this.SetZoom(this.Zoom + (e.Delta.Y * mouseWheelSensitivity), true, relativePoint);
                }
                else
                {
                    this.PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, true, relativePoint);
                }
                break;

            case MouseWheelZoomBehaviours.ZoomLevels:
                this.PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, true, relativePoint);
                break;

            case MouseWheelZoomBehaviours.ZoomLevelsAltNative:
                if ((e.KeyModifiers & KeyModifiers.Alt) == 0)
                {
                    this.PerformZoom(e.Delta.Y > 0 ? ZoomActions.ZoomIn : ZoomActions.ZoomOut, true, relativePoint);
                }
                else
                {
                    this.SetZoom(this.Zoom + (e.Delta.Y * mouseWheelSensitivity), true, relativePoint);
                }
                break;
        }

        this._lastZoomWithMouseWheelTimestamp = e.Timestamp;
    }

    private void ViewPortOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled || this._isPanning || this._isSelecting || this.Image is null)
        {
            return;
        }

        var pointer = e.GetCurrentPoint(this);
        if (pointer.Properties.IsRightButtonPressed)
        {
            this.ZoomToFit();
            e.Handled = true;
            return;
        }

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
        if ((location.X > this.Viewport.Width) || (location.Y > this.Viewport.Height))
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

        // TODO 
        // DO something with the selection 
    }

    private void ViewPortOnPointerExited(object? sender, PointerEventArgs e)
    {
        this.InvalidateVisual();
        e.Handled = true;
    }

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

        if (!this._isPanning && !this._isSelecting)
        {
            this.InvalidateVisual();
            return;
        }

        var pointer = e.GetCurrentPoint(viewPort);
        var pointerPosition = pointer.Position;
        double px = pointerPosition.X;
        double py = pointerPosition.Y;
        if (this._isPanning)
        {
            double x;
            double y;
            if (this.InvertMousePan)
            {
                x = this._startScrollPosition.X - (this._startMousePosition.X - px);
                y = this._startScrollPosition.Y - (this._startMousePosition.Y - py);
            }
            else
            {
                x = this._startScrollPosition.X + (this._startMousePosition.X - px);
                y = this._startScrollPosition.Y + (this._startMousePosition.Y - py);
            }

            this.Offset = new Vector(x, y);
        }
        else if (this._isSelecting)
        {
            var bounds = viewPort.Bounds;
            var viewPortPoint = new Point(Math.Min(px, bounds.Right), Math.Min(py, bounds.Bottom));

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

            double zoomFactor = this.ZoomFactor;
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

        bool panLeft = false;
        bool panUp = false;
        bool panRight = false;
        bool panDown = false;

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

    /// <summary> Resets the <see cref="SizeModes"/> property whilsts retaining the original <see cref="Zoom"/>. </summary>
    protected void RestoreSizeMode()
    {
        if (this.SizeMode != SizeModes.Normal)
        {
            double previousZoom = this.Zoom;
            this.SizeMode = SizeModes.Normal;
            this.Zoom = previousZoom; // Stop the zoom getting reset to 100% before calculating the new zoom
        }
    }

    // TODO: Create a property for this one 
    const double zoomMultiplier = 1.11;

    /// <summary> Returns an appropriate zoom level based on the specified action, relative to the current zoom level. </summary>
    /// <param name="action">The action to determine the zoom level.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if an unsupported action is specified.</exception>
    public double GetZoomLevel(ZoomActions action)
        => action switch
        {
            ZoomActions.None => this.Zoom,
            ZoomActions.ZoomIn => this.Zoom * zoomMultiplier,
            ZoomActions.ZoomOut => this.Zoom / zoomMultiplier,
            ZoomActions.ActualSize => 100.0,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    /// <summary> Performs the specified zoom action. </summary>
    /// <param name="action"></param>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void PerformZoom(ZoomActions action, bool preservePosition = true, Point? relativePoint = null)
        => this.SetZoom(this.GetZoomLevel(action), preservePosition, relativePoint);

    /// <summary> Sets the zoom level to the specified value. </summary>
    /// <param name="zoom"></param>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    /// <param name="relativePoint">Preserve position at given relative point. If null, <see cref="CenterPoint"/>> will be used.</param>
    public void SetZoom(double zoom, bool preservePosition = true, Point? relativePoint = null)
    {
        relativePoint ??= this.CenterPoint;
        double currentZoom = this.Zoom;
        Point currentPixel = this.PointToImage(relativePoint.Value);
        this.RestoreSizeMode();
        this.Zoom = zoom;
        if (preservePosition && this.Zoom != currentZoom)
        {
            this.ScrollTo(currentPixel, relativePoint.Value);
        }
    }

    /// <summary> Zooms into the image </summary>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    public void ZoomIn(bool preservePosition = true) => this.PerformZoom(ZoomActions.ZoomIn, preservePosition);

    /// <summary> Zooms out of the image </summary>
    /// <param name="preservePosition"><c>true</c> if the current scrolling position should be preserved relative to the new zoom level, <c>false</c> to reset.</param>
    public void ZoomOut(bool preservePosition = true) => this.PerformZoom(ZoomActions.ZoomOut, preservePosition);

    /// <summary> Zooms to the maximum size for displaying the entire image within the bounds of the control. </summary>
    public void ZoomToFit()
    {
        this.Zoom = this.ZoomLevelToFit;
        this.InvalidateVisual();
    }

    /// <summary> Adjusts the view port to fit the given region </summary>
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

        double ratioX = this.Viewport.Width / rectangle.Width;
        double ratioY = this.Viewport.Height / rectangle.Height;
        double zoomFactor = Math.Min(ratioX, ratioY);
        double cx = rectangle.X + rectangle.Width / 2;
        double cy = rectangle.Y + rectangle.Height / 2;
        this.Zoom = (int)(zoomFactor * 100); // This function sets the zoom so viewport will change

        //Dispatcher.UIThread.Post(() => CenterAt(new Point(cx, cy)));
        this.CenterAt(new Point(cx, cy)); // If I call this here, it will move to the wrong position due wrong viewport, dispatcher would solve but slower?
    }

    /// <summary> Zooms to current selection region </summary>
    public void ZoomToSelectionRegion(double margin = 0)
    {
        if (!this.HasSelection)
        {
            return;
        }

        this.ZoomToRegion(this.SelectionRegion, margin);
    }

    /// <summary> Resets the zoom to 100%. </summary>
    public void PerformActualSize()
    {
        this.SizeMode = SizeModes.Normal;
        this.Zoom = 100;
    }

    /// <summary> Determines whether the specified point is located within the image view port </summary>
    /// <param name="point">The point.</param>
    /// <returns> <c>true</c> if the specified point is located within the image view port; otherwise, <c>false</c>. </returns>
    public bool IsPointInImage(Point point) => this.GetImageViewPort().Contains(point);

    /// <summary> Converts the given client size point to represent a coordinate on the source image. </summary>
    /// <param name="point">The source point.</param>
    /// <param name="fitToBounds">
    ///   if set to <c>true</c> and the point is outside the bounds of the source image, it will be mapped to the nearest edge.
    /// </param>
    /// <returns><c>Point.Empty</c> if the point could not be matched to the source image, otherwise the new translated point</returns>
    public Point PointToImage(Point point, bool fitToBounds = true)
    {
        if (this.Image is null)
        {
            return new(0, 0);
        }

        var viewport = this.GetImageViewPort();
        if (fitToBounds && !viewport.Contains(point))
        {
            return new(0, 0);
        }

        double x = (point.X + this.Offset.X - viewport.X) / this.ZoomFactor;
        double y = (point.Y + this.Offset.Y - viewport.Y) / this.ZoomFactor;
        var imageSize = this.Image.Size;
        if (fitToBounds)
        {
            x = Math.Clamp(x, 0, imageSize.Width - 1);
            y = Math.Clamp(y, 0, imageSize.Height - 1);
        }
        return new(x, y);
    }

    /// <summary>
    ///   Returns the source <see cref="Point"/> repositioned to include the current image offset and scaled by the current zoom level
    /// </summary>
    /// <param name="source">The source <see cref="PointF"/> to offset.</param>
    /// <returns>A <see cref="PointF"/> which has been repositioned to match the current zoom level and image offset</returns>
    public Point GetOffsetPoint(Point source)
    {
        Rect viewport = this.GetImageViewPort();
        var scaled = this.GetScaledPoint(source);
        double offsetX = viewport.Left - this.Offset.X;
        double offsetY = viewport.Top - this.Offset.Y;

        return new(scaled.X + offsetX, scaled.Y + offsetY);
    }

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="source">The source <see cref="RectangleF"/> to offset.</param>
    /// <returns>A <see cref="RectangleF"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    public Rect GetOffsetRectangle(Rect source) => this.GetOffsetRectangle(source, this.GetImageViewPort());

    /// <summary>
    ///   Returns the source <see cref="T:System.Drawing.RectangleF" /> scaled according to the current zoom level and repositioned to include the current image offset
    /// </summary>
    /// <param name="source">The source <see cref="Rect"/> to offset.</param>
    /// <param name="imageViewPort">The image viewport to use for the offset calculation.</param>
    /// <returns>A <see cref="Rect"/> which has been resized and repositioned to match the current zoom level and image offset</returns>
    private Rect GetOffsetRectangle(Rect source, Rect imageViewPort)
    {
        var viewport = imageViewPort;
        var scaled = this.GetScaledRectangle(source);
        double offsetX = viewport.Left - this.Offset.X;
        double offsetY = viewport.Top - this.Offset.Y;

        return new(new Point(scaled.Left + offsetX, scaled.Top + offsetY), scaled.Size);
    }

    /// <summary> Fits a given <see cref="Rect" /> to match image boundaries </summary>
    /// <param name="rectangle">The rectangle.</param>
    /// <returns> A <see cref="Rect" /> structure remapped to fit the image boundaries. </returns>
    public Rect FitRectangle(Rect rectangle)
    {
        var image = this.Image;
        if (image is null)
        {
            return default;
        }

        double x = rectangle.X;
        double y = rectangle.Y;
        double w = rectangle.Width;
        double h = rectangle.Height;

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

    /// <summary> Scrolls the control to the given point in the image, offset at the specified display point </summary>
    /// <param name="imageLocation">The point of the image to attempt to scroll to.</param>
    /// <param name="relativeDisplayPoint">The relative display point to offset scrolling by.</param>
    public void ScrollTo(Point imageLocation, Point relativeDisplayPoint)
    {
        double zoomFactor = this.ZoomFactor;
        double x = imageLocation.X * zoomFactor - relativeDisplayPoint.X;
        double y = imageLocation.Y * zoomFactor - relativeDisplayPoint.Y;
        this.Offset = new Vector(x, y);

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

    /// <summary> Centers the given point in the image in the center of the control </summary>
    /// <param name="imageLocation">The point of the image to attempt to center.</param>
    public void CenterAt(Point imageLocation)
        => this.ScrollTo(imageLocation, new Point(this.Viewport.Width / 2, this.Viewport.Height / 2));

    /// <summary> Resets the viewport to show the center of the image. </summary>
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

    /// <summary> Returns the source <see cref="Point" /> scaled according to the current zoom level. </summary>
    /// <param name="source">The source <see cref="Point"/> to scale.</param>
    /// <returns>A <see cref="Point"/> which has been scaled to match the current zoom level</returns>
    public Point GetScaledPoint(Point source) => new(source.X * this.ZoomFactor, source.Y * this.ZoomFactor);

    /// <summary> Returns the source rectangle scaled according to the current zoom level </summary>
    /// <param name="source">The source <see cref="Rect"/> to scale.</param>
    /// <returns>A <see cref="Rect"/> which has been scaled to match the current zoom level</returns>
    public Rect GetScaledRectangle(Rect source)
        => new(source.Left * this.ZoomFactor, source.Top * this.ZoomFactor, source.Width * this.ZoomFactor, source.Height * this.ZoomFactor);

    /// <summary> Returns the source size scaled according to the current zoom level </summary>
    /// <param name="source">The source <see cref="Size"/> to scale.</param>
    /// <returns>A <see cref="Size"/> which has been resized to match the current zoom level</returns>
    public Size GetScaledSize(Size source) => new(source.Width * this.ZoomFactor, source.Height * this.ZoomFactor);

    /// <summary> Creates a selection region which encompasses the entire image </summary>
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

    /// <summary> Clears any existing selection region </summary>
    public void SelectNone() => this.SelectionRegion = default;

    /// <summary> Gets the source image region. </summary>
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
                double zoomFactor = this.ZoomFactor;
                double sourceLeft = (offset.X / zoomFactor);
                double sourceTop = (offset.Y / zoomFactor);
                double sourceWidth = (viewPort.Width / zoomFactor);
                double sourceHeight = (viewPort.Height / zoomFactor);

                return new(sourceLeft, sourceTop, sourceWidth, sourceHeight);
        }

        return new(0, 0, image.Size.Width, image.Size.Height);

    }

    /// <summary> Gets the image view port. </summary>
    /// <returns>The image viewport rectangle.</returns>
    public Rect GetImageViewPort()
    {
        var image = this.Image;
        if (image is null)
        {
            return default;
        }

        var viewPortSize = this.Viewport;
        if (viewPortSize is { Width: 0, Height: 0 })
        {
            return default;
        }

        double xOffset = 0.0;
        double yOffset = 0.0;
        double width;
        double height;

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
                double scaleFactor = 
                    Math.Min(
                        (viewPortSize.Width - padding.Left - padding.Right) / image.Size.Width, 
                        (viewPortSize.Height - padding.Top - padding.Bottom) / image.Size.Height);

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

    public WriteableBitmap? GetSelectedBitmap()
    {
        if (this.Image is not WriteableBitmap image || !this.HasSelection)
        {
            return null;
        }

        Rect selection = this.SelectionRegion;
        using var srcBuffer = image.Lock();

        // Clamp selection to actual image bounds to prevent buffer overread in unsafe copy.
        // Math.Ceiling on X/Y in SelectionRegionPixel can push Right/Bottom one pixel past the image edge.
        int clampedX = Math.Max(0, (int)selection.X);
        int clampedY = Math.Max(0, (int)selection.Y);
        int clampedWidth = Math.Min((int)selection.Right, srcBuffer.Size.Width) - clampedX;
        int clampedHeight = Math.Min((int)selection.Bottom, srcBuffer.Size.Height) - clampedY;
        if (clampedWidth <= 0 || clampedHeight <= 0)
        {
            return null;
        }

        var clampedSelection = new PixelRect(clampedX, clampedY, clampedWidth, clampedHeight);
        var cropBitmap = new WriteableBitmap(clampedSelection.Size, image.Dpi, srcBuffer.Format, AlphaFormat.Unpremul);
        using var dstBuffer = cropBitmap.Lock();
        unsafe
        {
            nint ySrc = srcBuffer.Address + srcBuffer.RowBytes * clampedSelection.Y + clampedSelection.X * (srcBuffer.Format.BitsPerPixel / 8);
            nint yDst = dstBuffer.Address;

            for (int y = clampedSelection.Y; y < clampedSelection.Bottom; y++)
            {
                Buffer.MemoryCopy(ySrc.ToPointer(), yDst.ToPointer(), dstBuffer.RowBytes, dstBuffer.RowBytes);
                ySrc += srcBuffer.RowBytes;
                yDst += dstBuffer.RowBytes;
            }
        }

        return cropBitmap;
    }
}