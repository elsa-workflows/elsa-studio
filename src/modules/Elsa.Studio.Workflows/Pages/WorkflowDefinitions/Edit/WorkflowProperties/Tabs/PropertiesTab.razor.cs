using Blazored.FluentValidation;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs;

using WorkflowDefinition = Api.Client.Resources.WorkflowDefinitions.Models.WorkflowDefinition;

public partial class PropertiesTab
{
    private readonly WorkflowPropertiesModel _propertiesModel = new();
    private FluentValidationValidator _fluentValidationValidator = default!;
    private WorkflowPropertiesModelValidator _validator = default!;
    private EditContext _editContext = default!;

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }

    protected override void OnInitialized()
    {
        _propertiesModel.DefinitionId = WorkflowDefinition.DefinitionId;
        _propertiesModel.Description = WorkflowDefinition.Description;
        _propertiesModel.Name = WorkflowDefinition.Name;
        _editContext = new EditContext(_propertiesModel);
    }

    private async Task ValidateForm()
    {
        if (!await _fluentValidationValidator.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private async Task OnValidSubmit()
    {
        WorkflowDefinition.Description = _propertiesModel.Description;
        WorkflowDefinition.Name = _propertiesModel.Name;

        if (OnWorkflowDefinitionUpdated != null)
            await OnWorkflowDefinitionUpdated();
    }
}