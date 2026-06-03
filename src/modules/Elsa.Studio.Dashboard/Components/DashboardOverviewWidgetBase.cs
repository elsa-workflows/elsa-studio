using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Dashboard.Components;

public abstract class DashboardOverviewWidgetBase : ComponentBase, IAsyncDisposable
{
    private CancellationTokenSource? _loadCancellationTokenSource;
    private DashboardWidgetContext? _loadedContext;

    [Inject] private IDashboardService DashboardService { get; set; } = null!;
    [CascadingParameter] public DashboardWidgetContext? WidgetContext { get; set; }

    protected DashboardOverview? Overview { get; private set; }
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
            var result = await DashboardService.LoadOverviewAsync(context.Range, cancellationToken: cancellationTokenSource.Token);
            Status = result.Status;
            _loadedContext = context;

            if (result.Value != null)
            {
                Overview = result.Value;
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
