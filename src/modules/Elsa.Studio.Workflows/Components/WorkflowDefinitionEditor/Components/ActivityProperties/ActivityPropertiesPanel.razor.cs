using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;

/// <summary>
/// Renders the properties of an activity.
/// </summary>
public partial class ActivityPropertiesPanel
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the activity.
    /// </summary>
    [Parameter]
    public JsonObject? Activity { get; set; }

    /// <summary>
    /// Gets or sets the activity descriptor.
    /// </summary>
    [Parameter]
    public ActivityDescriptor? ActivityDescriptor { get; set; }

    /// <summary>
    /// Gets or sets a callback that is invoked when the activity is updated.
    /// </summary>
    [Parameter]
    public Func<JsonObject, Task>? OnActivityUpdated { get; set; }

    /// <summary>
    /// Gets or sets the visible pane height.
    /// </summary>
    [Parameter]
    public int VisiblePaneHeight { get; set; }

    [Inject] private IExpressionService ExpressionService { get; set; } = null!;

    [Inject] private IEnumerable<IActivityTab> PluginTabs { get; set; } = new List<IActivityTab>();
    [Inject] private IRemoteFeatureProvider RemoteFeatureProvider { get; set; } = null!;

    private ExpressionDescriptorProvider ExpressionDescriptorProvider { get; } = new();
    private bool IsResilienceEnabled { get; set; }
    private bool IsResilientActivity => ActivityDescriptor?.CustomProperties.TryGetValue("Resilient", out var resilientObj) == true && resilientObj.ConvertTo<bool>();  
    private bool DisplayResilienceTab => IsResilienceEnabled && IsResilientActivity;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var descriptors = await ExpressionService.ListDescriptorsAsync();
        IsResilienceEnabled = await RemoteFeatureProvider.IsEnabledAsync("Elsa.Resilience");
        ExpressionDescriptorProvider.AddRange(descriptors);
    }

    private bool IsWorkflowAsActivity => ActivityDescriptor != null && ActivityDescriptor.CustomProperties.TryGetValue("RootType", out var value) && value.ConvertTo<string>() == "WorkflowDefinitionActivity";
    private bool IsTaskActivity => ActivityDescriptor?.Kind == ActivityKind.Task;
}