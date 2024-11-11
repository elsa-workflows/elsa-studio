using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Translations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalizationModuleCore(this IServiceCollection services,LocalizationConfig localizationConfig)
    {
        services.AddLocalization();

        services.AddMudTranslations();
        
        services.Configure(localizationConfig.ConfigureLocalizationOptions);

        services.AddScoped<IFeature, LocalizationFeature>();

        return services;
    }
}
