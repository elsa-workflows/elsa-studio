using System.Text.Json;
using Elsa.Studio.Workflows.Domain.Contracts;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A service that detects whether a JSON string is a workflow definition using a simple heuristic.
/// </summary>
#if JETBRAINS_ANNOTATIONS
[UsedImplicitly]
#endif
public class SimpleWorkflowJsonDetector : IWorkflowJsonDetector
{
    /// <inheritdoc />
    public bool IsWorkflowSchema(string json)
    {
        var jsonDocument = JsonDocument.Parse(json);
        var rootElement = jsonDocument.RootElement;

        if (!rootElement.TryGetProperty("definitionId", out _))
            return false;

        if (!rootElement.TryGetProperty("id", out _))
            return false;

        if (!rootElement.TryGetProperty("root", out _))
            return false;

        if (!rootElement.TryGetProperty("name", out _))
            return false;

        return true;
    }
}