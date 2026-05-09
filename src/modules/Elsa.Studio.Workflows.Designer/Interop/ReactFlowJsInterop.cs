using Elsa.Studio.Workflows.Designer.Components;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Loads the React Flow designer JS bundle and exposes its createReactGraph entry point.
/// </summary>
public class ReactFlowJsInterop(IJSRuntime jsRuntime) : JsInteropBase(jsRuntime)
{
    /// <inheritdoc />
    protected override string ModuleName => "react-designer";

    /// <summary>
    /// Creates a new React Flow graph and returns an API wrapper for it.
    /// </summary>
    public async ValueTask<ReactFlowGraphApi> CreateGraphAsync(string containerId, DotNetObjectReference<ReactFlowDesigner> componentRef, bool isReadOnly = false)
    {
        return await TryInvokeAsync(async module =>
        {
            await module.InvokeAsync<string>("createReactGraph", containerId, componentRef, isReadOnly, null);
            return new ReactFlowGraphApi(module, containerId);
        });
    }
}
