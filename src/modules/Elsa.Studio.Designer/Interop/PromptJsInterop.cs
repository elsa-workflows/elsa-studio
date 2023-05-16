using Microsoft.JSInterop;

namespace Elsa.Studio.Designer.Interop;

// This class provides an example of how JavaScript functionality can be wrapped
// in a .NET class for easy consumption. The associated JavaScript module is
// loaded on demand when first needed.
//
// This class can be registered as scoped DI service and then injected into Blazor
// components for use.

public class PromptJsInterop : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    public PromptJsInterop(IJSRuntime jsRuntime)
    {
        moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/Elsa.Studio.Designer/prompt.entry.js").AsTask());
    }

    public async ValueTask<string> Prompt(string message)
    {
        var module = await moduleTask.Value;
        return await module.InvokeAsync<string>("showPrompt", message);
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}