using Avalonia.Controls.Metadata;

using Size = Avalonia.Size;

namespace Lyt.Avalonia.Controls.Images;

[TemplatePart("PART_MainGrid", typeof(Grid))]
[TemplatePart("PART_ContentPresenter", typeof(ScrollContentPresenter))] // ViewPort
[TemplatePart("PART_HorizontalScrollBar", typeof(ScrollBar))]
[TemplatePart("PART_VerticalScrollBar", typeof(ScrollBar))]
[TemplatePart("PART_ScrollBarsSeparator", typeof(Panel))]
public partial class ZoomableImage : TemplatedControl, IScrollable
{
    /// <inheritdoc />
    public Size Extent
    {
        get
        {
            var viewPort = this.ViewPort;
            if (viewPort is null)
            {
                return default;
            }

            return 
                new(Math.Max(viewPort.Bounds.Width, this.ScaledImageWidth),
                Math.Max(viewPort.Bounds.Height, this.ScaledImageHeight));
        }
    }

    /// <inheritdoc />
    public Vector Offset
    {
        get
        {
            var horizontalScrollBar = this.HorizontalScrollBar;
            if (horizontalScrollBar is null)
            {
                return default;
            }

            var verticalScrollBar = this.VerticalScrollBar;
            if (verticalScrollBar is null)
            {
                return default;
            }

            return new Vector(horizontalScrollBar.Value, verticalScrollBar.Value);
        }
        set
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

            horizontalScrollBar.Value = value.X;
            verticalScrollBar.Value = value.Y;
            this.RaisePropertyChanged();
            this.TriggerRender();
        }
    }

    /// <inheritdoc />
    public Size Viewport => this.ViewPort?.Bounds.Size ?? this.Bounds.Size;

    /// <inheritdoc />
    public bool CanHorizontallyScroll => this.IsHorizontalBarVisible;

    /// <inheritdoc />
    public bool CanVerticallyScroll => this.IsVerticalBarVisible;
}
