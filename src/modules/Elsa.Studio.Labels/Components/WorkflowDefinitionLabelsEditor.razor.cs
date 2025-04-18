using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;
using Elsa.Labels.Entities;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Contracts;
using Elsa.Studio.Labels.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Labels.Components;

/// <summary>
/// A component that renders the workflow context editor.
/// </summary>
public partial class WorkflowDefinitionLabelsEditor
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition WorkflowDefinition { get; set; } = default!;

    /// <summary>
    /// Gets or sets the callback that is invoked when the workflow definition is updated.
    /// </summary>
    [Parameter]
    public EventCallback WorkflowDefinitionUpdated { get; set; }

    [Inject] private IWorkflowDefinitionLabelsProvider workflowDefinitionLabelsProvider { get; set; } = default!;

    private ICollection<WorkflowDefinitionLabelDescriptor> Labels { get; set; } = new List<WorkflowDefinitionLabelDescriptor>();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Labels = (await workflowDefinitionLabelsProvider.ListAsync(WorkflowDefinition.Id)).ToList();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
    }

    private Task AddLabelAsync()
    {
        throw new NotImplementedException();
    }
    private Task OnCloseAsync(MudBlazor.MudChip<string> chip)
    {
        throw new NotImplementedException();
    }
}