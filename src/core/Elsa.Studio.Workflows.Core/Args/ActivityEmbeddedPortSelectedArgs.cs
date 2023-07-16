using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Args;

public record ActivityEmbeddedPortSelectedArgs(JsonObject Activity, string PortName);