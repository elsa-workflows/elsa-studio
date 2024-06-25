using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

public partial class WorkflowInstanceWorkspace : IWorkspace
{
    private WorkflowInstanceDetails _workflowInstanceDetails = default!;
    private WorkflowInstanceDesigner _workflowInstanceDesigner = default!;
    private MudDynamicTabs _dynamicTabs = default!;

    /// <summary>
    /// Gets or sets the workflow instance to view.
    /// </summary>
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }

    /// <summary>
    /// Gets or sets the workflow definition of the workflow instance to view.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the selected workflow execution log record.
    /// </summary>
    [Parameter] public JournalEntry? SelectedWorkflowExecutionLogRecord { get; set; }
    /// <summary>
    /// An event callback that is invoked when the current workflow graph path has changed.
    /// </summary>
    [Parameter] public EventCallback<DesignerPathChangedArgs> PathChanged { get; set; }
    
    /// <summary>
    /// An event callback that is invoked when an activity is selected.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// <summary>
    /// An event that is invoked when a workflow definition is edited.
    /// </summary>
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public bool HasWorkflowEditPermission => true;

    private async Task OnPathChanged(DesignerPathChangedArgs args)
    {
        await _workflowInstanceDetails.UpdateSubWorkflowAsync(args.CurrentActivity);
        _workflowInstanceDesigner.UpdateSubWorkflow(args.CurrentActivity);
        
        if(PathChanged.HasDelegate)
            await PathChanged.InvokeAsync(args);
    }
}