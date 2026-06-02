using Elsa.Studio.Dashboard.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Dashboard.Components;

public abstract class WorkflowDashboardWidgetBase : ComponentBase, IAsyncDisposable
{
    private CancellationTokenSource? _loadCancellationTokenSource;
    private DashboardWidgetContext? _loadedContext;

    [Inject] private IWorkflowDashboardDataProvider DataProvider { get; set; } = null!;
    [CascadingParameter] public DashboardWidgetContext? WidgetContext { get; set; }

    protected DashboardSnapshot? Snapshot { get; private set; }
    protected DashboardLoadStatus Status { get; private set; } = DashboardLoadStatus.Unavailable;
    protected string? Message { get; private set; }
    protected bool Loading { get; private set; }

    protected override async Task OnParametersSetAsync()
    {
        if (WidgetContext == null || _loadedContext == WidgetContext)
            return;

        await LoadAsync(WidgetContext);
    }

    private async Task LoadAsync(DashboardWidgetContext context)
    {
        await CancelCurrentLoadAsync();

        var cancellationTokenSource = new CancellationTokenSource();
        _loadCancellationTokenSource = cancellationTokenSource;
        Loading = true;
        Message = null;

        try
        {
            var result = await DataProvider.LoadAsync(context, cancellationTokenSource.Token);
            Status = result.Status;
            _loadedContext = context;

            if (result.Snapshot != null)
            {
                Snapshot = result.Snapshot;
                Message = null;
            }
            else
            {
                Message = result.Message;
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Status = DashboardLoadStatus.Failed;
            Message = e.Message;
        }
        finally
        {
            if (ReferenceEquals(_loadCancellationTokenSource, cancellationTokenSource))
            {
                Loading = false;
                _loadCancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
        }
    }

    protected async Task CancelCurrentLoadAsync()
    {
        if (_loadCancellationTokenSource == null)
            return;

        await _loadCancellationTokenSource.CancelAsync();
        _loadCancellationTokenSource = null;
    }

    public async ValueTask DisposeAsync()
    {
        await CancelCurrentLoadAsync();
    }
}
