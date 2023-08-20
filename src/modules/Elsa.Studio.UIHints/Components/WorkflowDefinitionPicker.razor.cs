using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Provides a component for picking a workflow definition.
/// </summary>
public partial class WorkflowDefinitionPicker
{
    private ICollection<SelectListItem> _items = Array.Empty<SelectListItem>();

    /// <summary>
    /// Gets or sets the editor context.
    /// </summary>
    [Parameter]
    public DisplayInputEditorContext EditorContext { get; set; } = default!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;


    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var request = new ListWorkflowDefinitionsRequest();
            var availableWorkflowDefinitions = await WorkflowDefinitionService.ListAsync(request, VersionOptions.Published);
            var workflowDefinitions = availableWorkflowDefinitions.Items;
            _items = workflowDefinitions.Select(x => new SelectListItem(x.Name, x.DefinitionId)).OrderBy(x => x.Text).ToList();

            StateHasChanged();
        }
    }

    private SelectListItem? GetSelectedValue()
    {
        var value = EditorContext.GetLiteralValueOrDefault();
        return _items.FirstOrDefault(x => x.Value == value);
    }

    private async Task OnValueChanged(SelectListItem? value)
    {
        var expression = new LiteralExpression(value?.Value);
        await EditorContext.UpdateExpressionAsync(expression);
    }
}