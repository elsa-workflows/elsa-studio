using BlazorMonaco.Editor;
using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Components;

public partial class ExpressionInput
{
    private const string DefaultSyntax = "Literal";
    private string _selectedSyntax = DefaultSyntax;
    private string _monacoLanguage = "javascript";
    private StandaloneCodeEditor? _monacoEditor = default!;
    private bool _isInternalContentChange;
    private bool _isMonacoInitialized;

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;
    [Inject] private ISyntaxService SyntaxService { get; set; } = default!;

    private IEnumerable<string> SupportedSyntaxes => SyntaxService.ListSyntaxes();
    private string? SelectedSyntax => EditorContext.SyntaxProvider?.SyntaxName;
    private string? SelectedLanguage => (EditorContext.SyntaxProvider as IMonacoSyntaxProvider)?.Language;
    private string? ButtonIcon => _selectedSyntax == DefaultSyntax ? Icons.Material.Filled.MoreVert : default;
    private string? ButtonLabel => _selectedSyntax == DefaultSyntax ? default : _selectedSyntax;
    private Variant ButtonVariant => _selectedSyntax == DefaultSyntax ? default : Variant.Filled;
    private Color ButtonColor => _selectedSyntax == DefaultSyntax ? default : Color.Primary;
    private string? ButtonEndIcon => _selectedSyntax == DefaultSyntax ? default : Icons.Material.Filled.KeyboardArrowDown;
    private Color ButtonEndColor => _selectedSyntax == DefaultSyntax ? default : Color.Secondary;
    private bool ShowMonacoEditor => _selectedSyntax != DefaultSyntax;
    private string InputId => $"{EditorContext.Activity.Id.Camelize()}-{EditorContext.InputDescriptor.Name.Camelize()}";
    private string MonacoEditorId => $"{InputId}-monaco-editor";
    private string DisplayName => EditorContext.InputDescriptor.DisplayName ?? EditorContext.InputDescriptor.Name;
    private string? Description => EditorContext.InputDescriptor.Description;
    private string InputValue => EditorContext.Value?.Expression.ToString() ?? string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        _selectedSyntax = SelectedSyntax ?? DefaultSyntax;
        _monacoLanguage = SelectedLanguage ?? "javascript";
        
        if (_isMonacoInitialized)
        {
            if (!_isInternalContentChange)
            {
                _isInternalContentChange = true;
                var model = await _monacoEditor!.GetModel();
                
                await model.SetValue(InputValue);
                _isInternalContentChange = false;
                
                await Global.SetModelLanguage(model, _monacoLanguage);
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // if (_isMonacoInitialized)
        // {
        //     if(_monacoEditor!.Id == MonacoEditorId)
        //     {
        //         if (!_isInternalContentChange)
        //         {
        //             _isInternalContentChange = true;
        //             var model = await _monacoEditor.GetModel();
        //             await model.SetValue(InputValue);
        //             _isInternalContentChange = false;
        //         }
        //     }
        //     else
        //     {
        //         StateHasChanged();
        //     }
        // }
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
            GlyphMargin = false
        };
    }

    private async Task OnSyntaxSelected(string syntax)
    {
        _selectedSyntax = syntax;

        if (_monacoEditor == null)
            return;

        var syntaxProvider = SyntaxService.GetSyntaxProviderByName(syntax) as IMonacoSyntaxProvider;

        if (syntaxProvider == null)
            return;

        var model = await _monacoEditor.GetModel();
        await Global.SetModelLanguage(model, syntaxProvider.Language);
    }

    private async Task OnMonacoContentChanged(ModelContentChangedEvent e)
    {
        if (_isInternalContentChange)
            return;

        var syntaxProvider = SyntaxService.GetSyntaxProviderByName(_selectedSyntax);
        var value = await _monacoEditor!.GetValue();

        if (EditorContext.OnValueChanged.HasDelegate)
        {
            var input = EditorContext.Value ?? new ActivityInput();
            input.Expression = syntaxProvider.CreateExpression(value);
            await EditorContext.OnValueChanged.InvokeAsync(input);
        }
    }

    private void OnMonacoInitialized()
    {
        _isMonacoInitialized = true;
    }

    private async Task OnMonacoConfigChanged(ConfigurationChangedEvent arg)
    {
        // _isInternalContentChange = true;
        // await _monacoEditor!.SetValue(InputValue);
        // _isInternalContentChange = false;
    }

    private async Task OnMonacoModelChanged(ModelChangedEvent arg)
    {
    }
}