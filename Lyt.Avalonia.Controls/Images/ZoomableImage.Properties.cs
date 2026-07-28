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
    public static readonly DirectProperty<ZoomableImage, bool> CanRenderProperty =
        AvaloniaProperty.RegisterDirect<ZoomableImage, bool>(
            nameof(CanRender),
            o => o.CanRender);

    /// <summary> Gets or sets if control can render the image </summary>
    public bool CanRender
    {
        get => this._canRender;
        set
        {
            if (!this.SetAndRaise(CanRenderProperty, ref this._canRender, value))
            {
                return;
            }

            if (this._canRender)
            {
                this.TriggerRender();
            }
        }
    }

    //public static readonly StyledProperty<byte> GridCellSizeProperty =
    //    AvaloniaProperty.Register<ZoomableImage, byte>(nameof(GridCellSize), 15);

    ///// <summary> Gets or sets the grid cell size </summary>
    //public byte GridCellSize
    //{
    //    get => this.GetValue(GridCellSizeProperty);
    //    set => this.SetValue(GridCellSizeProperty, value);
    //}

    //public static readonly StyledProperty<ISolidColorBrush> GridColorProperty =
    //    AvaloniaProperty.Register<ZoomableImage, ISolidColorBrush>(nameof(GridColor), Brushes.Gainsboro);

    ///// <summary> Gets or sets the color used to create the checkerboard style background </summary>
    //public ISolidColorBrush GridColor
    //{
    //    get => this.GetValue(GridColorProperty);
    //    set => this.SetValue(GridColorProperty, value);
    //}

    //public static readonly StyledProperty<ISolidColorBrush> GridColorAlternateProperty =
    //    AvaloniaProperty.Register<ZoomableImage, ISolidColorBrush>(nameof(GridColorAlternate), Brushes.White);

    ///// <summary> Gets or sets the color used to create the checkerboard style background </summary>
    //public ISolidColorBrush GridColorAlternate
    //{
    //    get => this.GetValue(GridColorAlternateProperty);
    //    set => this.SetValue(GridColorAlternateProperty, value);
    //}

    public static readonly StyledProperty<Bitmap?> ImageProperty =
        AvaloniaProperty.Register<ZoomableImage, Bitmap?>(nameof(Image));

    /// <summary> Gets or sets the image to be displayed </summary>
    public Bitmap? Image
    {
        get => this.GetValue(ImageProperty);
        set
        {
            if (this._imageNeedsDisposal)
            {
                this.Image?.Dispose();
                this._imageNeedsDisposal = false;
            }
            this.SetValue(ImageProperty, value);
        }
    }

    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<ZoomableImage, bool>(nameof(ShowGrid), true);

    /// <summary> Gets or sets the grid visibility when reach high zoom levels </summary>
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

}
