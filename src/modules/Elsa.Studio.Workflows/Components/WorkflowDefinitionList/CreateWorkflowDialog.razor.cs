using Blazored.FluentValidation;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// A dialog that allows the user to create a new workflow.
/// </summary>
public partial class CreateWorkflowDialog
{
    private readonly WorkflowMetadataModel _metadataModel = new();
    private EditContext _editContext = default!;
    private WorkflowPropertiesModelValidator _validator = default!;
    private FluentValidationValidator _fluentValidationValidator = default!;

    /// <summary>
    /// The name of the workflow to create.
    /// </summary>
    [Parameter] public string WorkflowName { get; set; } = "New workflow";
    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private string WorkflowDescription { get; set; } = "";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _metadataModel.Name = WorkflowName;
        _editContext = new EditContext(_metadataModel);
        _validator = new WorkflowPropertiesModelValidator(WorkflowDefinitionService, BlazorServiceAccessor, Services);
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
        MudDialog.Close(_metadataModel);
        return Task.CompletedTask;
    }
}