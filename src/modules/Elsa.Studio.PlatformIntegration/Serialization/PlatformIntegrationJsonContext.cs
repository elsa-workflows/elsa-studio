using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Studio.PlatformIntegration.Serialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(WorkspaceArtifactRegistrationRequest))]
[JsonSerializable(typeof(WorkspaceArtifactResponse))]
[JsonSerializable(typeof(ProblemDetailsResponse))]
internal sealed partial class PlatformIntegrationJsonContext : JsonSerializerContext;
