using BlazorMonaco.Editor;

namespace Elsa.Studio.Models;

/// <summary>
/// Represents a context for working with the Monaco editor.
/// </summary>
public record MonacoContext(StandaloneCodeEditor Editor, DisplayInputEditorContext DisplayInputEditorContext);