using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Components;

namespace Elsa.Studio.Environments;

/// <summary>
/// Adds environment selection capabilities to the studio shell.
/// </summary>
public class Feature(IAppBarService appBarService) : FeatureBase
{
    /// <summary>
    /// Registers the environment picker in the application bar.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the initialization.</param>
    /// <returns>A completed task once the picker has been added.</returns>
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddAppBarItem<EnvironmentPicker>();

        return ValueTask.CompletedTask;
    }
}