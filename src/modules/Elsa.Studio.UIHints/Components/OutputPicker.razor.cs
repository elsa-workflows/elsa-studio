using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Components;

public partial class OutputPicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    private ICollection<OutputDefinition> Outputs => EditorContext.WorkflowDefinition.Outputs;

    protected override void OnParametersSet()
    {
        var items = Outputs.Select(x => new SelectListItem(x.DisplayName, x.Name)).OrderBy(x => x.Text).ToList();
        items.Insert(0, new SelectListItem("(None)", ""));
        _items = items;
    }

    private SelectListItem? GetSelectedValue()
    {
        var outputName = EditorContext.GetExpressionValueOrDefault();
        return _items.FirstOrDefault(x => x.Value == outputName);
    }
    
    private async Task OnValueChanged(SelectListItem? value)
    {
        var outputName = value?.Value;
        await EditorContext.UpdateExpressionAsync(new LiteralExpression(outputName));
    }
}