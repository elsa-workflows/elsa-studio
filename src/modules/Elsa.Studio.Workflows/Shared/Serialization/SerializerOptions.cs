using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elsa.Studio.Workflows.Shared.Serialization;

/// <summary>
/// Provides options for serializing and deserializing objects.
/// </summary>
public static class SerializerOptions
{
    /// <summary>
    /// Gets the serializer options for the log persistence configuration.
    /// </summary>
    public static JsonSerializerOptions LogPersistenceConfigSerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}