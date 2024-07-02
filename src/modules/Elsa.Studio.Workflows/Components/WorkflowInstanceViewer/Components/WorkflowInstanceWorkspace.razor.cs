using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
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
    
    /// Gets or sets the workflow instance to view.
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }
    
    /// Gets or sets the workflow definition of the workflow instance to view.
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    
    /// An event callback that is invoked when the current workflow graph path has changed.
    [Parameter] public EventCallback<DesignerPathChangedArgs> PathChanged { get; set; }
    
    /// An event callback that is invoked when an activity is selected.
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }
    
    /// An event that is invoked when a workflow definition is edited.
    [Parameter] public EventCallback<string> EditWorkflowDefinition { get; set; }

    /// <inheritdoc />
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public bool HasWorkflowEditPermission => true;
    
    /// Selects the associated activity in the designer and activates its Event tab.
    public async Task SelectWorkflowExecutionLogRecordAsync(JournalEntry entry)
    {
        await _workflowInstanceDesigner.SelectWorkflowExecutionLogRecordAsync(entry);
    }

    private async Task OnPathChanged(DesignerPathChangedArgs args)
    {
        await _workflowInstanceDetails.UpdateSubWorkflowAsync(args.ContainerActivity);
        _workflowInstanceDesigner.UpdateSubWorkflow(args.ContainerActivity);
        
        if(PathChanged.HasDelegate)
            await PathChanged.InvokeAsync(args);
    }
}