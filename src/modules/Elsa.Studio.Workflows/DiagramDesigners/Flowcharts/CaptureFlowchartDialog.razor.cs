using Blazored.FluentValidation;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts
{
    public partial class CaptureFlowchartDialog
    {
        private readonly CaptureOptions _captureModel = new();
        private EditContext _editContext = null!;
        private CaptureOptionsValidator _validator = null!;
        private FluentValidationValidator _fluentValidationValidator = null!;
        private bool loading = false;

        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
        [Parameter] public string FileName { get; set; } = null!;

        protected override void OnParametersSet()
        {
            _captureModel.FileName = FileName;
            _editContext = new(_captureModel);
            _validator = new(Localizer);
        }

        private Task OnCancelClicked()
        {
            MudDialog.Cancel();
            return Task.CompletedTask;
        }

        private async Task OnSubmitClicked()
        {
            if (!await _fluentValidationValidator.ValidateAsync())
                return;

            await OnValidSubmit();
        }

        private Task OnValidSubmit()
        {
            MudDialog.Close(_captureModel);
            return Task.CompletedTask;
        }
    }
}