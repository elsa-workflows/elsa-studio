using Elsa.Api.Client.Expressions;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Extensions;
using Elsa.Studio.UIHintHandlers.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Components;

public partial class Dropdown
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;
    
    protected override async Task OnInitializedAsync()
    {
        var selectList = await EditorContext.InputDescriptor.GetSelectListAsync();
        _items = selectList.Items;
    }

    private SelectListItem? GetSelectedValue()
    {
        var inputDescriptor = EditorContext.InputDescriptor;
        var value = (EditorContext.Value?.Expression as LiteralExpression)?.Value?.ToString() ?? string.Empty;
        var defaultValue = inputDescriptor.DefaultValue?.ToString();
        var selectedValue = string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        return _items.FirstOrDefault(x => x.Value == selectedValue);
    }
    
    private async Task OnValueChanged(SelectListItem? value)
    {
        var expression = new LiteralExpression(value?.Value);
        await EditorContext.UpdateExpressionAsync(expression);
    }
}