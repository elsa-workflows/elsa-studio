using System.Text.Json;

namespace Elsa.Studio.UIHintHandlers.Helpers;

public static class JsonParser
{
    /// <summary>
    /// Parses the specified JSON string into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="defaultValueFactory">A factory function that returns a default value of type <typeparamref name="T"/>.</param>
    /// <typeparam name="T">The type of the object to parse the JSON string into.</typeparam>
    /// <returns>An object of type <typeparamref name="T"/>.</returns>
    public static T ParseJson<T>(string? json, Func<T> defaultValueFactory)
    {
        if (string.IsNullOrWhiteSpace(json))
            return defaultValueFactory();

        try
        {
            return JsonSerializer.Deserialize<T>(json) ?? defaultValueFactory();
        }
        catch (JsonException)
        {
            return defaultValueFactory();
        }
    }
}