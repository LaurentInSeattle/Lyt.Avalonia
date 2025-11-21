namespace Lyt.Avalonia.Mvvm.Behaviors.DragDrop;

public sealed class InProcessDataTransfer(object inProcessData) : IDataTransfer
{
    private object? inProcessData = inProcessData;

    public object InProcessData => this.inProcessData!;

    IReadOnlyList<DataFormat> IDataTransfer.Formats => [];
    
    IReadOnlyList<IDataTransferItem> IDataTransfer.Items => [];
    
    void IDisposable.Dispose() => this.inProcessData = null;
}