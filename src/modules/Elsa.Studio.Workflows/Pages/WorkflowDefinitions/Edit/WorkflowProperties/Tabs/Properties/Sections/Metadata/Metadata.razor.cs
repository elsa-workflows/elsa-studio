using Blazored.FluentValidation;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.Properties.Sections.Metadata;

public partial class Metadata
{
    private readonly WorkflowMetadataModel _model = new();
    private FluentValidationValidator _fluentValidationValidator = default!;
    private WorkflowPropertiesModelValidator _validator = default!;
    private EditContext _editContext = default!;

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }
    [CascadingParameter] public IWorkspace? Workspace { get; set; }
    
    private bool IsReadOnly => Workspace?.IsReadOnly ?? false;

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

        if (OnWorkflowDefinitionUpdated != null)
            await OnWorkflowDefinitionUpdated();
    }
}