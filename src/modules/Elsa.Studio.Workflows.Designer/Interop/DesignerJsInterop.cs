using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.UI.Models;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
internal class DesignerJsInterop : JsInteropBase
{
    private readonly IServiceProvider _serviceProvider;

    public DesignerJsInterop(IJSRuntime jsRuntime, IServiceProvider serviceProvider) : base(jsRuntime)
    {
        _serviceProvider = serviceProvider;
    }

    protected override string ModuleName => "designer";

    /// <summary>
    /// Creates a new X6 graph object and returns its ID.
    /// </summary>
    /// <param name="containerId">The ID of the container element.</param>
    /// <param name="componentRef">A reference to the <see cref="FlowchartDesigner"/> component.</param>
    /// <param name="isReadOnly">Whether the graph is read-only.</param>
    /// <returns>The ID of the graph.</returns>
    public async ValueTask<X6GraphApi> CreateGraphAsync(string containerId, DotNetObjectReference<FlowchartDesigner> componentRef, bool isReadOnly = false)
    {
        return await InvokeAsync(async module =>
        {
            await module.InvokeAsync<string>("createGraph", containerId, componentRef, isReadOnly);
            return new X6GraphApi(module, _serviceProvider, containerId);
        });
    }

    public async Task UpdateActivitySizeAsync(string elementId, JsonObject activity, Size? size = default) =>
        await TryInvokeAsync(module => module.InvokeVoidAsync("updateActivitySize", elementId, activity, size));
    
    public async Task UpdateActivityStatsAsync(string elementId, string activityId, ActivityStats stats) =>
        await TryInvokeAsync(module => module.InvokeVoidAsync("updateActivityStats", elementId, activityId, stats));
    
    public async Task RaiseActivitySelectedAsync(string elementId, JsonObject activity) =>
        await TryInvokeAsync(module => module.InvokeVoidAsync("raiseActivitySelected", elementId, activity));
    
    public async Task RaiseActivityEmbeddedPortSelectedAsync(string elementId, JsonObject activity, string portName) =>
        await TryInvokeAsync(module => module.InvokeVoidAsync("raiseActivityEmbeddedPortSelected", elementId, activity, portName));
}