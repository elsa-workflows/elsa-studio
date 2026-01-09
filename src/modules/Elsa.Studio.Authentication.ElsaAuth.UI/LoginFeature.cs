using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.UI.Components;

namespace Elsa.Studio.Authentication.ElsaAuth.UI;

/// <summary>
/// Adds a login app-bar component.
/// </summary>
public class LoginFeature(IAppBarService appBarService) : FeatureBase
{
    /// <inheritdoc />
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddComponent<LoginState>();
        return base.InitializeAsync(cancellationToken);
    }
}
