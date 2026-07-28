namespace Lyt.Avalonia.Controls.Images;

/// <summary> Determines the sizing mode of an image hosted in an <see cref="ZoomableImage" /> control. </summary>
public enum SizeModes
{
    /// <summary> The image is displayed according to current zoom and scroll properties. </summary>
    Normal,

    /// <summary> The image is stretched to fill the client area of the control. </summary>
    Stretch,

    /// <summary> The image is stretched to fill as much of the client area of the control as possible, whilst retaining the same aspect ratio for the width and height.</summary>
    Fit
}

[Flags]
public enum MouseButtons
{
    None = 0,
    LeftButton = 1,
    MiddleButton = 2,
    RightButton = 4
}

public enum MouseWheelZoomBehaviours
{
    /// <summary> No action is performed when using the mouse wheel. </summary>
    None,

    /// <summary> Zoom in and out in a native way using the mouse wheel delta. </summary>
    ZoomNative,

    /// <summary> Zoom in and out in a native way using the mouse wheel delta, but change to tick levels when holding ALT key. </summary>
    ZoomNativeAltLevels,

    /// <summary> Zoom in and out using tick levels defined in the <see cref="ZoomableImage.ZoomLevels"/> collection. </summary>
    ZoomLevels,

    /// <summary> Zoom in and out using tick levels defined in the <see cref="ZoomableImage.ZoomLevels"/> collection, but change to native when holding ALT key.</summary>
    ZoomLevelsAltNative,
}

/// <summary> Describes the zoom action occurring </summary>
[Flags]
public enum ZoomActions
{
    /// <summary> No action. </summary>
    None = 0,

    /// <summary> The control is increasing the zoom. </summary>
    ZoomIn = 1,

    /// <summary> The control is decreasing the zoom. </summary>
    ZoomOut = 2,

    /// <summary> The control zoom was reset. </summary>
    ActualSize = 4
}

public enum SelectionModes
{
    /// <summary> No selection. </summary>
    None,

    /// <summary> Rectangle selection. </summary>
    Rectangle,

    /// <summary> Zoom selection. </summary>
    Zoom
}
