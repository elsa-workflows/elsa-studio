using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Helpers;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Renders an editor for a collection of <see cref="SwitchCase"/> objects.
/// </summary>
public partial class Cases
{
    private readonly string[] _uiSyntaxes = { "Literal", "Object" };

    private SwitchCaseRecord? _caseBeingEdited;
    private SwitchCaseRecord? _caseBeingAdded;
    private MudTable<SwitchCaseRecord> _table = default!;

    /// <summary>
    /// The context for the editor.
    /// </summary>
    [Parameter]
    public DisplayInputEditorContext EditorContext { get; set; } = default!;

    [CascadingParameter] private ExpressionDescriptorProvider ExpressionDescriptorProvider { get; set; } = default!;

    private ICollection<SwitchCaseRecord> Items { get; set; } = new List<SwitchCaseRecord>();
    private bool DisableAddButton => _caseBeingEdited != null || _caseBeingAdded != null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Items = GetItems();
    }

    private ICollection<SwitchCaseRecord> GetItems()
    {
        var input = EditorContext.GetObjectValueOrDefault();
        var cases = ParseJson(input);
        var caseRecords = cases.Select(Map).ToList();
        return caseRecords;
    }

    private IEnumerable<SwitchCase> ParseJson(string? json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonParser.ParseJson(json, () => new List<SwitchCase>(), options);
    }

    private IEnumerable<ExpressionDescriptor> GetSupportedExpressions()
    {
        return ExpressionDescriptorProvider.ListDescriptors().Where(x => !_uiSyntaxes.Contains(x.Type)).ToList();
    }

    private SwitchCaseRecord Map(SwitchCase @case)
    {
        var syntaxProvider = ExpressionDescriptorProvider.GetByType(@case.Condition.Type);

        return new SwitchCaseRecord
        {
            Label = @case.Label,
            Condition = @case.Condition.ToString(),
            ExpressionType = syntaxProvider?.Type ?? "JavaScript",
            Activity = @case.Activity
        };
    }

    private SwitchCase Map(SwitchCaseRecord switchCase)
    {
        var expression = new Expression(switchCase.ExpressionType);

        return new SwitchCase
        {
            Label = switchCase.Label,
            Condition = expression,
            Activity = switchCase.Activity
        };
    }

    private async Task SaveChangesAsync()
    {
        var cases = Items.Select(Map).ToList();

        await EditorContext.UpdateValueAsync(cases);
    }

    private async void OnRowEditCommitted(object data)
    {
        _caseBeingAdded = null;
        _caseBeingEdited = null;
        await SaveChangesAsync();
        StateHasChanged();
    }

    private void OnRowEditPreview(object obj)
    {
        var @case = (SwitchCaseRecord)obj;
        _caseBeingEdited = new SwitchCaseRecord
        {
            Label = @case.Label,
            Condition = @case.Condition,
            ExpressionType = @case.ExpressionType
        };

        StateHasChanged();
    }

    private async void OnRowEditCancel(object obj)
    {
        if (_caseBeingAdded != null)
        {
            Items.Remove(_caseBeingAdded);
            await SaveChangesAsync();
            _caseBeingAdded = null;
            StateHasChanged();
            return;
        }

        var @case = (SwitchCaseRecord)obj;
        @case.Condition = _caseBeingEdited?.Condition ?? "";
        @case.Label = _caseBeingEdited?.Label ?? "";
        @case.ExpressionType = _caseBeingEdited?.ExpressionType ?? "";
        _caseBeingEdited = null;
        StateHasChanged();
    }

    private async Task OnDeleteClicked(SwitchCaseRecord switchCase)
    {
        Items.Remove(switchCase);
        await SaveChangesAsync();
    }

    private void OnAddClicked()
    {
        var @case = new SwitchCaseRecord
        {
            Label = $"Case {Items.Count + 1}",
            Condition = "",
            ExpressionType = "JavaScript"
        };

        Items.Add(@case);
        _caseBeingAdded = @case;

        // Need to do it this way, otherwise MudTable doesn't show the item in edit mode.
        _ = Task.Delay(1).ContinueWith(_ =>
        {
            InvokeAsync(() =>
            {
                _table.SetEditingItem(@case);
                StateHasChanged();
            });
        });
    }
}

public class SwitchCaseRecord
{
    public string Label { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;

    [Obsolete("Use ExpressionType instead.")]
    public string Syntax
    {
        get => ExpressionType;
        set => ExpressionType = value;
    }

    public string ExpressionType { get; set; } = "JavaScript";

    /// <summary>
    /// When used in a <see cref="Switch"/> activity, specifies the activity to schedule when the condition evaluates to true.
    /// </summary>
    public JsonObject? Activity { get; set; }
}