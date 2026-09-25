using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Components;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Models;

namespace Elsa.Studio.Environments;

/// <summary>
/// Represents the environments feature module for managing server environment selection in the app bar.
/// </summary>
public class Feature(
    IAppBarService appBarService,
    EnvironmentLoader environmentLoader) : FeatureBase
{
    /// <inheritdoc />
    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        // After authentication in the app shell (MainLayout), when a circuit and user context exist.
        await environmentLoader.EnsureLoadedAsync(cancellationToken);
        appBarService.AddElement(new AppBarElement<EnvironmentPicker>());
    }
}
