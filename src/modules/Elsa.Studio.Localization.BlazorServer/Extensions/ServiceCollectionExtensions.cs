using Elsa.Studio.Localization.BlazorServer.Services;
using Elsa.Studio.Localization.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Localization.BlazorServer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalizationModule(this IServiceCollection services, LocalizationConfig localizationConfig)
        {
            services.AddControllers();

            services.AddLocalizationModuleCore(localizationConfig);

            services.AddScoped<ICultureService, BlazorServerCultureService>();

            return services;
        }
    }
}
