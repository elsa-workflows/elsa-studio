using System.ComponentModel.DataAnnotations;
using Blazored.FluentValidation;
using Elsa.Studio.Workflows.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.List;

public partial class CreateWorkflowDialog
{
    private NewWorkflowModel _model = new();
    private EditContext _editContext = default!;
    private NewWorkflowModelValidator _validator = default!;
    private FluentValidationValidator _fluentValidationValidator = default!;

    [Parameter] public string WorkflowName { get; set; } = "New workflow";
    [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private string WorkflowDescription { get; set; } = "";

    protected override void OnParametersSet()
    {
        _model.Name = WorkflowName;
        _editContext = new EditContext(_model);
        _validator = new NewWorkflowModelValidator(WorkflowDefinitionService);
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
        MudDialog.Close(_model);
        return Task.CompletedTask;
    }
}

public class NewWorkflowModel
{
    [Required] public string? Name { get; set; }
    public string? Description { get; set; } = default!;
}

public class NewWorkflowModelValidator : AbstractValidator<NewWorkflowModel>
{
    public NewWorkflowModelValidator(IWorkflowDefinitionService workflowDefinitionService)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the workflow.");
        
        RuleFor(x => x.Name)
            .MustAsync(async (name, cancellationToken) => await workflowDefinitionService.GetIsNameUniqueAsync(name!, cancellationToken))
            .WithMessage("A workflow with this name already exists.");
    }
}