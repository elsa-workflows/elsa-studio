using Blazored.FluentValidation;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.List;

public partial class CreateWorkflowDialog
{
    private WorkflowPropertiesModel _propertiesModel = new();
    private EditContext _editContext = default!;
    private WorkflowPropertiesModelValidator _validator = default!;
    private FluentValidationValidator _fluentValidationValidator = default!;

    [Parameter] public string WorkflowName { get; set; } = "New workflow";
    [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private string WorkflowDescription { get; set; } = "";

    protected override void OnParametersSet()
    {
        _propertiesModel.Name = WorkflowName;
        _editContext = new EditContext(_propertiesModel);
        _validator = new WorkflowPropertiesModelValidator(WorkflowDefinitionService);
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        if(!await _fluentValidationValidator.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private Task OnValidSubmit()
    {
        MudDialog.Close(_propertiesModel);
        return Task.CompletedTask;
    }
}