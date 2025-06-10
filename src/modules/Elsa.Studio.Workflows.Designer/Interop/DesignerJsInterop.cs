using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Size = Elsa.Api.Client.Shared.Models.Size;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides access to the designer JavaScript module.
/// </summary>
public class DesignerJsInterop(IJSRuntime jsRuntime, IOptions<DesignerOptions> options, IServiceProvider serviceProvider) : JsInteropBase(jsRuntime)
{
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
        return await TryInvokeAsync(async module =>
        {
            var graphSettings = options.Value.GraphSettings;
            await module.InvokeAsync<string>("createGraph", containerId, componentRef, isReadOnly, graphSettings);
            return new X6GraphApi(module, serviceProvider, containerId);
        });
    }

    public async Task UpdateActivitySizeAsync(string elementId, JsonObject activity, Size? size = null)
    {
        var serializerOptions = GetSerializerOptions();
        var activityJson = JsonSerializer.Serialize(activity, serializerOptions);
        await TryInvokeAsync(module => module.InvokeVoidAsync("updateActivitySize", elementId, activityJson, size));
    }

    public async Task UpdateActivityStatsAsync(string elementId, string activityId, ActivityStats stats) =>
        await TryInvokeAsync(module => module.InvokeVoidAsync("updateActivityStats", elementId, activityId, stats));

    public async Task RaiseActivitySelectedAsync(string elementId, JsonObject activity)
    {
        var serializerOptions = GetSerializerOptions();
        var activityJson = JsonSerializer.Serialize(activity, serializerOptions);
        await TryInvokeAsync(module => module.InvokeVoidAsync("raiseActivitySelected", elementId, activityJson));
    }

    public async Task RaiseActivityEmbeddedPortSelectedAsync(string elementId, JsonObject activity, string portName)
    {
        var serializerOptions = GetSerializerOptions();
        var activityJson = JsonSerializer.Serialize(activity, serializerOptions);
        await TryInvokeAsync(module => module.InvokeVoidAsync("raiseActivityEmbeddedPortSelected", elementId, activityJson, portName));
    }

    private static JsonSerializerOptions GetSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        return serializerOptions;
    }
}