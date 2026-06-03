using Elsa.Studio.Dashboard.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Dashboard.Components;

public abstract class DashboardAsyncWidgetBase<TValue> : ComponentBase, IAsyncDisposable
{
    private CancellationTokenSource? _loadCancellationTokenSource;
    private DashboardWidgetContext? _loadedContext;

    [CascadingParameter] public DashboardWidgetContext? WidgetContext { get; set; }

    protected TValue? Value { get; private set; }
    protected string? Message { get; private set; }
    protected bool Loading { get; private set; }

    protected override async Task OnParametersSetAsync()
    {
        if (WidgetContext == null || _loadedContext == WidgetContext)
            return;

        await LoadAsync(WidgetContext);
    }

    protected abstract Task<TValue> LoadValueAsync(DashboardWidgetContext context, CancellationToken cancellationToken);

    private async Task LoadAsync(DashboardWidgetContext context)
    {
        await CancelCurrentLoadAsync();

        var cancellationTokenSource = new CancellationTokenSource();
        _loadCancellationTokenSource = cancellationTokenSource;
        Loading = true;
        Message = null;
        _loadedContext = context;

        try
        {
            Value = await LoadValueAsync(context, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
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
