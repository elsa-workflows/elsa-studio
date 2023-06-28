using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.DiagramDesigners.Fallback;

public class FallbackDiagramDesigner : IDiagramDesigner
{
    private Activity _activity = default!;
    
    public Task UpdateActivityAsync(Activity activity)
    {
        return Task.CompletedTask;
    }

    public Task<Activity> ReadRootActivityAsync()
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