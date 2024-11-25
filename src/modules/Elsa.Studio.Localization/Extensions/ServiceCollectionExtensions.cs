using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Translations;

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
