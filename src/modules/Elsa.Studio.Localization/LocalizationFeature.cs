using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Components;

namespace Elsa.Studio.Localization;

/// <summary>
/// Registers localization UI elements and services for the studio shell.
/// </summary>
public class LocalizationFeature(IAppBarService appBarService) : FeatureBase
{
    /// <summary>
    /// Adds the language picker to the application bar when the feature initializes.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the initialization.</param>
    /// <returns>A completed task once the menu item is registered.</returns>
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddAppBarItem<LanguagePicker>();

        return ValueTask.CompletedTask;
    }
}
