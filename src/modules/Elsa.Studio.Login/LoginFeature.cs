using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Components;

namespace Elsa.Studio.Login;

/// <inheritdoc/>
public class LoginFeature(IAppBarService appBarService) : FeatureBase
{
    /// <inheritdoc/>
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<LoginState>();
        return base.InitializeAsync(cancellationToken);
    }
}