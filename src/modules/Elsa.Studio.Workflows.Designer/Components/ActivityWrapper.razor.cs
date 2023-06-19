using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Designer.Interop;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Designer.Components;

public partial class ActivityWrapper
{
    private string _label = default!;

    [Parameter] public string? ElementId { get; set; }
    [Parameter] public string ActivityId { get; set; } = default!;
    [Parameter] public Activity Activity { get; set; } = default!;

    [Inject] DesignerJsInterop DesignerInterop { get; set; } = default!;
    [Inject] IActivityRegistry ActivityRegistry { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var activityType = Activity.Type;
        var descriptor = await ActivityRegistry.FindAsync(activityType);

        _label = descriptor?.DisplayName ?? descriptor?.Name ?? "Unknown Activity";

        // If the activity has a size, don't update it.
        var size = Activity.Metadata.TryGetValue<ActivityDesignerMetadata>("metadata")?.Size;

        if (size != null)
        {
            if (size.Width > 0 || size.Height > 0)
                return;
        }

        // Otherwise, update the activity node.
        if (!string.IsNullOrEmpty(ElementId))
            await DesignerInterop.UpdateActivityNodeAsync(ElementId, Activity);
    }
}