using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Components;

public partial class ExpressionInput
{
    private const string DefaultSyntax = "Literal";
    private string _selectedSyntax = DefaultSyntax;

    public ExpressionInput()
    {
        SupportedSyntaxes = new List<string>
        {
            DefaultSyntax,
            "JavaScript",
            "Liquid",
            "Python"
        };
    }

    [Parameter] public string InputId { get; set; } = default!;
    [Parameter] public ICollection<string> SupportedSyntaxes { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    private IEnumerable<string> Syntaxes => SupportedSyntaxes;
    private string? ButtonIcon => _selectedSyntax == DefaultSyntax ? MudBlazor.Icons.Material.Filled.MoreVert : default;
    private string? ButtonLabel => _selectedSyntax == DefaultSyntax ? default : _selectedSyntax;
    private Variant ButtonVariant => _selectedSyntax == DefaultSyntax ? default : Variant.Filled;
    private Color ButtonColor => _selectedSyntax == DefaultSyntax ? default : Color.Primary;
    private string? ButtonEndIcon => _selectedSyntax == DefaultSyntax ? default : Icons.Material.Filled.KeyboardArrowDown;
    private Color ButtonEndColor => _selectedSyntax == DefaultSyntax ? default : Color.Secondary;
    private bool ShowMonacoEditor => _selectedSyntax != DefaultSyntax;
    private string MonacoEditorId => $"{InputId}-monaco-editor";

    private StandaloneEditorConstructionOptions ConfigureMonacoEditor(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            Language = "javascript",
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

    private void OnSyntaxSelected(string syntax)
    {
        _selectedSyntax = syntax;
    }
}