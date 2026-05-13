namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Storage-level diagnostics returned by the backend.
/// </summary>
public class StructuredLogStorageDiagnostics
{
    public long DroppedWriteCount { get; set; }
}
