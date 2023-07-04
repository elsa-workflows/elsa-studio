using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Components;

public partial class VariablePicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    private ICollection<Variable> Variables => EditorContext.WorkflowDefinition.Variables;

    protected override void OnParametersSet()
    {
        _items = Variables.Select(x => new SelectListItem(x.Name, x.Id)).OrderBy(x => x.Text).ToList();
    }

    private SelectListItem? GetSelectedValue()
    {
        var value = EditorContext.GetValueOrDefault<Variable>();
        return _items.FirstOrDefault(x => x.Value == value?.Id);
    }
    
    private async Task OnValueChanged(SelectListItem? value)
    {
        var variableId = value?.Value;
        var variable = Variables.FirstOrDefault(x => x.Id == variableId);
        await EditorContext.UpdateValueAsync(variable);
    }
}