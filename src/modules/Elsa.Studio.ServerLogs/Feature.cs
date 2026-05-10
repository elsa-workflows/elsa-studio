using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.ServerLogs;

/// <summary>
/// Represents the server logs feature module for the Elsa Studio application.
/// </summary>
[RemoteFeature("Elsa.ServerLogs.ShellFeatures.ServerLogStreaming")]
public class Feature : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.ServerLogs.ShellFeatures.ServerLogStreaming";
}
