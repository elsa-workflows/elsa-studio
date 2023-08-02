using BlazorMonaco.Editor;
using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ThrottleDebounce;

namespace Elsa.Studio.Components;

/// <summary>
/// A component that renders an input for an expression.
/// </summary>
public partial class ExpressionInput : IDisposable
{
    private const string DefaultSyntax = "Literal";
    private readonly string[] _uiSyntaxes = { "Literal", "Object" };
    private string _selectedSyntax = DefaultSyntax;
    private string _monacoLanguage = "";
    private StandaloneCodeEditor? _monacoEditor = default!;
    private bool _isInternalContentChange;
    private string _monacoEditorId = $"monaco-editor-{Guid.NewGuid()}:N";
    private string? _lastMonacoEditorContent;
    private RateLimitedFunc<WrappedInput, Task> _throttledValueChanged;

    /// <inheritdoc />
    public ExpressionInput()
    {
        _throttledValueChanged = Debouncer.Debounce<WrappedInput, Task>(InvokeValueChangedCallback, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// The context for the editor.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;
    
    /// <summary>
    /// The content to render inside the editor.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;
    [Inject] private ISyntaxService SyntaxService { get; set; } = default!;
    
    private string UISyntax => EditorContext.UIHintHandler.UISyntax;
    private bool IsUISyntax => _selectedSyntax == UISyntax;
    private string? ButtonIcon => IsUISyntax ? Icons.Material.Filled.MoreVert : default;
    private string? ButtonLabel => IsUISyntax ? default : _selectedSyntax;
    private Variant ButtonVariant => IsUISyntax ? default : Variant.Filled;
    private Color ButtonColor => IsUISyntax ? default : Color.Primary;
    private string? ButtonEndIcon => IsUISyntax ? default : Icons.Material.Filled.KeyboardArrowDown;
    private Color ButtonEndColor => IsUISyntax ? default : Color.Secondary;
    private bool ShowMonacoEditor => !IsUISyntax && EditorContext.InputDescriptor.IsWrapped;
    private string DisplayName => EditorContext.InputDescriptor.DisplayName ?? EditorContext.InputDescriptor.Name;
    private string? Description => EditorContext.InputDescriptor.Description;
    private string InputValue => EditorContext.GetExpressionValueOrDefault();
    
    private IEnumerable<SyntaxDescriptor> GetSupportedSyntaxes()
    {
        yield return new SyntaxDescriptor(UISyntax, "Default");
        var syntaxes = SyntaxService.ListSyntaxes().Except(_uiSyntaxes);
        
        foreach (var syntax in syntaxes)
            yield return new SyntaxDescriptor(syntax, syntax);
    }

    private async Task UpdateMonacoLanguageAsync(string syntax)
    {
        if (_monacoEditor == null)
            return;

        if (SyntaxService.GetSyntaxProviderByName(syntax) is not IMonacoSyntaxProvider syntaxProvider)
            return;

        var model = await _monacoEditor.GetModel();
        await Global.SetModelLanguage(model, syntaxProvider.Language);
    }


    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        _selectedSyntax = EditorContext.SelectedSyntaxProvider?.SyntaxName ?? UISyntax;
        _monacoLanguage = (EditorContext.SelectedSyntaxProvider as IMonacoSyntaxProvider)?.Language ?? "";
        return base.OnParametersSetAsync();
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            Language = _monacoLanguage,
            Value = InputValue,
            FontFamily = "Roboto Mono, monospace",
            RenderLineHighlight = "none",
            Minimap = new EditorMinimapOptions
            {
                Enabled = false
            },
            AutomaticLayout = true,
            LineNumbers = "on",
            Theme = "vs",
            RoundedSelection = true,
            ScrollBeyondLastLine = false,
            ReadOnly = false,
            OverviewRulerLanes = 0,
            OverviewRulerBorder = false,
            LineDecorationsWidth = 0,
            HideCursorInOverviewRuler = true,
            GlyphMargin = false,
            DomReadOnly = EditorContext.IsReadOnly
        };
    }

    private async Task OnSyntaxSelected(string syntax)
    {
        _selectedSyntax = syntax;

        var syntaxProvider = SyntaxService.GetSyntaxProviderByName(_selectedSyntax);
        var value = InputValue;
        var input = (WrappedInput?)EditorContext.Value ?? new WrappedInput();
        input.Expression = syntaxProvider.CreateExpression(value);
        await InvokeValueChangedCallback(input);
        await UpdateMonacoLanguageAsync(syntax);
    }

    private async Task OnMonacoContentChanged(ModelContentChangedEvent e)
    {
        if (_isInternalContentChange)
            return;

        var value = await _monacoEditor!.GetValue();

        // This event gets fired even when the content hasn't changed, but for example when the containing pane is resized.
        // This happens from within the monaco editor itself (or the Blazor wrapper, not sure).
        if (value == _lastMonacoEditorContent)
            return;
        
        var syntaxProvider = SyntaxService.GetSyntaxProviderByName(_selectedSyntax);
        var input = (WrappedInput?)EditorContext.Value ?? new WrappedInput();
        input.Expression = syntaxProvider.CreateExpression(value);
        _lastMonacoEditorContent = value;
        await ThrottleValueChangedCallback(input);
    }

    private async Task ThrottleValueChangedCallback(WrappedInput input) => await _throttledValueChanged.InvokeAsync(input);
    private async Task InvokeValueChangedCallback(WrappedInput input)
    {
        await InvokeAsync(async () => await EditorContext.OnValueChanged(input));
    }

    private async Task OnMonacoInitialized()
    {   
        _isInternalContentChange = true;
        var model = await _monacoEditor!.GetModel();
        _lastMonacoEditorContent = InputValue;
        await model.SetValue(InputValue);
        _isInternalContentChange = false;
        await Global.SetModelLanguage(model, _monacoLanguage);
    }

    public void Dispose() => _throttledValueChanged.Dispose();
}

public record SyntaxDescriptor(string Syntax, string DisplayName);