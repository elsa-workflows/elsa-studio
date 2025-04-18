using Blazored.FluentValidation;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Models;
using Elsa.Studio.Labels.UI.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Labels.UI.Components;

/// A dialog that creates a new secret.
public partial class CreateLabelDialog
{
    private readonly LabelInputModel _inputModel = new();
    private EditContext _editContext = null!;
    private FluentValidationValidator _fluentValidationValidator = null!;
    private LabelInputModelValidator _validator = null!;
    
    /// The default name of the agent to create.
    [Parameter] public string LabelName { get; set; } = "";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _inputModel.Name = LabelName;
        _inputModel.Description = "";
        _editContext = new(_inputModel);
        var api = await ApiClientProvider.GetApiAsync<ILabelsApi>();
        _validator = new(api);
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
        MudDialog.Close(_inputModel);
        return Task.CompletedTask;
    }
}