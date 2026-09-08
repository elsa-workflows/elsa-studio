using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Options;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDesignerWrapper"/>'s switch on <see cref="DesignerOptions.UseReactFlow"/>:
/// while it is set, W9b's React Flow adapter for BPMN does not exist yet, so the wrapper must show a
/// clear notice rather than the X6 canvas -- and, per the issue, rather than the JSON fallback or a
/// crash. Otherwise it must render the X6 canvas.
/// </summary>
public sealed class BpmnDesignerWrapperTests : BunitContext, IAsyncLifetime
{
    public BpmnDesignerWrapperTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<BpmnDiagnostic[]>("loadBpmnDiagram", _ => true).SetResult([]);
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddWorkflowsCore();
        Services.AddWorkflowsDesigner();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry, NoOpActivityRegistry>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void RendersTheReactFlowNotice_WhenUseReactFlowIsSet()
    {
        Services.Configure<DesignerOptions>(o => o.UseReactFlow = true);

        var cut = Render<BpmnDesignerWrapper>(parameters => parameters.Add(p => p.Activity, CreateActivity()));

        Assert.Contains("mud-alert", cut.Markup);
        Assert.DoesNotContain("graph-container", cut.Markup);
    }

    [Fact]
    public void RendersTheX6Canvas_WhenUseReactFlowIsNotSet()
    {
        Services.Configure<DesignerOptions>(o => o.UseReactFlow = false);

        var cut = Render<BpmnDesignerWrapper>(parameters => parameters.Add(p => p.Activity, CreateActivity()));

        Assert.Contains("graph-container", cut.Markup);
        Assert.DoesNotContain("mud-alert", cut.Markup);
    }

    private static JsonObject CreateActivity() => new()
    {
        ["id"] = "root",
        ["type"] = "Elsa.BpmnProcess",
        ["activities"] = new JsonArray()
    };

    private class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class NoOpActivityRegistry : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => [];
        public ActivityDescriptor? Find(string activityType, int? version = default) => null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => [];
        public void MarkStale()
        {
        }
    }
}
