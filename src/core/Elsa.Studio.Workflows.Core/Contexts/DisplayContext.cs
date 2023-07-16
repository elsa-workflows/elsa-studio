using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Args;

namespace Elsa.Studio.Workflows.Contexts;

public record DisplayContext(
    JsonObject Activity, 
    Func<JsonObject, Task>? ActivitySelectedCallback = default,
    Func<ActivityEmbeddedPortSelectedArgs, Task>? ActivityEmbeddedPortSelectedCallback = default,
    Func<Task>? GraphUpdatedCallback = default, 
    bool IsReadOnly = false);