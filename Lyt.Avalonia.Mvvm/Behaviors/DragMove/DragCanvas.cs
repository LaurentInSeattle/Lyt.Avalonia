namespace Lyt.Avalonia.Mvvm.Behaviors.DragMove;

public sealed class DragCanvas : Canvas
{
    private readonly NestedDictionary<int, int, List<DragMovable>> dragMovables = [];
    private int bucketSize; 

    public DragCanvas() : base()
    {
        this.Background = Brushes.Transparent;
        this.PointerPressed += this.OnPointerPressed;
        // this.PointerMoved += this.OnPointerMoved;
    }

    public void InitializeBuckets(int bucketSize)
    {
        this.bucketSize = bucketSize;
        if (dragMovables.Count > 0)
        {
            this.dragMovables.Clear();
        }

        foreach (var item in this.Children)
        {
            if ((item is View view) && (view is IDragMovableView dragMovableView))
            {
                var dragMovable = dragMovableView.DragMovable;
                Point position = dragMovableView.GetCenterLocation;
                int x = (int)position.X;
                int y = (int)position.Y;
                Debug.WriteLine("Canvas: Position at: " + x.ToString() + " - " + y.ToString());
                int bucketIdX = x / this.bucketSize;
                int bucketIdY = y / this.bucketSize;
                if (!this.dragMovables.TryGetValue(bucketIdX, bucketIdY, out List<DragMovable>? bucket))
                {
                    bucket = [];
                    this.dragMovables.Add(bucketIdX, bucketIdY, bucket);
                }

                bucket?.Add(dragMovable);
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
        Debug.WriteLine("Canvas: Pressed at: " + x.ToString() + " - " + y.ToString());

        int bucketIdX = x / this.bucketSize;
        int bucketIdY = y / this.bucketSize;
        if (this.dragMovables.TryGetValue(bucketIdX, bucketIdY, out List<DragMovable>? bucket))
        {
            if (bucket is not null)
            {
                if (bucket.Count == 0)
                {
                    Debug.WriteLine("Empty bucket");
                }
                else if (bucket.Count == 1)
                {
                    var dragMovable = bucket[0];
                    dragMovable.OnPointerPressed(sender, pointerPressedEventArgs);
                }
                else
                {
                    // TODO:
                    // If there are multiple items in the bucket,
                    // we need to check which one is actually under the pointer.
                }
            }
        }
    }
}
