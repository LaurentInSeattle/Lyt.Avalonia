namespace Lyt.Avalonia.Mvvm.Behaviors.DragDrop;

public sealed class InProcessDataTransfer(object inProcessData) : IDataTransfer
{
    public object InProcessData { get; set; } = inProcessData;

    IReadOnlyList<DataFormat> IDataTransfer.Formats => [];
    
    IReadOnlyList<IDataTransferItem> IDataTransfer.Items => [];
    
    void IDisposable.Dispose() { }
}