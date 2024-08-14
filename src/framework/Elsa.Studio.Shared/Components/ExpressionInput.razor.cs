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
using MudExtensions;
using Polly;
using System.Reflection.Emit;
using ThrottleDebounce;
using static MudBlazor.Colors;

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
    private string _monacoLanguage = "";
    private StandaloneCodeEditor? _monacoEditor = default!;
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
    [Parameter]
    public DisplayInputEditorContext EditorContext { get; set; } = default!;

    /// <summary>
    /// The content to render inside the editor.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    [Inject] private TypeDefinitionService TypeDefinitionService { get; set; } = default!;
    [Inject] private IExpressionService ExpressionService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IEnumerable<IMonacoHandler> MonacoHandlers { get; set; } = default!;

    private IEnumerable<ExpressionDescriptor> BrowseableExpressionDescriptors => _expressionDescriptors.Where(x => x.IsBrowsable);
    private string UISyntax => EditorContext.UIHintHandler.UISyntax;
    private bool IsUISyntax => _selectedExpressionType == UISyntax;
    private string? ButtonIcon => IsUISyntax ? Icons.Material.Filled.MoreVert : default;
    private string? ButtonLabel => IsUISyntax ? default : _selectedExpressionTypeDisplayName;
    private Variant ButtonVariant => IsUISyntax ? default : Variant.Filled;
    private Color ButtonColor => IsUISyntax ? default : Color.Primary;
    private string? ButtonEndIcon => IsUISyntax ? default : Icons.Material.Filled.KeyboardArrowDown;
    private Color ButtonEndColor => IsUISyntax ? default : Color.Secondary;
    private bool ShowMonacoEditor => !IsUISyntax && EditorContext.InputDescriptor.IsWrapped && MonacoSyntaxExist;
    private string DisplayName => EditorContext.InputDescriptor.DisplayName ?? EditorContext.InputDescriptor.Name;
    private string? Description => EditorContext.InputDescriptor.Description;
    private string InputValue => EditorContext.GetExpressionValueOrDefault();

    private string? MonacoSyntax { get; set; }
    private bool MonacoSyntaxExist => !String.IsNullOrEmpty(_monacoLanguage);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var defaultDescriptor = new ExpressionDescriptor(UISyntax, "Default");
        var expressionDescriptors = (await ExpressionService.ListDescriptorsAsync())
            .ExceptBy(_uiSyntaxes, x => x.Type)
            .Prepend(defaultDescriptor);

        _expressionDescriptors = expressionDescriptors.ToList();
    }

    private async Task UpdateMonacoLanguageAsync(string expressionType)
    {
        if (_monacoEditor == null)
            return;

        var expressionDescriptor = await ExpressionService.GetByTypeAsync(expressionType);

        if (expressionDescriptor == null)
            return;

        var monacoLanguage = expressionDescriptor.GetMonacoLanguage();
        _monacoLanguage = monacoLanguage;
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

        _monacoLanguage = selectedExpressionDescriptor?.GetMonacoLanguage() ?? "";
        UpdateUIHintAsync(EditorContext);
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
            OverviewRulerLanes = 0,
            OverviewRulerBorder = false,
            LineDecorationsWidth = 0,
            HideCursorInOverviewRuler = true,
            GlyphMargin = false,
            ReadOnly = EditorContext.IsReadOnly,
            DomReadOnly = EditorContext.IsReadOnly
        };
    }

    private async Task OnSyntaxSelectedAsync(string syntax)
    {
        _selectedExpressionType = syntax;

        var value = InputValue;
        var input = (WrappedInput?)EditorContext.Value ?? new WrappedInput();
        input.Expression = new Expression(_selectedExpressionType, value);
        await InvokeValueChangedCallbackAsync(input);
        await UpdateMonacoLanguageAsync(syntax);
        
    }
    [Inject] private IUIHintService UIHintService { get; set; } = default!;
    private void UpdateUIHintAsync(DisplayInputEditorContext editorContext)
    {
        if (MonacoSyntax == null
            && _selectedExpressionType == editorContext?.SelectedExpressionDescriptor?.Type
            && editorContext.SelectedExpressionDescriptor.Properties.ContainsKey("UIHint"))
        {
            var uiHint = editorContext.SelectedExpressionDescriptor.Properties["UIHint"];

            //Create ChildContent
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
        input.Expression = new Expression(_selectedExpressionType, value);
        _lastMonacoEditorContent = value;
        await ThrottleValueChangedCallbackAsync(input);
    }

    private async Task ThrottleValueChangedCallbackAsync(WrappedInput input) => await _throttledValueChanged.InvokeAsync(input);

    private async Task InvokeValueChangedCallbackAsync(WrappedInput input)
    {
        await InvokeAsync(async () => await EditorContext.OnValueChanged(input));
    }

    private async Task OnMonacoInitializedAsync()
    {
        _isInternalContentChange = true;
        var model = await _monacoEditor!.GetModel();
        _lastMonacoEditorContent = InputValue;
        await model.SetValue(InputValue);
        _isInternalContentChange = false;
        await Global.SetModelLanguage(JSRuntime, model, _monacoLanguage);
        await RunMonacoHandlersAsync(_monacoEditor);
    }

    private async Task RunMonacoHandlersAsync(StandaloneCodeEditor editor)
    {
        var context = new MonacoContext(editor, EditorContext);

        foreach (var handler in MonacoHandlers)
            await handler.InitializeAsync(context);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _throttledValueChanged.Dispose();
    }
}