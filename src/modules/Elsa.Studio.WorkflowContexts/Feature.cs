using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;

namespace Elsa.Studio.WorkflowContexts;

/// <summary>
/// Registers the workflow contexts feature.
/// </summary>
[RemoteFeature("Elsa.WorkflowContexts")]
public class Feature : FeatureBase
{
    /// <inheritdoc />
    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        
    }
}