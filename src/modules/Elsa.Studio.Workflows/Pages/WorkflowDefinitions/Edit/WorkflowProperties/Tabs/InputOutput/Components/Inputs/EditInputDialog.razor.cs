using Blazored.FluentValidation;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Models;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Validators;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Components.Inputs;

public partial class EditInputDialog
{
    private readonly InputDefinitionModel _model = new();
    private EditContext _editContext = default!;
    private InputModelValidator _validator = default!;
    private FluentValidationValidator _fluentValidationValidator = default!;
    private ICollection<StorageDriverDescriptor> _storageDriverDescriptors = new List<StorageDriverDescriptor>();
    private ICollection<VariableTypeDescriptor> _variableTypes = new List<VariableTypeDescriptor>();
    private ICollection<UIHintDescriptor> _uiHints = new List<UIHintDescriptor>();
    private ICollection<IGrouping<string, VariableTypeDescriptor>> _groupedVariableTypes = new List<IGrouping<string, VariableTypeDescriptor>>();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public InputDefinition? Input { get; set; }
    [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] private IVariableTypeService VariableTypeService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(_model);
        _storageDriverDescriptors = (await StorageDriverService.GetStorageDriversAsync()).ToList();
        _variableTypes = (await VariableTypeService.GetVariableTypesAsync()).ToList();
        _uiHints = (await GetUIHitsAsync()).ToList();
        _groupedVariableTypes = _variableTypes.GroupBy(x => x.Category).ToList();
    }

    protected override void OnParametersSet()
    {
        _editContext = new EditContext(_model);
        _validator = new InputModelValidator(WorkflowDefinition);
        
        if (Input == null)
        {
            _model.Name = GetNewInputName(WorkflowDefinition.Inputs);
            _model.DisplayName = _model.Name.Humanize();
            _model.StorageDriver = _storageDriverDescriptors.First();
            _model.Type = _variableTypes.First();
            _model.UIHint = _uiHints.First();
        }
        else
        {
            _model.Name = Input.Name;
            _model.Type = _variableTypes.FirstOrDefault(x => x.TypeName == Input.Type) ?? _variableTypes.First();
            _model.IsArray = Input.IsArray;
            _model.StorageDriver = _storageDriverDescriptors.FirstOrDefault(x => x.TypeName == Input.StorageDriverType) ?? _storageDriverDescriptors.First();
            _model.UIHint = _uiHints.FirstOrDefault(x => x.Name == Input.UIHint) ?? _uiHints.First();
            _model.Description = Input.Description;
            _model.Category = Input.Category;
            _model.DisplayName = Input.DisplayName;
        }
    }

    private string GetNewInputName(ICollection<InputDefinition> existingInputs)
    {
        var count = 0;

        while (true)
        {
            var inputName = $"Input{++count}";

            if (existingInputs.All(x => x.Name != inputName))
                return inputName;
        }
    }

    private Task<IEnumerable<UIHintDescriptor>> GetUIHitsAsync()
    {
        // TODO: Get these from the backend.
        var descriptors = new[]
        {
            new UIHintDescriptor("single-line", "Single line", "A single line of text input"),
            new UIHintDescriptor("multi-line", "Multi line", "Multiple lines of text input"),
            new UIHintDescriptor("checkbox", "Checkbox", "A checkbox"),
            new UIHintDescriptor("checklist", "Checklist", "A list of checkboxes"),
            new UIHintDescriptor("radio-list", "Radio list", "A list of radio buttons"),
            new UIHintDescriptor("dropdown", "Dropdown", "A dropdown list"),
            new UIHintDescriptor("multi-text", "Multi text", "An input for multiple words, like a tagging input"),
            new UIHintDescriptor("code-editor", "Code editor", "A code editor"),
            new UIHintDescriptor("variable-picker", "Variable picker", "A variable picker"),
            new UIHintDescriptor("workflow-definition-picker", "Workflow definition picker", "A workflow definition picker"),
            new UIHintDescriptor("output-picker", "Output picker", "A workflow output definition picker"),
            new UIHintDescriptor("outcome-picker", "Outcome picker", "An outcome picker"),
            new UIHintDescriptor("json-editor", "JSON editor", "A JSON editor"),
        };
        
        return Task.FromResult(descriptors.AsEnumerable());
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
        var input = Input ?? new InputDefinition();

        input.Name = _model.Name;
        input.Type = _model.Type.TypeName;
        input.IsArray = _model.IsArray;
        input.StorageDriverType = _model.StorageDriver.TypeName;
        input.Category = _model.Type.Category;
        input.UIHint = _model.UIHint!.Name;
        input.Description = _model.Description;
        input.DisplayName = _model.DisplayName;

        MudDialog.Close(input);
        return Task.CompletedTask;
    }
}