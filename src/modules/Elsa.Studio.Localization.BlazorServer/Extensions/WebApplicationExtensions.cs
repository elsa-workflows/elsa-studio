using Elsa.Studio.Localization.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Localization.BlazorServer.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseElsaLocalization(this WebApplication app)
        {

            var supportedCultures = app.Services.GetService<IOptions<LocalizationOptions>>()?.Value.SupportedCultures;

            if (supportedCultures != null)
            {
                var localizationOptions = new RequestLocalizationOptions()
                    .SetDefaultCulture(supportedCultures[0])
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures);

                app.UseRequestLocalization(localizationOptions);
            }

            return app;
        }
    }
}
