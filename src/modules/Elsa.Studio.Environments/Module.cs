using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Components;
using Elsa.Studio.Environments.Tasks;
using Elsa.Studio.Models;

namespace Elsa.Studio.Environments;

/// <summary>
/// Represents the environments feature module for managing server environment selection in the app bar.
/// </summary>
public class Feature(IAppBarService appBarService, LoadEnvironmentsStartupTask loadEnvironmentsStartupTask) : FeatureBase
{
    /// <inheritdoc />
    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Circuit-scoped load so Blazor Server pickers see the same IEnvironmentService instance.
        // IStartupTask also runs this for hosts that execute startup tasks (e.g. WASM).
        await loadEnvironmentsStartupTask.LoadAsync(cancellationToken);
        appBarService.AddElement(new AppBarElement<EnvironmentPicker>());
    }
}
