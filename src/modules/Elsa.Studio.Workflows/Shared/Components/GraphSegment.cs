using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Shared.Components;

public record GraphSegment(JsonObject Activity, string PortName, JsonObject? EmbeddedActivity = default);