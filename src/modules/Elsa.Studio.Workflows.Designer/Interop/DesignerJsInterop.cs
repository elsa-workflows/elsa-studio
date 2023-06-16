using System.Text.Json;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
internal class DesignerJsInterop : JsInteropBase
{
    public DesignerJsInterop(IJSRuntime jsRuntime) : base(jsRuntime)
    {
    }

    protected override string ModuleName => "designer";

    /// <summary>
    /// Creates a new X6 graph object and returns its ID.
    /// </summary>
    /// <param name="containerId">The ID of the container element.</param>
    /// <param name="componentRef">A reference to the <see cref="FlowchartDesigner"/> component.</param>
    /// <returns>The ID of the graph.</returns>
    public async ValueTask<X6GraphApi> CreateGraphAsync(string containerId, DotNetObjectReference<FlowchartDesigner> componentRef)
    {
        return await InvokeAsync(async module =>
        {
            await module.InvokeAsync<string>("createGraph", containerId, componentRef);
            return new X6GraphApi(module, containerId);
        });
        
    }

    public async Task UpdateActivityNodeAsync(string elementId, JsonElement activity) =>
        await InvokeAsync(module => module.InvokeVoidAsync("updateActivityNode", elementId, activity));
}