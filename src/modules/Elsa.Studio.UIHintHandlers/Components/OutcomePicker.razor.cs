using Elsa.Api.Client.Expressions;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Components;

public partial class OutcomePicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    private ICollection<string> Outcomes => EditorContext.WorkflowDefinition.Outcomes;

    protected override void OnParametersSet()
    {
        _items = Outcomes.Select(x => new SelectListItem(x, x)).OrderBy(x => x.Text).ToList();
    }

    private SelectListItem? GetSelectedValue()
    {
        var value = EditorContext.GetLiteralValueOrDefault();
        return _items.FirstOrDefault(x => x.Value == value);
    }
    
    private async Task OnValueChanged(SelectListItem? value)
    {
        var outcome = value?.Value;
        var expression = new LiteralExpression(outcome);
        await EditorContext.UpdateExpressionAsync(expression);
    }
}