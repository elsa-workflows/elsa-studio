using Blazored.FluentValidation;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Validators;
using Elsa.Studio.Workflows.Domain.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Components.Inputs;

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
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] private IVariableTypeService VariableTypeService { get; set; } = default!;
    [Inject] private ILocalizer _localizer { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _editContext = new(_model);
        _storageDriverDescriptors = (await StorageDriverService.GetStorageDriversAsync()).ToList();
        _variableTypes = (await VariableTypeService.GetVariableTypesAsync()).ToList();
        _uiHints = (await GetUIHintsAsync()).ToList();
        _groupedVariableTypes = _variableTypes.GroupBy(x => x.Category).ToList();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _editContext = new(_model);
        _validator = new(WorkflowDefinition);
        
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

    private Task<IEnumerable<UIHintDescriptor>> GetUIHintsAsync()
    {
        // TODO: Get these from the backend.
        var descriptors = new[]
        {
            new UIHintDescriptor("singleline", _localizer["Single line"], _localizer["A single line of text input"]),
            new UIHintDescriptor("multiline", _localizer["Multi line"], _localizer["Multiple lines of text input"]),
            new UIHintDescriptor("checkbox", _localizer["Checkbox"], _localizer["A checkbox"]),
            new UIHintDescriptor("checklist", _localizer["Checklist"], _localizer["A list of checkboxes"]),
            new UIHintDescriptor("radiolist", _localizer["Radio list"], _localizer["A list of radio buttons"]),
            new UIHintDescriptor("dropdown", _localizer["Dropdown"], _localizer["A dropdown list"]),
            new UIHintDescriptor("multitext", _localizer["Multi text"], _localizer["An input for multiple words, like a tagging input"]),
            new UIHintDescriptor("code-editor", _localizer["Code editor"], _localizer["A code editor"]),
            new UIHintDescriptor("variable-picker", _localizer["Variable picker"], _localizer["A variable picker"]),
            new UIHintDescriptor("workflow-definition-picker", _localizer["Workflow definition picker"], _localizer["A workflow definition picker"]),
            new UIHintDescriptor("output-picker", _localizer["Output picker"], _localizer["A workflow output definition picker"]),
            new UIHintDescriptor("outcome-picker", _localizer["Outcome picker"], _localizer["An outcome picker"]),
            new UIHintDescriptor("json-editor", _localizer["JSON editor"], _localizer["A JSON editor"]),
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