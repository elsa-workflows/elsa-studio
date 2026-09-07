using Blazilla;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// A dialog that allows the user to clone a workflow.
/// </summary>
public partial class CloneWorkflowDialog
{
    private readonly WorkflowMetadataModel _metadataModel = new();
    private EditContext _editContext = null!;
    private WorkflowPropertiesModelValidator _validator = null!;
   
    /// <summary>
    /// The name of the workflow to create.
    /// </summary>
    [Parameter] public string WorkflowName { get; set; } = null!;
    [Parameter] public string? WorkflowDescription { get; set; }
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;    

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _metadataModel.Name = WorkflowName;
        _metadataModel.Description = WorkflowDescription;
        _editContext = new(_metadataModel);
        _validator = new(WorkflowDefinitionService, Localizer);
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private Task OnSubmitClicked() => ValidateAndSubmitAsync();

    // Blazilla runs the async uniqueness rule outside the synchronous validation pass, so the form is
    // routed through OnSubmit: OnValidSubmit would fire before that rule completed and could clone a
    // workflow under a name that turns out to be taken.
    private Task OnSubmit(EditContext _) => ValidateAndSubmitAsync();

    private async Task ValidateAndSubmitAsync()
    {
        // The bound name can change while the asynchronous uniqueness check is pending. Capture it up
        // front and bail out if it no longer matches once validation completes, so a stale value is
        // never submitted; the user's next submission will re-validate the current value.
        var submittedName = _metadataModel.Name;

        if (!await _editContext.ValidateAsync())
            return;

        if (!string.Equals(_metadataModel.Name, submittedName, StringComparison.Ordinal))
            return;

        MudDialog.Close(_metadataModel);
    }
}
