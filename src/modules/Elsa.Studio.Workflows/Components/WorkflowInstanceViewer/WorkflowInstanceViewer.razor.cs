using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer;

/// The index page for viewing a workflow instance.
public partial class WorkflowInstanceViewer : IAsyncDisposable
{
    private WorkflowInstance _workflowInstance = default!;
    private WorkflowDefinition _workflowDefinition = default!;
    private WorkflowInstanceWorkspace _workspace = default!;
    private IWorkflowInstanceObserver? _workflowInstanceObserver = default!;

    /// The ID of the workflow instance to view.
    [Parameter] public string InstanceId { get; set; } = default!;

    /// An event that is invoked when a workflow definition is edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = default!;

    private Journal Journal { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var instance = await WorkflowInstanceService.GetAsync(InstanceId) ?? throw new InvalidOperationException($"Workflow instance with ID {InstanceId} not found.");
        var workflowDefinition = await WorkflowDefinitionService.FindByIdAsync(instance.DefinitionVersionId);
        _workflowInstance = instance;
        _workflowDefinition = workflowDefinition!;
        await SelectWorkflowInstanceAsync(instance);

        if (_workflowInstance.Status == WorkflowStatus.Running)
            await ObserveWorkflowInstanceAsync();
    }

    private async Task ObserveWorkflowInstanceAsync()
    {
        await DisposeObserverAsync();
        _workflowInstanceObserver = await WorkflowInstanceObserverFactory.CreateAsync(_workflowInstance.Id);
        _workflowInstanceObserver.WorkflowInstanceUpdated += OnWorkflowInstanceUpdatedAsync;
    }

    private async Task DisposeObserverAsync()
    {
        if (_workflowInstanceObserver != null)
        {
            _workflowInstanceObserver.WorkflowInstanceUpdated -= OnWorkflowInstanceUpdatedAsync;
            await _workflowInstanceObserver.DisposeAsync();
            _workflowInstanceObserver = null;
        }
    }

    private async Task SelectWorkflowInstanceAsync(WorkflowInstance instance)
    {
        // Select activity node IDs that are direct children of the root.
        var activityNodeIds = _workflowDefinition.Root.GetActivities().Select(x => x.GetNodeId()).ToList();
        var filter = new JournalFilter
        {
            ActivityNodeIds = activityNodeIds
        };
        await Journal.SetWorkflowInstanceAsync(instance, filter);
    }
    
    private async Task OnWorkflowInstanceUpdatedAsync(WorkflowInstanceUpdatedMessage message)
    {
        var workflowInstance = (await InvokeWithBlazorServiceContext(() => WorkflowInstanceService.GetAsync(_workflowInstance.Id)))!;
        await InvokeAsync(async () =>
        {
            _workflowInstance = workflowInstance;
            StateHasChanged();

            if (_workflowInstance.Status == WorkflowStatus.Finished)
            {
                await DisposeObserverAsync();
                await Journal.SetWorkflowInstanceAsync(_workflowInstance);
            }
        });
    }

    private async Task OnDesignerPathChanged(DesignerPathChangedArgs args)
    {
        var activityNodeIds = args.ContainerActivity.GetActivities().Select(x => x.GetNodeId()).ToList();
        var filter = new JournalFilter
        {
            ActivityNodeIds = activityNodeIds
        };
        await Journal.SetWorkflowInstanceAsync(_workflowInstance, filter);
    }

    private async Task OnWorkflowExecutionLogRecordSelected(JournalEntry entry)
    {
        await _workspace.SelectWorkflowExecutionLogRecordAsync(entry);
    }

    private Task OnActivitySelected(JsonObject arg)
    {
        Journal.ClearSelection();
        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeObserverAsync();
    }
}