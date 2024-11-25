using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Components;

namespace Elsa.Studio.Localization;
public class LocalizationFeature(IAppBarService appBarService) : FeatureBase
{
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        appBarService.AddAppBarItem<LanguagePicker>();

        return ValueTask.CompletedTask;
    }
}
