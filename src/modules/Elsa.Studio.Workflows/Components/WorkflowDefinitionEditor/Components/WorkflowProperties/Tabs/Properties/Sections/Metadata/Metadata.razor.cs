using Blazored.FluentValidation;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Metadata;

/// <summary>
/// A component that renders the workflow definition metadata.
/// </summary>
public partial class Metadata
{
    private readonly WorkflowMetadataModel _model = new();
    private FluentValidationValidator _fluentValidationValidator = default!;
    private WorkflowPropertiesModelValidator _validator = default!;
    private EditContext _editContext = default!;

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the callback that is invoked when the workflow definition is updated.
    /// </summary>
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }
    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    
    private bool IsReadOnly => Workspace?.IsReadOnly ?? false;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _model.DefinitionId = WorkflowDefinition.DefinitionId;
        _model.Description = WorkflowDefinition.Description;
        _model.Name = WorkflowDefinition.Name;
        _editContext = new EditContext(_model);
    }

    private async Task ValidateForm()
    {
        if (!await _fluentValidationValidator.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private async Task OnValidSubmit()
    {
        WorkflowDefinition.Description = _model.Description;
        WorkflowDefinition.Name = _model.Name;

        if (WorkflowDefinitionUpdated.HasDelegate)
            await WorkflowDefinitionUpdated.InvokeAsync();
    }
}