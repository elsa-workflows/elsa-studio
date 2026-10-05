namespace Elsa.Studio.Login.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="string"/>.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Ensures the string starts with the specified character when non-empty.
    /// </summary>
    public static string EnsureStartsWith(this string? value, string prefix)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.StartsWith(prefix) ? value : prefix + value;
    }

    /// <summary>
    /// Ensures the string ends with the specified string when non-empty.
    /// </summary>
    public static string EnsureEndsWith(this string? value, string suffix)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.EndsWith(suffix) ? value : value + suffix;
    }
}
