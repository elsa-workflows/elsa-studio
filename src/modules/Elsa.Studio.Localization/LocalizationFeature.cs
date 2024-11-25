using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Components;

namespace Elsa.Studio.Localization;
public class LocalizationFeature : FeatureBase
{
    private readonly IAppBarService _appBarService;

    public LocalizationFeature(IAppBarService appBarService)
    {
        _appBarService = appBarService;
    }

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        _appBarService.AddAppBarItem<LanguagePicker>();

        return ValueTask.CompletedTask;
    }
}
