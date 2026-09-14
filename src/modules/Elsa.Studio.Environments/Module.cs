using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Components;
using Elsa.Studio.Environments.Tasks;
using Elsa.Studio.Models;
using Refit;

namespace Elsa.Studio.Environments;

/// <summary>
/// Represents the environments feature module for managing server environment selection in the app bar.
/// </summary>
public class Feature(IAppBarService appBarService, LoadEnvironmentsStartupTask loadEnvironmentsStartupTask) : FeatureBase
{
    /// <inheritdoc />
    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Circuit-scoped load so Blazor Server pickers see the same IEnvironmentService instance.
            await loadEnvironmentsStartupTask.LoadAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            // A missing or unreachable /environments API must not block other features.
        }
        catch (ApiException)
        {
        }

        appBarService.AddElement(new AppBarElement<EnvironmentPicker>());
    }
}
