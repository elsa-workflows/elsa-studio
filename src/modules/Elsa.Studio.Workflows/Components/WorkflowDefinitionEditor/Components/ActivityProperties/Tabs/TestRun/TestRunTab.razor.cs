using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Tests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.TestRun;

/// <summary>
/// The info tab for an activity.
/// </summary>
public partial class TestRunTab
{
    /// <summary>
    /// The workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    
    /// <summary>
    /// The activity descriptor.
    /// </summary>
    [Parameter] public ActivityDescriptor ActivityDescriptor { get; set; } = null!;
    
    /// <summary>
    /// The activity.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = null!;
    
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = null!;
    
    private DataPanelModel TestRunResults { get; } = new();
    private bool IsRunning { get; set; }

    private async Task OnTestRunClick()
    {
        var testsApi = await BackendApiClientProvider.GetApiAsync<ITestsApi>();
        var request = new TestActivityRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(WorkflowDefinition.Id),
            ActivityHandle = ActivityHandle.FromActivityNodeId(Activity.GetNodeId())
        };
        IsRunning = true;
        StateHasChanged();
        var response = await testsApi.TestActivityAsync(request);
        TestRunResults.Clear();
        TestRunResults.Add("Status", response.Status.ToString());
        if (response.Outcomes != null) TestRunResults.Add("Outcomes", string.Join(", ", response.Outcomes));
        if(response.Outputs != null) TestRunResults.Add("Outputs", response.Outputs.SerializeToNode().ToString());
        if(response.Exception != null) TestRunResults.Add("Exception", response.Exception.SerializeToNode().ToString());
        IsRunning = false;
        StateHasChanged();
    }
}