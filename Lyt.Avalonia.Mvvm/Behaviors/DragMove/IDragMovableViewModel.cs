namespace Lyt.Avalonia.Mvvm.Behaviors.DragMove;

/// <summary> Interface contract for view models that have a view that can be moved </summary>
public interface IDragMovableViewModel
{
    void OnEntered();

    void OnExited();

    void OnLongPress();

    void OnClicked(bool isRightClick);

    // Returns true if move is currently allowed 
    bool OnBeginMove(Point fromPoint);

    void OnMove(Point fromPoint, Point toPoint);

    void OnEndMove(Point fromPoint, Point toPoint);
}
