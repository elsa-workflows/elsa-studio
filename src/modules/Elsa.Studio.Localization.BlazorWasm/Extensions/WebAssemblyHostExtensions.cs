using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization.BlazorWasm.Extensions
{
    public static class WebAssemblyHostExtensions
    {
        public static async Task<WebAssemblyHost> UseElsaLocalization(this WebAssemblyHost host) {

            const string defaultCulture = "en-US";
            var js = host.Services.GetRequiredService<IJSRuntime>();
            var module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.Localization.BlazorWasm/blazorCulture.js");
            var result = await js.InvokeAsync<string>("blazorCulture.get");

            var culture = CultureInfo.GetCultureInfo(result ?? defaultCulture);

            if (result == null)
            {
                await js.InvokeVoidAsync("blazorCulture.set", defaultCulture);
            }

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            return host;
        }
    }
}
