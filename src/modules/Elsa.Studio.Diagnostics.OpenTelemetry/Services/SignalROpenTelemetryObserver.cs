using Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Models;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

/// <summary>
/// Placeholder live observer. SignalR connection management is implemented with the trace-investigation story.
/// </summary>
public class SignalROpenTelemetryObserver : IOpenTelemetryObserver
{
    public async IAsyncEnumerable<OpenTelemetryStreamItem> ObserveAsync(OpenTelemetryTraceFilter filter, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
