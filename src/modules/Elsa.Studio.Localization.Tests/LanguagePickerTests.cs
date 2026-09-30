using System.Globalization;
using Bunit;
using Elsa.Studio.Localization.Components;
using Elsa.Studio.Localization.Options;
using Elsa.Studio.Localization.Services;
using Elsa.Studio.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Localization.Tests;

public sealed class LanguagePickerTests : BunitContext, IAsyncLifetime
{
    public LanguagePickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ICultureService, StubCultureService>();
        // Few enough cultures that MudBlazor's flip logic would not move a menu that opens over its button.
        Services.Configure<LocalizationOptions>(options => options.SupportedCultures = ["en-US", "nl-NL"]);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void LanguagePicker_OpensBelowItsButton() => MenuPopoverAssert.OpensBelowItsButton<LanguagePicker>(this);

    private sealed class StubCultureService : ICultureService
    {
        public Task ChangeCultureAsync(CultureInfo culture) => Task.CompletedTask;
    }
}
