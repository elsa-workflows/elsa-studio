using System.Text.Json;
using Elsa.Studio.Workflows.Domain.Contracts;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A service that detects whether a JSON string is a workflow definition using a schema.
/// </summary>
#if JETBRAINS_ANNOTATIONS
[UsedImplicitly]
#endif
public class SchemaWorkflowJsonDetector : IWorkflowJsonDetector
{
    /// <inheritdoc />
    public bool IsWorkflowSchema(string json)
    {
        var jsonDocument = JsonDocument.Parse(json);
        var rootElement = jsonDocument.RootElement;

        if (!rootElement.TryGetProperty("$schema", out var schemaUrl))
            return false;

        if (schemaUrl.GetString()?.StartsWith("https://elsaworkflows.io/schemas/workflow-definition") == false)
            return false;

        return true;
    }
}