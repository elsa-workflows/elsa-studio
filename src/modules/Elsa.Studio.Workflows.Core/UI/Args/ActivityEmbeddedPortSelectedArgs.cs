using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.UI.Args;

public record ActivityEmbeddedPortSelectedArgs(JsonObject Activity, string PortName);