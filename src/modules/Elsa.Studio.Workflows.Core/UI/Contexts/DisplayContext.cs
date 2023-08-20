using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.UI.Args;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.UI.Contexts;

public record DisplayContext(
    JsonObject Activity, 
    Func<JsonObject, Task>? ActivitySelectedCallback = default,
    Func<ActivityEmbeddedPortSelectedArgs, Task>? ActivityEmbeddedPortSelectedCallback = default,
    Func<Task>? GraphUpdatedCallback = default, 
    bool IsReadOnly = false,
    IDictionary<string, ActivityStats>? ActivityStats = default);