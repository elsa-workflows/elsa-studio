using BlazorMonaco.Editor;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.UIHints.CodeEditor;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using ThrottleDebounce;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// A component that renders a code editor.
/// </summary>
public partial class Code : IDisposable
{
    private readonly string _monacoEditorId = $"monaco-editor-{Guid.NewGuid():N}";
    private StandaloneCodeEditor? _monacoEditor;
    private string _monacoLanguage = "";
    private string? _lastMonacoEditorContent;
    private readonly RateLimitedFunc<Task> _throttledValueChanged;
    private CodeEditorOptions _codeEditorOptions = new();
    private bool _isInternalContentChange;

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IEnumerable<IMonacoHandler> MonacoHandlers { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    /// <inheritdoc />
    public Code()
    {
        _throttledValueChanged = Debouncer.Debounce(InvokeValueChangedCallback, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// The context for the editor.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    private string InputValue => EditorContext.GetLiteralValueOrDefault();
    private InputDescriptor InputDescriptor => EditorContext.InputDescriptor;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _codeEditorOptions = EditorContext.InputDescriptor.GetCodeEditorOptions();
        _monacoLanguage = _codeEditorOptions.Language ?? "javascript";
        ;
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

    private async Task OnMonacoInitializedAsync()
    {
        try
        {
            _isInternalContentChange = true;
            var model = await _monacoEditor!.GetModel();
            _lastMonacoEditorContent = InputValue;
            await model.SetValue(InputValue);
            _isInternalContentChange = false;
            await Global.SetModelLanguage(JSRuntime, model, _monacoLanguage);
            await RunMonacoHandlersAsync(_monacoEditor);
        }
        catch (Microsoft.JSInterop.JSException ex) when (ex.Message.Contains("Couldn't find the editor"))
        {
            // This can happen when the component is being disposed while the Monaco editor is initializing.
            // This is a timing issue in Blazor WASM where the disposal and creation of Monaco editors can race.
            // We can safely ignore this error as the component is being recreated anyway.
            _isInternalContentChange = false;
        }
    }

    private async Task OnMonacoContentChanged(ModelContentChangedEvent e)
    {
        if (_isInternalContentChange)
            return;

        await _throttledValueChanged.InvokeAsync();
    }

    private async Task InvokeValueChangedCallback()
    {
        try
        {
            var value = await _monacoEditor!.GetValue();

            // This event gets fired even when the content hasn't changed, but for example when the containing pane is resized.
            // This happens from within the monaco editor itself (or the Blazor wrapper, not sure).
            if (value == _lastMonacoEditorContent)
                return;

            _lastMonacoEditorContent = value;
            var expression = Expression.CreateLiteral(value);

            await InvokeAsync(async () => await EditorContext.UpdateExpressionAsync(expression));
        }
        catch (Microsoft.JSInterop.JSException ex) when (ex.Message.Contains("Couldn't find the editor"))
        {
            // This can happen when the component is being disposed while the Monaco editor is initializing.
            // We can safely ignore this error as the component is being recreated anyway.
        }
    }

    private async Task RunMonacoHandlersAsync(StandaloneCodeEditor editor)
    {
        var customProps = new Dictionary<string, object>
        {
            { nameof(ActivityDescriptor), EditorContext.ActivityDescriptor },
            { nameof(InputDescriptor), EditorContext.InputDescriptor },
            { "WorkflowDefinitionId", EditorContext.WorkflowDefinition.DefinitionId }
        };

        var expressionDescriptor = EditorContext.SelectedExpressionDescriptor ?? new ExpressionDescriptor("Default", "Default");
        var context = new MonacoContext(editor, expressionDescriptor, customProps);

        foreach (var handler in MonacoHandlers)
            await handler.InitializeAsync(context);
    }

    private async Task ShowScriptEditor()
    {
        var param = new DialogParameters<CodeEditorDialog>
        {
            { x => x.Label, InputDescriptor.DisplayName },
            { x => x.HelperText, InputDescriptor.Description },
            { x => x.Value, _lastMonacoEditorContent ?? InputValue ?? string.Empty },
            { x => x.LanguageLabel, InputDescriptor.DefaultSyntax },
            { x => x.MonacoLanguage, _monacoLanguage },
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, Position = DialogPosition.Center, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialogRef = await DialogService.ShowAsync<CodeEditorDialog>("Code", param, options);
        var dialogResult = await dialogRef.Result;

        if (dialogResult?.Data is string newValue && InputValue != newValue)
        {
            _lastMonacoEditorContent = newValue;
            var expression = Expression.CreateLiteral(newValue);
            await EditorContext.UpdateExpressionAsync(expression);

            try
            {
                var model = await _monacoEditor!.GetModel();
                await model.SetValue(newValue);
            }
            catch (Microsoft.JSInterop.JSException ex) when (ex.Message.Contains("Couldn't find the editor"))
            {
                // This can happen when the component is being disposed while the Monaco editor is initializing.
                // We can safely ignore this error as the component is being recreated anyway.
            }
        }
    }

    void IDisposable.Dispose() => _throttledValueChanged.Dispose();
}