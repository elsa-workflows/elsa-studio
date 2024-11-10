using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Components;
using Elsa.Studio.Localization.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
