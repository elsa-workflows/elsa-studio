using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.DiagramDesigners.Fallback;

public class FallbackDiagramDesigner : IDiagramDesigner
{
    private JsonObject _activity = default!;

    public bool IsInitialized => true;

    public Task LoadRootActivityAsync(JsonObject activity)
    {
        _activity = activity;
        return Task.CompletedTask;
    }

    public Task UpdateActivityAsync(string id, JsonObject activity)
    {
        return Task.CompletedTask;
    }

    public Task<JsonObject> ReadRootActivityAsync()
    {
        return Task.FromResult(_activity);
    }

    public RenderFragment DisplayDesigner(DisplayContext context)
    {
        _activity = context.Activity;

        return builder =>
        {
            builder.OpenComponent<FallbackDesigner>(0);
            builder.AddAttribute(1, nameof(FallbackDesigner.Activity), _activity);
            builder.CloseComponent();
        };
    }
}