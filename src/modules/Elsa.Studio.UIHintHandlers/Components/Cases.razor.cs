using System.Text.Json;
using Elsa.Api.Client.Converters;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Helpers;
using Elsa.Studio.UIHintHandlers.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.UIHintHandlers.Components;

public partial class Cases
{
    private readonly string[] _uiSyntaxes = { "Literal", "Object" };
    
    private CaseRecord? _caseBeingEdited;
    private CaseRecord? _caseBeingAdded;
    private MudTable<CaseRecord> _table = default!;

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;
    [Parameter] public ICollection<CaseRecord> Items { get; set; } = new List<CaseRecord>();
    [Inject] private ISyntaxService SyntaxService { get; set; } = default!;
    
    private bool DisableAddButton => _caseBeingEdited != null || _caseBeingAdded != null;

    protected override void OnParametersSet()
    {
        Items = GetItems();
    }

    private ICollection<CaseRecord> GetItems()
    {
        var input = EditorContext.GetObjectValueOrDefault();
        var cases = ParseJson(input);
        var caseRecords = cases.Select(Map).ToList();
        return caseRecords;
    }

    private IEnumerable<Case> ParseJson(string? json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        options.Converters.Add(new ExpressionJsonConverterFactory());
        return JsonParser.ParseJson(json, () => new List<Case>(), options);
    }
    
    private IEnumerable<SyntaxDescriptor> GetSupportedSyntaxes()
    {
        var syntaxes = SyntaxService.ListSyntaxes().Except(_uiSyntaxes);
        
        foreach (var syntax in syntaxes)
            yield return new SyntaxDescriptor(syntax, syntax);
    }

    private CaseRecord Map(Case @case)
    {
        var syntaxProvider = SyntaxService.GetSyntaxProviderByExpressionType(@case.Condition.GetType());

        return new CaseRecord
        {
            Label = @case.Label,
            Condition = @case.Condition.ToString() ?? string.Empty,
            Syntax = syntaxProvider.SyntaxName
        };
    }
    
    private Case Map(CaseRecord @case)
    {
        var syntaxProvider = SyntaxService.GetSyntaxProviderByName(@case.Syntax);
        var expression = syntaxProvider.CreateExpression(@case.Condition);
        
        return new Case
        {
            Label = @case.Label,
            Condition = expression
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
        var @case = (CaseRecord)obj;
        _caseBeingEdited = new CaseRecord
        {
            Label = @case.Label,
            Condition = @case.Condition,
            Syntax = @case.Syntax
        };
        
        StateHasChanged();
    }

    private async void OnRowEditCancel(object obj)
    {
        if(_caseBeingAdded != null)
        {
            Items.Remove(_caseBeingAdded);
            await SaveChangesAsync();
            _caseBeingAdded = null;
            StateHasChanged();
            return;
        }
        
        var @case = (CaseRecord)obj;
        @case.Condition = _caseBeingEdited?.Condition ?? "";
        @case.Label = _caseBeingEdited?.Label ?? "";
        @case.Syntax = _caseBeingEdited?.Syntax ?? "";
        _caseBeingEdited = null;
        StateHasChanged();
    }

    private async Task OnDeleteClicked(CaseRecord @case)
    {
        Items.Remove(@case);
        await SaveChangesAsync();
    }

    private async Task OnAddClicked()
    {
        var @case = new CaseRecord
        {
            Label = $"Case {Items.Count + 1}",
            Condition = "",
            Syntax = "JavaScript"
        };

        Items.Add(@case);
        await SaveChangesAsync();
        _caseBeingAdded = @case;
        _table.SetEditingItem(@case);
        StateHasChanged();
    }
}

public class CaseRecord
{
    public string Label { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Syntax { get; set; } = "JavaScript";
}