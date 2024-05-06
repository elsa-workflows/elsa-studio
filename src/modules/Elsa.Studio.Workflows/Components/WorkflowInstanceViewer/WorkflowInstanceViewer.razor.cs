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

/// <summary>
/// The index page for viewing a workflow instance.
/// </summary>
public partial class WorkflowInstanceViewer
{
    private IList<WorkflowInstance> _workflowInstances = new List<WorkflowInstance>();
    private IList<WorkflowDefinition> _workflowDefinitions = new List<WorkflowDefinition>();
    private WorkflowInstanceWorkspace _workspace = default!;

    /// <summary>
    /// The ID of the workflow instance to view.
    /// </summary>
    [Parameter] public string InstanceId { get; set; } = default!;

    /// <summary>
    /// An event that is invoked when a workflow definition is edited.
    /// </summary>
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private Journal Journal { get; set; } = default!;
    private JournalEntry? SelectedJournalEntry { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var instance = await WorkflowInstanceService.GetAsync(InstanceId) ?? throw new InvalidOperationException($"Workflow instance with ID {InstanceId} not found.");
        var definitionVersionIds = new[] { instance.DefinitionVersionId };
        var response = await WorkflowDefinitionService.FindManyByIdAsync(definitionVersionIds, true);
        _workflowInstances = new List<WorkflowInstance> { instance };
        _workflowDefinitions = response.ToList();
        await SelectWorkflowInstanceAsync(instance);
    }

    private async Task SelectWorkflowInstanceAsync(WorkflowInstance instance)
    {
        // Select activity IDs that are direct children of the root.
        var definition = _workflowDefinitions.First(x => x.Id == instance.DefinitionVersionId);
        var activityIds = definition.Root.GetActivities().Select(x => x.GetId()).ToList();
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
        var instance = _workflowInstances.First();
        await Journal.SetWorkflowInstanceAsync(instance, filter);
    }

    private Task OnWorkflowExecutionLogRecordSelected(JournalEntry entry)
    {
        SelectedJournalEntry = entry;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnActivitySelected(JsonObject arg)
    {
        Journal.ClearSelection();
        return Task.CompletedTask;
    }
}