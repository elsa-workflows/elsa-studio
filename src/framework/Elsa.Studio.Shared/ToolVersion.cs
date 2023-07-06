using JetBrains.Annotations;

namespace Elsa.Studio;

/// <summary>
/// Represents the version of the tool.
/// </summary>
[PublicAPI]
public static class ToolVersion
{
    public static readonly Version Version = new(3, 0, 0, 0);
}