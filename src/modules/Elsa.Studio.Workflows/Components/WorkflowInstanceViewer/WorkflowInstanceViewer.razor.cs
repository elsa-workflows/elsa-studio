using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer;

/// The index page for viewing a workflow instance.
public partial class WorkflowInstanceViewer
{
    private WorkflowInstance _workflowInstance = default!;
    private WorkflowDefinition _workflowDefinition = default!;
    private WorkflowInstanceWorkspace _workspace = default!;

    /// The ID of the workflow instance to view.
    [Parameter] public string InstanceId { get; set; } = default!;

    /// An event that is invoked when a workflow definition is edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private Journal Journal { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var instance = await WorkflowInstanceService.GetAsync(InstanceId) ?? throw new InvalidOperationException($"Workflow instance with ID {InstanceId} not found.");
        var workflowDefinition = await WorkflowDefinitionService.FindByIdAsync(instance.DefinitionVersionId);
        _workflowInstance = instance;
        _workflowDefinition = workflowDefinition!;
        await SelectWorkflowInstanceAsync(instance);
    }

    private async Task SelectWorkflowInstanceAsync(WorkflowInstance instance)
    {
        // Select activity IDs that are direct children of the root.
        var activityIds = _workflowDefinition.Root.GetActivities().Select(x => x.GetId()).ToList();
        var filter = new JournalFilter
        {
            ActivityIds = activityIds
        };
        await Journal.SetWorkflowInstanceAsync(instance, filter);
    }

    private async Task OnSelectedWorkflowInstanceChanged(WorkflowInstance value)
    {
        await SelectWorkflowInstanceAsync(value);
    }

    private async Task OnDesignerPathChanged(DesignerPathChangedArgs args)
    {
        var activityIds = args.ContainerActivity.GetActivities().Select(x => x.GetId()).ToList();
        var filter = new JournalFilter
        {
            ActivityIds = activityIds
        };
        await Journal.SetWorkflowInstanceAsync(_workflowInstance, filter);
    }

    private async Task OnWorkflowExecutionLogRecordSelected(JournalEntry entry)
    {
        await _workspace.SelectWorkflowExecutionLogRecordAsync(entry);
        StateHasChanged();
    }

    private Task OnActivitySelected(JsonObject arg)
    {
        Journal.ClearSelection();
        return Task.CompletedTask;
    }
}