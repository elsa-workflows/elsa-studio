using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.Scripting.Extensions;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using ThrottleDebounce;

namespace Elsa.Studio.Components;

/// <summary>
/// A component that renders an input for an expression.
/// </summary>
public partial class ExpressionEditor : IDisposable
{
    private readonly string[] _expressionTypeBlacklist = ["Literal", "Object", "Variable"];
    private string? _selectedExpressionType;
    private StandaloneCodeEditor? _monacoEditor;
    private bool _isInternalContentChange;
    private string _monacoEditorId = $"monaco-editor-{Guid.NewGuid()}:N";
    private string? _lastMonacoEditorContent;
    private RateLimitedFunc<Expression, Task> _throttledValueChanged;
    private ICollection<ExpressionDescriptor> _expressionDescriptors = new List<ExpressionDescriptor>();

    /// <inheritdoc />
    public ExpressionEditor()
    {
        _throttledValueChanged = Debouncer.Debounce<Expression, Task>(InvokeValueChangedCallbackAsync, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// The content to render inside the editor.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;
    
    [Parameter] public Expression? Expression { get; set; } = new(string.Empty, string.Empty);
    [Parameter] public string DefaultOption { get; set; } = "Default";
    [Parameter] public string DisplayName { get; set; } = string.Empty;
    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    [Parameter] public Func<Expression?, Task>? ExpressionChanged { get; set; }

    [Inject] private TypeDefinitionService TypeDefinitionService { get; set; } = default!;
    [Inject] private IExpressionService ExpressionService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IEnumerable<IMonacoHandler> MonacoHandlers { get; set; } = default!;
    [Inject] private IUIHintService UIHintService { get; set; } = default!;
    private IEnumerable<ExpressionDescriptor> BrowsableExpressionDescriptors => _expressionDescriptors.Where(x => x.IsBrowsable);
    private ExpressionDescriptor? SelectedExpressionDescriptor => _expressionDescriptors.FirstOrDefault(x => x.Type == _selectedExpressionType);
    private string SelectedExpressionTypeDisplayName => SelectedExpressionDescriptor?.DisplayName ?? DefaultOption;
    private string MonacoLanguage => SelectedExpressionDescriptor?.GetMonacoLanguage() ?? string.Empty;
    private bool DisplayCodeEditor => _selectedExpressionType != null && _selectedExpressionType != DefaultOption;
    private bool DisplayModePicker => !DisplayCodeEditor;
    private string? ButtonIcon => DisplayModePicker ? Icons.Material.Filled.MoreVert : default;
    private string? ButtonLabel => DisplayModePicker ? default : SelectedExpressionTypeDisplayName;
    private Variant ButtonVariant => DisplayModePicker ? default : Variant.Filled;
    private Color ButtonColor => DisplayModePicker ? default : Color.Primary;
    private string? ButtonEndIcon => DisplayModePicker ? default : Icons.Material.Filled.KeyboardArrowDown;
    private Color ButtonEndColor => DisplayModePicker ? default : Color.Secondary;

    private string? MonacoSyntax { get; set; }
    private bool MonacoSyntaxExist => !string.IsNullOrEmpty(MonacoLanguage);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var expressionDescriptors = (await ExpressionService.ListDescriptorsAsync()).ExceptBy(_expressionTypeBlacklist, x => x.Type).ToList();
        _selectedExpressionType = expressionDescriptors.First().Type;
        _expressionDescriptors = expressionDescriptors.ToList();
    }

    private async Task UpdateMonacoLanguageAsync()
    {
        if (_monacoEditor == null)
            return;
        
        if (SelectedExpressionDescriptor?.Type == null)
            return;

        if (string.IsNullOrWhiteSpace(MonacoLanguage))
            return;

        var model = await _monacoEditor.GetModel();
        await Global.SetModelLanguage(JSRuntime, model, MonacoLanguage);
        await RunMonacoHandlersAsync(_monacoEditor);
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        _selectedExpressionType = Expression?.Type;
        return base.OnParametersSetAsync();
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            Language = MonacoLanguage,
            Value = Expression?.Value?.ToString() ?? "",
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
            OverviewRulerLanes = 0,
            OverviewRulerBorder = false,
            LineDecorationsWidth = 0,
            HideCursorInOverviewRuler = true,
            GlyphMargin = false,
            ReadOnly = ReadOnly,
            DomReadOnly = ReadOnly
        };
    }

    private async Task OnSyntaxSelectedAsync(string? syntax)
    {
        _selectedExpressionType = syntax;

        var value = Expression?.Value?.ToString() ?? "";
        var expressionType = syntax ?? DefaultOption;
        var expression = new Expression(expressionType, value);
        await InvokeValueChangedCallbackAsync(expression);
        await UpdateMonacoLanguageAsync();
        StateHasChanged();
    }
    
    private async Task OnMonacoContentChangedAsync(ModelContentChangedEvent e)
    {
        if (_isInternalContentChange)
            return;

        var value = await _monacoEditor!.GetValue();

        // This event gets fired even when the content hasn't changed, for example, when the containing pane is resized.
        // This happens from within the monaco editor itself (or the Blazor wrapper, not sure).
        if (value == _lastMonacoEditorContent)
            return;
        
        var expression = new Expression(_selectedExpressionType!, value);
        _lastMonacoEditorContent = value;
        await ThrottleValueChangedCallbackAsync(expression);
    }

    private async Task ThrottleValueChangedCallbackAsync(Expression expression) => await _throttledValueChanged.InvokeAsync(expression);

    private async Task InvokeValueChangedCallbackAsync(Expression? expression)
    {
        await InvokeAsync(async () =>
        {
            if (ExpressionChanged != null)
                await ExpressionChanged.Invoke(expression);
        });
    }

    private async Task OnMonacoInitializedAsync()
    {
        _isInternalContentChange = true;
        var model = await _monacoEditor!.GetModel();
        var expressionText = Expression?.Value?.ToString() ?? "";
        _lastMonacoEditorContent = expressionText;
        await model.SetValue(expressionText);
        _isInternalContentChange = false;
        await Global.SetModelLanguage(JSRuntime, model, MonacoLanguage);
        await RunMonacoHandlersAsync(_monacoEditor);
    }

    private async Task RunMonacoHandlersAsync(StandaloneCodeEditor editor)
    {
        if(SelectedExpressionDescriptor == null)
            return;
        
        var context = new MonacoContext(editor, SelectedExpressionDescriptor, CustomProperties);

        foreach (var handler in MonacoHandlers)
            await handler.InitializeAsync(context);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _throttledValueChanged.Dispose();
    }
}