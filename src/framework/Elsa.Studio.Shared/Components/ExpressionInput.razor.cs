using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Scripting.Extensions;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
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
public partial class ExpressionInput : IDisposable
{
    private const string DefaultSyntax = "Literal";
    private readonly string[] _uiSyntaxes = ["Literal", "Object"];
    private string _selectedExpressionType = DefaultSyntax;
    private string _selectedExpressionTypeDisplayName = DefaultSyntax;
    private StandaloneCodeEditor? _monacoEditor = null!;
    private bool _isInternalContentChange;
    private string _monacoEditorId = $"monaco-editor-{Guid.NewGuid()}:N";
    private string? _lastMonacoEditorContent;
    private RateLimitedFunc<WrappedInput, Task> _throttledValueChanged;
    private ICollection<ExpressionDescriptor> _expressionDescriptors = new List<ExpressionDescriptor>();

    /// <inheritdoc />
    public ExpressionInput()
    {
        _throttledValueChanged = Debouncer.Debounce<WrappedInput, Task>(InvokeValueChangedCallbackAsync, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// The context for the editor.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = null!;

    /// <summary>
    /// The content to render inside the editor.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = null!;
    
    /// <summary>
    /// A flag indicating whether the editor should only display code.
    /// </summary>
    [Parameter] public bool IsCodeOnly { get; set; }

    [Inject] private TypeDefinitionService TypeDefinitionService { get; set; } = null!;
    [Inject] private IExpressionService ExpressionService { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IEnumerable<IMonacoHandler> MonacoHandlers { get; set; } = null!;
    [Inject] private IUIHintService UIHintService { get; set; } = null!;
    private IEnumerable<ExpressionDescriptor> BrowsableExpressionDescriptors => _expressionDescriptors.Where(x => x.IsBrowsable);
    private ExpressionDescriptor? SelectedExpressionDescriptor => _expressionDescriptors.FirstOrDefault(x => x.Type == _selectedExpressionType);
    private string MonacoLanguage => SelectedExpressionDescriptor?.GetMonacoLanguage() ?? string.Empty;
    private string UISyntax => EditorContext.UIHintHandler.UISyntax;
    private string ComponentType => EditorContext.InputDescriptor.UIHint;
    private bool IsUISyntax => !IsCodeOnly && _selectedExpressionType == UISyntax;
    private string? ButtonIcon => IsUISyntax ? Icons.Material.Filled.MoreVert : null;
    private string? ButtonLabel => IsUISyntax ? null : _selectedExpressionTypeDisplayName;
    private Variant ButtonVariant => IsUISyntax ? default : Variant.Filled;
    private Color ButtonColor => IsUISyntax ? default : Color.Primary;
    private string? ButtonEndIcon => IsUISyntax ? null : Icons.Material.Filled.KeyboardArrowDown;
    private Color ButtonEndColor => IsUISyntax ? default : Color.Secondary;
    private bool ShowMonacoEditor => IsCodeOnly || (!IsUISyntax && EditorContext.InputDescriptor.IsWrapped && MonacoSyntaxExist);
    private string DisplayName => EditorContext.InputDescriptor.DisplayName ?? EditorContext.InputDescriptor.Name;
    private string? Description => EditorContext.InputDescriptor.Description;
    private string InputValue => EditorContext.GetExpressionValueOrDefault();

    private string? MonacoSyntax { get; set; }
    private bool MonacoSyntaxExist => !string.IsNullOrEmpty(MonacoLanguage);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var defaultDescriptor = new ExpressionDescriptor(UISyntax, "Default");
        var expressionDescriptors = (await ExpressionService.ListDescriptorsAsync()).ExceptBy(_uiSyntaxes, x => x.Type).ToList();

        if (IsCodeOnly)
            _selectedExpressionType = expressionDescriptors.First().Type;
        else
            expressionDescriptors = expressionDescriptors.Prepend(defaultDescriptor).ToList();

        _expressionDescriptors = expressionDescriptors.ToList();
    }

    private async Task UpdateMonacoLanguageAsync()
    {
        if (_monacoEditor == null)
            return;

        var expressionDescriptor = SelectedExpressionDescriptor;

        if (expressionDescriptor == null)
            return;

        var monacoLanguage = expressionDescriptor.GetMonacoLanguage();
        _selectedExpressionTypeDisplayName = expressionDescriptor.DisplayName;

        if (string.IsNullOrWhiteSpace(monacoLanguage))
            return;

        var model = await _monacoEditor.GetModel();
        await Global.SetModelLanguage(JSRuntime, model, monacoLanguage);
        await RunMonacoHandlersAsync(_monacoEditor);
    }

    /// <inheritdoc />
    protected override Task OnParametersSetAsync()
    {
        var selectedExpressionDescriptor = EditorContext.SelectedExpressionDescriptor;
        _selectedExpressionType = selectedExpressionDescriptor?.Type ?? UISyntax;
        _selectedExpressionTypeDisplayName = selectedExpressionDescriptor?.DisplayName ?? UISyntax;
        
        UpdateUIHintAsync(EditorContext);
        return base.OnParametersSetAsync();
    }

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        return new()
        {
            Language = MonacoLanguage,
            Value = InputValue,
            FontFamily = "Roboto Mono, monospace",
            RenderLineHighlight = "none",
            FixedOverflowWidgets = true,
            Minimap = new()
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
            ReadOnly = EditorContext.IsReadOnly,
            DomReadOnly = EditorContext.IsReadOnly
        };
    }

    private async Task OnMonacoInitializedAsync()
    {
        _isInternalContentChange = true;
        var model = await _monacoEditor!.GetModel();
        _lastMonacoEditorContent = InputValue;
        await model.SetValue(InputValue);
        _isInternalContentChange = false;
        await Global.SetModelLanguage(JSRuntime, model, MonacoLanguage);
        await RunMonacoHandlersAsync(_monacoEditor);
    }

    private async Task OnSyntaxSelectedAsync(string syntax)
    {
        _selectedExpressionType = syntax;

        var value = InputValue;
        var input = (WrappedInput?)EditorContext.Value ?? new WrappedInput();

        input.Expression = new(_selectedExpressionType, value);
        await InvokeValueChangedCallbackAsync(input);
        await UpdateMonacoLanguageAsync();
    }

    /// <summary>
    /// Used to display the UI hint when the user selects any other choice than a language supported by the monaco editor.
    /// </summary>
    private void UpdateUIHintAsync(DisplayInputEditorContext editorContext)
    {
        if (_selectedExpressionType == "Variable")
        {
            var uiHintHandler = UIHintService.GetHandler("variable-picker");
            var editor = uiHintHandler.DisplayInputEditor(editorContext);
            ChildContent = editor;
            return;
        }

        if (_selectedExpressionType == "Input")
        {
            var uiHintHandler = UIHintService.GetHandler("input-picker");
            var editor = uiHintHandler.DisplayInputEditor(editorContext);
            ChildContent = editor;

            return;
        }

        if (MonacoSyntax == null
            && _selectedExpressionType == editorContext.SelectedExpressionDescriptor?.Type
            && editorContext.SelectedExpressionDescriptor.Properties.TryGetValue("UIHint", out var uiHint))
        {
            var uiHintHandler = UIHintService.GetHandler(uiHint);
            var editor = uiHintHandler.DisplayInputEditor(editorContext);
            _selectedExpressionTypeDisplayName = editorContext.SelectedExpressionDescriptor.DisplayName;

            ChildContent = editor;
        }
    }

    private async Task OnMonacoContentChangedAsync(ModelContentChangedEvent e)
    {
        if (_isInternalContentChange)
            return;

        var value = await _monacoEditor!.GetValue();

        // This event gets fired even when the content hasn't changed, but for example when the containing pane is resized.
        // This happens from within the monaco editor itself (or the Blazor wrapper, not sure).
        if (value == _lastMonacoEditorContent)
            return;

        var input = (WrappedInput?)EditorContext.Value ?? new WrappedInput();
        input.Expression = new(_selectedExpressionType, value);
        _lastMonacoEditorContent = value;
        await ThrottleValueChangedCallbackAsync(input);
    }

    private async Task ThrottleValueChangedCallbackAsync(WrappedInput input) => await _throttledValueChanged.InvokeAsync(input);

    private async Task InvokeValueChangedCallbackAsync(WrappedInput input)
    {
        await InvokeAsync(async () => await EditorContext.OnValueChanged(input));
    }

    private async Task RunMonacoHandlersAsync(StandaloneCodeEditor editor)
    {
        var customProps = new Dictionary<string, object>
        {
            { nameof(ActivityDescriptor), EditorContext.ActivityDescriptor },
            { nameof(PropertyDescriptor), EditorContext.InputDescriptor },
            { "WorkflowDefinitionId", EditorContext.WorkflowDefinition.DefinitionId }
        };

        var expressionDescriptor = SelectedExpressionDescriptor?? new ExpressionDescriptor("Literal", "Default");
        var context = new MonacoContext(editor, expressionDescriptor, customProps);

        foreach (var handler in MonacoHandlers)
            await handler.InitializeAsync(context);
    }

    /// <inheritdoc />
    void IDisposable.Dispose() => _throttledValueChanged.Dispose();
}