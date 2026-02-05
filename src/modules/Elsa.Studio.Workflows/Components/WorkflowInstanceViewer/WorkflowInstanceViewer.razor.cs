using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.RealTime.Messages;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
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
    private WorkflowInstance _workflowInstance = null!;
    private WorkflowDefinition _workflowDefinition = null!;
    private WorkflowInstanceWorkspace _workspace = null!;
    private IWorkflowInstanceObserver? _workflowInstanceObserver;
    private int _leftPanelTabIndex;
    private string? _selectedActivityExecutionRecordId;
    private string? _selectedActivityNodeId;
    private bool _isExecutionDetailsDrawerOpen;
    private ActivityExecutionRecord? _selectedExecutionForDrawer;
    private bool _isSelectingFromCallStack;
    private bool _isSelectingFromJournal;

    /// The ID of the workflow instance to view.
    [Parameter] public string InstanceId { get; set; } = null!;

    /// An event that is invoked when a workflow definition is edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = null!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;

    private Journal Journal { get; set; } = null!;

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
        var workflowInstance = (await WorkflowInstanceService.GetAsync(_workflowInstance.Id))!;
        await InvokeAsync(async () =>
        {
            _workflowInstance = workflowInstance;
            StateHasChanged();

            if (_workflowInstance.Status == WorkflowStatus.Finished) 
                await Journal.SetWorkflowInstanceAsync(_workflowInstance);
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
        _isSelectingFromJournal = true;
        try
        {
            await _workspace.SelectWorkflowExecutionLogRecordAsync(entry);
        }
        finally
        {
            _isSelectingFromJournal = false;
        }
    }

    private async Task OnCallStackEntrySelected(ActivityExecutionRecord record)
    {
        _isSelectingFromCallStack = true;
        try
        {
            _selectedActivityNodeId = record.ActivityNodeId;
            _selectedExecutionForDrawer = record;
            _isExecutionDetailsDrawerOpen = true;
            StateHasChanged(); // Open drawer and highlight immediately.

            await _workspace.SelectActivityByIdAsync(record.ActivityId, record.ActivityNodeId);
        }
        finally
        {
            _isSelectingFromCallStack = false;
        }
    }

    private async Task OnActivitySelected(JsonObject arg)
    {
        if (_isSelectingFromCallStack || _isSelectingFromJournal)
            return;

        Journal.ClearSelection();
        _selectedActivityNodeId = arg.GetNodeId();

        // Get the last activity execution record ID for this activity
        var summaries = (await ActivityExecutionService.ListSummariesAsync(_workflowInstance.Id, _selectedActivityNodeId)).ToList();
        _selectedActivityExecutionRecordId = summaries.LastOrDefault()?.Id;

        if (_selectedActivityExecutionRecordId != null)
            _selectedExecutionForDrawer = await ActivityExecutionService.GetAsync(_selectedActivityExecutionRecordId);
        else
            _selectedExecutionForDrawer = null;

        StateHasChanged();
    }

    private async Task OnActivityExecutionSelected(string? executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            return;

        // Load the full execution record
        var record = await ActivityExecutionService.GetAsync(executionId);

        if (record != null)
        {
            _selectedActivityExecutionRecordId = record.Id;
            _selectedActivityNodeId = record.ActivityNodeId;
            _selectedExecutionForDrawer = record;
            _isExecutionDetailsDrawerOpen = true;
            StateHasChanged();
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeObserverAsync();
    }
}
