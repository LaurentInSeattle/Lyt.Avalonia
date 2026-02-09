namespace Lyt.Avalonia.Mvvm.Behaviors.DragMove;

public sealed class DragCanvas : Canvas
{
    private readonly Dictionary<int, List<DragMovable>> dragMovables = [];

    public DragCanvas() : base()
    {
        // this.PointerPressed += this.OnPointerPressed;
        // this.PointerMoved += this.OnPointerMoved;
    }

    public void InitializeBuckets()
    {
        if(dragMovables.Count > 0)
        {
            this.dragMovables.Clear();
        }

        foreach (var item in this.Children)
        {
            if ((item is View view) && (view is IDragMovableView dragMovableView))
            {
                var dragMovable = dragMovableView.DragMovable;
                Point position = dragMovableView.GetCenterPosition;
                int x = (int)position.X;
                int y = (int)position.Y;
                Debug.WriteLine("Canvas: Position at: " + x.ToString() + " - " + y.ToString());
                int bucketId  = (x / 128) + (( y / 64) << 8) ;
                if (!this.dragMovables.TryGetValue(bucketId, out List<DragMovable>? bucket))
                {
                    bucket = [];
                    this.dragMovables.Add(bucketId, bucket);
                }

                bucket.Add(dragMovable);
            }
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs pointerEventArgs)
    {
        var position = pointerEventArgs.GetPosition(this);
        int x = (int)position.X;
        int y = (int)position.Y;
        Debug.WriteLine("Canvas: Moved at: " + x.ToString() + " - " + y.ToString());
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs pointerPressedEventArgs)
    {
        var position = pointerPressedEventArgs.GetPosition(this);
        int x = (int)position.X;
        int y = (int)position.Y;
        Debug.WriteLine("Canvas: Pressed at: " + x.ToString() + " - " + y.ToString() );

        int bucketId = (x / 128) + ((y / 64) << 8);
        if (this.dragMovables.TryGetValue(bucketId, out List<DragMovable>? bucket))
        {
            if ( bucket is not null && bucket.Count == 1 )
            {
                var dragMovable = bucket[0];
                var view = dragMovable.View;
                // view.IsVisible = false;
            }
        }
    }
}
