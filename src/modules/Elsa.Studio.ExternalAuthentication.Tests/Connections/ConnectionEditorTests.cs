using System.Net;
using System.Text.Json;
using Bunit;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Components.ConnectionEditor;
using Elsa.Studio.ExternalAuthentication.Menu;
using Elsa.Studio.ExternalAuthentication.Models;
using ConnectionIndex = Elsa.Studio.ExternalAuthentication.Pages.Connections.Index;
using ConnectionEdit = Elsa.Studio.ExternalAuthentication.Pages.Connections.Edit;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Connections;

public sealed class ConnectionEditorTests : BunitContext, IAsyncLifetime
{
    private readonly TestConnectionsApi _api = new();
    private readonly TestOperationsApi _operations = new();
    private readonly PermissionService _permissions = new(true);
    private readonly TestCustomEditorRegistry _customEditors = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;
    private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;

    public ConnectionEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true).SetVoidResult();
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true).SetVoidResult();
        JSInterop.Setup<int>("mudpopoverHelper.countProviders").SetResult(1);
        Services.AddSingleton<IBackendApiClientProvider>(new TestBackendApiClientProvider(_api, _operations));
        Services.AddSingleton<IExternalAuthenticationPermissionService>(_permissions);
        Services.AddSingleton<ICustomConnectionEditorRegistry>(_customEditors);
        _popoverProvider = Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();


    [Fact]
    public void ConfigurationOwnedConnection_IsClearlyReadOnly()
    {
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection(source: "configuration"))
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.ReadOnly, true));

        Assert.Contains("configuration-owned and read-only", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("override is unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Create full Studio override", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Save changes", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GenericEditor_ShowsDerivedCallbackReadOnlyAndDoesNotExposeTenantScope()
    {
        var connection = CreateConnection();
        connection.CallbackUri = "https://elsa.example.test/external-authentication/callback/connection-1";
        connection.PreviewCallbackUri = "https://elsa.example.test/external-authentication/previews/callback/record-1";
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation()));

        Assert.Contains("Provider callback URI", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(connection.CallbackUri, cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Provider preview callback URI", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(connection.PreviewCallbackUri, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Tenant ID", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Host-wide", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TagsArrayField_AllowsAddingAValueWhenNoOptionsAreProvided()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal)
        {
            ["scopes"] = System.Text.Json.JsonSerializer.SerializeToElement(Array.Empty<string>())
        };
        var cut = Render<DescriptorField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor
            {
                Name = "scopes",
                DisplayName = "Scopes",
                ValueType = "string-array",
                UiHint = "tags"
            })
            .Add(component => component.Settings, settings));

        cut.Find("input").Input("email");
        cut.FindAll("button").Single(button => button.TextContent.Contains("Add", StringComparison.Ordinal)).Click();

        Assert.Equal(["email"], settings["scopes"].EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task StringArrayField_WithAllowedValuesUsesMultiSelectAndPreservesArrayValues()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal);
        var field = new ConnectionFieldDescriptor
        {
            Name = "scopes",
            DisplayName = "Scopes",
            ValueType = "string-array",
            AllowedValues = ["profile", "email"]
        };
        var cut = Render<DescriptorField>(parameters => parameters
            .Add(component => component.Field, field)
            .Add(component => component.Settings, settings));

        var select = cut.FindComponent<MudSelect<string>>().Instance;
        Assert.True(select.MultiSelection);
        Assert.Equal(["profile", "email"], cut.FindComponents<MudSelectItem<string>>().Select(item => item.Instance.Value));

        await cut.InvokeAsync(() => select.SelectedValuesChanged.InvokeAsync(["profile", "email"]));

        Assert.Empty(DescriptorEditorState.Validate(field, settings));
        Assert.Equal(["profile", "email"], settings["scopes"].EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void PolicyEditor_UsesRoleOptionsAndFailsClosedWhenTheyAreUnavailable()
    {
        var descriptor = new UnlinkedIdentityPolicyDescriptor
        {
            Type = "create-user",
            DisplayName = "Create user",
            SettingsVersion = 1,
            Fields = [new ConnectionFieldDescriptor { Name = "defaultRoleIds", DisplayName = "Default roles", ValueType = "string-array" }]
        };
        var value = new PolicySelection
        {
            Type = "create-user",
            SettingsVersion = 1,
            Settings = System.Text.Json.JsonSerializer.SerializeToElement(new { defaultRoleIds = Array.Empty<string>() })
        };

        var cut = Render<ConnectionPolicyEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Descriptors, [descriptor])
            .Add(component => component.Roles, [new IdentityRoleOption { Id = "role-admin", Name = "Administrators" }])
            .Add(component => component.CanSelectRoles, true));

        var roleSelect = cut.FindComponents<MudSelect<string>>().Last().Instance;
        Assert.True(roleSelect.MultiSelection);
        Assert.False(roleSelect.Disabled);
        Assert.DoesNotContain("Enter role IDs", cut.Markup, StringComparison.OrdinalIgnoreCase);

        cut.Render(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Descriptors, [descriptor])
            .Add(component => component.RoleOptionsError, "Elsa roles could not be loaded. Default-role settings are read-only."));
        Assert.Contains("read-only", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.True(cut.FindComponents<MudSelect<string>>().Last().Instance.Disabled);
    }

    [Fact]
    public void ConnectionPage_LoadsRoleOptionsWithoutShowingASpuriousError()
    {
        var descriptor = new UnlinkedIdentityPolicyDescriptor
        {
            Type = "create-user",
            DisplayName = "Create user",
            SettingsVersion = 1,
            Fields = [new ConnectionFieldDescriptor { Name = "defaultRoleIds", DisplayName = "Default roles", ValueType = "string-array" }]
        };
        var connection = CreateConnection();
        connection.UnlinkedPolicy = new PolicySelection
        {
            Type = descriptor.Type,
            SettingsVersion = descriptor.SettingsVersion,
            Settings = JsonSerializer.SerializeToElement(new { defaultRoleIds = new[] { "role-admin" } })
        };
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        _api.Policies = [descriptor];
        _api.Roles = [new IdentityRoleOption { Id = "role-admin", Name = "Administrators" }];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("RoleOptionsError", cut.Markup, StringComparison.Ordinal);
            Assert.Contains(
                cut.FindComponent<ConnectionPolicyEditor>().Instance.Roles,
                role => role is { Id: "role-admin", Name: "Administrators" });
            Assert.False(cut.FindComponents<MudSelect<string>>().Last().Instance.Disabled);
        });
    }

    [Fact]
    public void ConnectionPage_ShowsTheActualManagedSecretResolverError()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter(includeSecret: true)];
        _api.ManagedSecretResolvers = [];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Managed secret storage is not available on this Elsa server.", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("ManagedSecretResolverError", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionPage_SeparatesManagementTasksIntoWorkspaceTabs()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        connection.EffectivelyEnabled = true;
        connection.Validity = "valid";
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                ["Settings", "Provisioning", "Diagnostics"],
                cut.FindComponents<MudTabPanel>().Select(panel => panel.Instance.Text));
            var tabs = cut.FindComponent<MudTabs>().Instance;
            Assert.True(tabs.KeepPanelsAlive);
            Assert.Equal("pa-4 pa-sm-6", tabs.TabPanelsClass);
            Assert.Equal(0, tabs.GetState(component => component.ActivePanelIndex));
            var tabPanels = cut.Find(".mud-tabs-panels");
            Assert.Contains("pa-4", tabPanels.ClassList);
            Assert.Contains("pa-sm-6", tabPanels.ClassList);
            Assert.NotNull(tabPanels.QuerySelector(".connection-workspace__configuration"));
            Assert.NotNull(tabPanels.QuerySelector(".connection-workspace__provisioning"));
            Assert.NotNull(tabPanels.QuerySelector(".connection-workspace__diagnostics"));
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Effective: Studio", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("OpenID Connect settings", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.Contains("At a glance", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.Contains("Open Diagnostics", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.Contains("User provisioning and linking", cut.Find(".connection-workspace__provisioning").TextContent, StringComparison.Ordinal);
            Assert.Contains("Operations", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
            Assert.Contains("Enabled", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Valid", cut.Markup, StringComparison.Ordinal);
        });

        cut.FindAll(".mud-tab").Single(tab => tab.TextContent.Contains("Provisioning", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex));
            Assert.Equal(1, cut.Find(".connection-workspace__provisioning").TextContent.Split("User provisioning and linking", StringSplitOptions.None).Length - 1);
            Assert.Contains("Review configuration", cut.Find(".connection-workspace__provisioning").TextContent, StringComparison.Ordinal);
        });
        cut.Find(".connection-workspace__provisioning")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Review configuration", StringComparison.Ordinal))
            .Click();
        cut.WaitForAssertion(() => Assert.Equal(0, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex)));

        cut.FindAll(".mud-tab").Single(tab => tab.TextContent.Contains("Diagnostics", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex)));
    }

    [Fact]
    public void NewConnection_UsesDraftWorkspaceWithoutDiagnostics()
    {
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                ["Settings", "Provisioning"],
                cut.FindComponents<MudTabPanel>().Select(panel => panel.Instance.Text));
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Create identity provider connection", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Draft", header.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Validate", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Provider protocol", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Diagnostics", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void NewConnection_InitializesDocumentedProviderDefaultsAndKeepsDiscoverySettingsVisible()
    {
        var adapter = new AdapterDescriptor
        {
            Type = "openid-connect",
            DisplayName = "OpenID Connect",
            Fields =
            [
                new ConnectionFieldDescriptor
                {
                    Name = "clientAuthenticationMethod",
                    DisplayName = "Client authentication",
                    AllowedValues = ["client_secret_basic", "client_secret_post"]
                },
                new ConnectionFieldDescriptor
                {
                    Name = "mode",
                    DisplayName = "Trust mode",
                    AllowedValues = ["discovery", "manual"]
                },
                new ConnectionFieldDescriptor
                {
                    Name = "discoveryUrl",
                    DisplayName = "Discovery URL",
                    IsRequired = true,
                    VisibleWhen = new ConnectionFieldVisibilityCondition { Field = "mode", ExpectedValue = "discovery" }
                }
            ]
        };
        _api.Adapters = [adapter];

        var cut = Render<ConnectionEdit>();

        cut.WaitForAssertion(() =>
        {
            var editor = cut.FindComponent<ConnectionEditor>().Instance;
            Assert.Equal("client_secret_basic", DescriptorEditorState.ToDisplayString(editor.Model.AdapterSettings["clientAuthenticationMethod"]));
            Assert.Equal("discovery", DescriptorEditorState.ToDisplayString(editor.Model.AdapterSettings["mode"]));
            Assert.False(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);
            Assert.Contains("Discovery URL", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.Contains("Client secret (basic authentication)", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.Contains("Discovery", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionEditor_PresentsFixedPkceAsStaticS256InsteadOfAnEmptyInput()
    {
        var adapter = new AdapterDescriptor
        {
            Type = "openid-connect",
            DisplayName = "OpenID Connect",
            Fields = [new ConnectionFieldDescriptor { Name = "providerPkce", DisplayName = "Provider PKCE", IsReadOnly = true }]
        };

        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, adapter)
            .Add(component => component.Model, CreateMutation()));

        var pkce = cut.Find(".connection-editor__fixed-pkce");
        Assert.Contains("Provider PKCE", pkce.TextContent, StringComparison.Ordinal);
        Assert.Contains("S256", pkce.TextContent, StringComparison.Ordinal);
        Assert.Empty(pkce.QuerySelectorAll("input"));
    }

    [Fact]
    public void ConnectionEditor_ReportsEditsForUnsavedChangeProtection()
    {
        var changes = 0;
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.Changed, EventCallback.Factory.Create(this, () => changes++)));

        cut.FindAll("input").First().Change("Updated connection");

        Assert.Equal(1, changes);
        Assert.Equal("Updated connection", cut.Instance.Model.DisplayName);
    }

    [Fact]
    public async Task ConnectionEditor_ReportsUnsafeTrustConfirmationForUnsavedChangeProtection()
    {
        var changes = 0;
        var mutation = CreateMutation();
        mutation.AdapterSettings["allowInsecureMetadata"] = System.Text.Json.JsonSerializer.SerializeToElement(true);
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter(includeUnsafe: true))
            .Add(component => component.Model, mutation)
            .Add(component => component.CanConfigureUnsafeSettings, true)
            .Add(component => component.Changed, EventCallback.Factory.Create(this, () => changes++)));

        var confirmation = cut.FindComponents<MudCheckBox<bool>>()
            .Single(component => component.Instance.Label?.Contains("explicitly confirm", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => confirmation.Instance.ValueChanged.InvokeAsync(true));

        Assert.True(mutation.ConfirmUnsafeSettings);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void SavedConnection_AllowsManagedSecretConfiguration()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter(includeSecret: true)];
        _api.ManagedSecretResolvers = [new ManagedSecretResolverDescriptor { Type = "elsa-secrets", DisplayName = "Elsa Secrets" }];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            var secrets = cut.Find(".connection-editor__secrets");
            Assert.Contains("Replace managed secret", secrets.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Save the connection draft", secrets.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConfigurationOwnedConnection_UsesReadOnlyWorkspaceContext()
    {
        var connection = CreateConnection("configuration");
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Effective: Deployment", header.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Validate", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("configuration-owned and read-only", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Save changes", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.Contains("Tests and previews are available under Diagnostics.", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("lifecycle actions are available", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
            Assert.Contains("Operations", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Lifecycle", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ValidateFromWorkspaceHeader_OpensDiagnosticsWithTheResult()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        _api.ValidationResult = new ConnectionValidationResult
        {
            Valid = false,
            Errors =
            [
                new ConnectionValidationMessage
                {
                    Field = "adapterSettings.authority",
                    Code = "invalid",
                    Message = "Authority could not be resolved."
                }
            ]
        };
        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() => Assert.Contains("Validate", cut.Find(".connection-workspace__header").TextContent, StringComparison.Ordinal));

        cut.Find(".connection-workspace__header")
            .QuerySelectorAll("button")
            .Single(button => button.TextContent.Contains("Validate", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(2, cut.FindComponent<MudTabs>().Instance.GetState(component => component.ActivePanelIndex));
            Assert.Contains("Authority could not be resolved.", cut.Find(".connection-workspace__diagnostics").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void MatchUserPolicy_UsesMatcherDescriptorsAndHidesRawMatcherJson()
    {
        var policy = new UnlinkedIdentityPolicyDescriptor
        {
            Type = "match-user",
            DisplayName = "Match an existing user",
            SettingsVersion = 1,
            Fields =
            [
                new ConnectionFieldDescriptor { Name = "matcher", DisplayName = "User matcher", ValueType = "json" },
                new ConnectionFieldDescriptor { Name = "noMatchAction", DisplayName = "No-match action", ValueType = "string" },
                new ConnectionFieldDescriptor { Name = "defaultRoleIds", DisplayName = "Default roles", ValueType = "string-array" }
            ]
        };
        var value = new PolicySelection
        {
            Type = "match-user",
            SettingsVersion = 1,
            Settings = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                matcher = new { type = "email", settingsVersion = 1, settings = new { claimType = "email" } },
                noMatchAction = "create-user",
                defaultRoleIds = Array.Empty<string>()
            })
        };
        var matcher = new ExternalUserMatcherDescriptor
        {
            Type = "email",
            DisplayName = "Email matcher",
            SettingsVersion = 1,
            RequiredClaimTypes = ["email"],
            Fields = [new ConnectionFieldDescriptor { Name = "claimType", DisplayName = "Email claim", ValueType = "string" }]
        };

        var cut = Render<ConnectionPolicyEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Descriptors, [policy])
            .Add(component => component.Matchers, [matcher])
            .Add(component => component.Roles, [new IdentityRoleOption { Id = "role-user", Name = "Users" }])
            .Add(component => component.CanSelectRoles, true));

        Assert.Contains("Required projected claims: email", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("When no user matches", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Versioned matcher selection and settings", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(4, cut.FindComponents<MudSelect<string>>().Count);

        cut.Render(parameters => parameters
            .Add(component => component.Value, null)
            .Add(component => component.Descriptors, [policy])
            .Add(component => component.Matchers, []));
        Assert.DoesNotContain("Match an existing user", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void FullStudioOverride_ClearsSecretBindingsAndSendsExplicitOverrideFlag()
    {
        var connection = CreateConnection("configuration");
        connection.CanCreateOverride = true;
        connection.AdapterSettings["authority"] = System.Text.Json.JsonSerializer.SerializeToElement("https://login.example.test");
        connection.SecretBindings["clientSecret"] = new SecretBindingState
        {
            IsConfigured = true,
            IsResolvable = true
        };
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter(includeSecret: true)];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() => Assert.Contains("Create full Studio override", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Create full Studio override", StringComparison.Ordinal)).Click();
        Assert.True(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);
        cut.FindAll("button").Single(x => x.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.NotNull(_api.CreatedRequest));

        Assert.True(_api.CreatedRequest!.OverridesConfigurationConnection);
        Assert.Equal("host", _api.CreatedRequest.Scope.Kind);
        Assert.DoesNotContain(typeof(ConnectionMutation).GetProperties(), property => property.Name == "SecretBindings");
    }

    [Fact]
    public void StudioOverride_ExplainsDisabledShadowingAndArchiveReveal()
    {
        var connection = CreateConnection();
        connection.OverridesConfigurationConnection = true;
        connection.AdapterSettings["authority"] = System.Text.Json.JsonSerializer.SerializeToElement("https://login.example.test");
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("keeps shadowing deployment configuration while disabled", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Archiving it reveals", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("restoring it resumes shadowing", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void ShadowedDatabaseConnection_PromotesTheExistingRecordOnlyWhenPermitted(bool canPromote, bool canUpdate, bool expectedAction)
    {
        _permissions.Allowed = canUpdate;
        var connection = CreateConnection();
        connection.Shadowed = true;
        connection.CanPromoteToConfigurationOverride = canPromote;
        connection.SecretBindings["clientSecret"] = new SecretBindingState { IsConfigured = true, IsResolvable = true };
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter(includeSecret: true)];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() =>
        {
            if (expectedAction)
                Assert.Contains("Make this Studio record effective", cut.Markup, StringComparison.Ordinal);
            else
                Assert.DoesNotContain("Make this Studio record effective", cut.Markup, StringComparison.Ordinal);
        });

        if (!expectedAction)
            return;

        cut.FindAll("button").Single(button => button.TextContent.Contains("Make this Studio record effective", StringComparison.Ordinal)).Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Make this Studio record effective?", _dialogProvider.Markup, StringComparison.Ordinal));
        Assert.Null(_api.UpdatedRequest);

        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains("Make record effective", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.NotNull(_api.UpdatedRequest));

        Assert.Equal(connection.Id, _api.UpdatedConnectionId);
        Assert.Equal("\"7\"", _api.UpdatedIfMatch);
        Assert.True(_api.UpdatedRequest!.OverridesConfigurationConnection);
        Assert.Null(_api.CreatedRequest);
        Assert.DoesNotContain(typeof(ConnectionMutation).GetProperties(), property => property.Name == "SecretBindings");
        Assert.True(connection.SecretBindings["clientSecret"].IsConfigured);
    }

    [Fact]
    public async Task ShadowedDatabaseConnection_PromotionRemainsAvailableWithALegacyCustomEditor()
    {
        var connection = CreateConnection();
        connection.Shadowed = true;
        connection.CanPromoteToConfigurationOverride = true;
        _api.GetResult = connection;
        var adapter = CreateAdapter();
        adapter.CustomEditor = new CustomEditorContract { Key = "legacy-editor", ContractVersion = 1 };
        _api.Adapters = [adapter];
        _customEditors.ComponentType = typeof(TestCustomEditor);

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Legacy custom editor", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Make this Studio record effective", cut.Markup, StringComparison.Ordinal);
        });

        var tabs = cut.FindComponent<MudTabs>();
        var customEditor = cut.FindComponent<TestCustomEditor>().Instance;
        await cut.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(1));
        await cut.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(0));

        Assert.Same(customEditor, cut.FindComponent<TestCustomEditor>().Instance);
    }

    [Fact]
    public async Task ChangeTrackingCustomEditor_MarksConnectionAsDirty()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        var adapter = CreateAdapter();
        adapter.CustomEditor = new CustomEditorContract { Key = "tracked-editor", ContractVersion = 1 };
        _api.Adapters = [adapter];
        _customEditors.ComponentType = typeof(ChangeTrackingCustomEditor);

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() => Assert.Contains("Change-tracking custom editor", cut.Markup, StringComparison.Ordinal));
        Assert.False(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);

        var editor = cut.FindComponent<ChangeTrackingCustomEditor>().Instance;
        await cut.InvokeAsync(() => editor.Changed.InvokeAsync());

        Assert.True(cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>().Instance.ConfirmExternalNavigation);
    }

    [Fact]
    public async Task Diagnostics_ShowsTheBackendManagementContractAndBuild()
    {
        var connection = CreateConnection();
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        _api.Runtime = new ExternalAuthenticationRuntimeDescriptor
        {
            ManagementContractVersion = 1,
            ProductVersion = "3.8.0",
            InformationalVersion = "3.8.0+abcdef"
        };

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        var tabs = cut.FindComponent<MudTabs>();
        await cut.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(2));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Management contract v1", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Backend 3.8.0", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("3.8.0+abcdef", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ShadowedConnection_ExplainsWhyThePersistedRecordIsNotEffective()
    {
        var connection = CreateConnection();
        connection.Shadowed = true;
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("shadowed by deployment configuration", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Effective: Deployment", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Stored: Disabled", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Stored: Not validated", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Stored record not tested", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Stored record validity", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ShadowedConfigurationConnection_ExplainsThatStudioIsEffective()
    {
        var connection = CreateConnection("configuration");
        connection.Shadowed = true;
        connection.EnabledIntent = true;
        connection.Validity = "valid";
        connection.CanCreateOverride = true;
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];
        _api.ListResults.Enqueue(new ListConnectionsResponse
        {
            Items =
            [
                new ConnectionSummary
                {
                    Id = "studio-override",
                    Key = connection.Key,
                    Source = "database",
                    Scope = connection.Scope
                }
            ]
        });

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("deployment-defined connection is shadowed by an effective Studio override", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("persisted record is shadowed by deployment configuration", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var header = cut.Find(".connection-workspace__header");
            Assert.Contains("Effective: Studio", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Deployment: Enabled", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Deployment: Valid", header.TextContent, StringComparison.Ordinal);
            Assert.Contains("Deployment record validity", cut.Find(".connection-workspace__configuration").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SecretBinding_ShowsConfiguredStateWithoutRevealingASecretValue()
    {
        var connection = CreateConnection();
        connection.SecretBindings["clientSecret"] = new SecretBindingState
        {
            IsConfigured = true,
            IsResolvable = true
        };

        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter(includeSecret: true))
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.ReadOnly, true));

        Assert.Contains("Configured and resolvable", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret-value", cut.Markup);
        Assert.DoesNotContain("contoso-client-secret", cut.Markup);
    }

    [Fact]
    public void EnabledConnection_CannotRemoveARequiredSecretBinding()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        connection.SecretBindings["clientSecret"] = new SecretBindingState { Ownership = "managed", IsConfigured = true, IsResolvable = true };
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter(includeSecret: true))
            .Add(component => component.Model, CreateMutation()));

        Assert.Contains("cannot be removed while the connection is enabled", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.True(cut.FindAll("button").Single(x => x.TextContent.Contains("Remove secret binding", StringComparison.Ordinal)).HasAttribute("disabled"));
    }

    [Fact]
    public void ManagedSecret_IsWriteOnlyAndExternalBindingsAreNotEditableInStudio()
    {
        ManagedSecretMutation? replaced = null;
        var cut = Render<SecretBindingField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor { Name = "clientSecret", DisplayName = "Client secret", IsSecretBinding = true })
            .Add(component => component.ManagedSecretResolvers,
            [
                new ManagedSecretResolverDescriptor { Type = "custom-vault", DisplayName = "Custom vault" },
                new ManagedSecretResolverDescriptor { Type = "elsa-secrets", DisplayName = "Elsa Secrets" }
            ])
            .Add(component => component.OnManagedReplace, EventCallback.Factory.Create<ManagedSecretMutation>(this, value => replaced = value)));

        Assert.Contains("Managed secret", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("External binding", cut.Markup, StringComparison.Ordinal);
        var password = cut.Find("input[type=password]");
        password.Change("top-secret-value");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Replace managed secret", StringComparison.Ordinal)).Click();

        Assert.NotNull(replaced);
        Assert.Equal("elsa-secrets", replaced!.ResolverType);
        Assert.Equal("top-secret-value", replaced.Value);
        Assert.DoesNotContain("top-secret-value", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ManagedSecretOption_IsHiddenWhenBackendCapabilityIsUnavailable()
    {
        var cut = Render<SecretBindingField>(parameters => parameters
            .Add(component => component.Field, new ConnectionFieldDescriptor { Name = "clientSecret", DisplayName = "Client secret", IsSecretBinding = true })
            .Add(component => component.ManagedSecretResolverError, "Managed secret storage is unavailable."));

        Assert.Contains("Managed secret storage is unavailable", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Replace managed secret", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("External binding", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SecretRemovalPrompt_DistinguishesManagedDeletionFromExternalReferenceRemoval()
    {
        var managed = SecretBindingRemovalPrompt.GetMessage(new SecretBindingState { Ownership = "managed" });
        var external = SecretBindingRemovalPrompt.GetMessage(new SecretBindingState { Ownership = "external" });

        Assert.Contains("secret value", managed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deleted", managed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reference", external, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not deleted", external, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FinalLoginPathGuard_IsDistinguishedFromOrdinaryConcurrencyConflict()
    {
        Assert.True(ConnectionManagementError.IsFinalLoginPathGuard(
            System.Net.HttpStatusCode.Conflict,
            """{"details":{"code":"final_login_path_guard"}}"""));
        Assert.False(ConnectionManagementError.IsFinalLoginPathGuard(
            System.Net.HttpStatusCode.Conflict,
            """{"error":"conflict"}"""));
    }

    [Fact]
    public void UnsafeConfirmation_IsOnlyShownWhenAnUnsafeSettingIsActive()
    {
        var model = CreateMutation();
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter(includeUnsafe: true))
            .Add(component => component.Model, model)
            .Add(component => component.CanConfigureUnsafeSettings, false));

        Assert.DoesNotContain("I understand and explicitly confirm", cut.Markup, StringComparison.Ordinal);

        DescriptorEditorState.SetBoolean(model.AdapterSettings, "allowInsecureMetadata", true);
        cut.Render();

        Assert.Contains("requires explicit confirmation", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not have permission", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DescriptorState_PreservesTypedJsonValuesAndEvaluatesVisibilityAndValidation()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal);
        DescriptorEditorState.SetBoolean(settings, "enabled", true);
        DescriptorEditorState.SetInteger(settings, "retries", 3);
        DescriptorEditorState.SetNumber(settings, "timeout", 1.5m);
        DescriptorEditorState.SetString(settings, "authority", "https://login.example.test");

        Assert.True(DescriptorEditorState.GetBoolean(settings, "enabled"));
        Assert.Equal(3, DescriptorEditorState.GetInteger(settings, "retries"));
        Assert.Equal(1.5m, DescriptorEditorState.GetNumber(settings, "timeout"));
        Assert.Equal("https://login.example.test", DescriptorEditorState.ToDisplayString(settings["authority"]));

        var conditional = new ConnectionFieldDescriptor { Name = "clientId", DisplayName = "Client ID", VisibleWhen = new ConnectionFieldVisibilityCondition { Field = "enabled", ExpectedValue = "true" } };
        Assert.True(DescriptorEditorState.IsVisible(conditional, settings));

        var constrained = new ConnectionFieldDescriptor
        {
            Name = "authority",
            DisplayName = "Authority",
            ValueType = "uri",
            IsRequired = true,
            Validation = new ConnectionFieldValidation { MaximumLength = 10, Pattern = "^https://" }
        };
        Assert.NotEmpty(DescriptorEditorState.Validate(constrained, settings));
    }

    [Fact]
    public void AllowedValues_PreserveTheDescriptorValueType()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement>(StringComparer.Ordinal);
        var integer = new ConnectionFieldDescriptor { Name = "retries", DisplayName = "Retries", ValueType = "integer" };
        var number = new ConnectionFieldDescriptor { Name = "timeout", DisplayName = "Timeout", ValueType = "number" };
        var boolean = new ConnectionFieldDescriptor { Name = "enabled", DisplayName = "Enabled", ValueType = "boolean" };

        Assert.True(DescriptorEditorState.TrySetAllowedValue(settings, integer, "3", out _));
        Assert.True(DescriptorEditorState.TrySetAllowedValue(settings, number, "1.5", out _));
        Assert.True(DescriptorEditorState.TrySetAllowedValue(settings, boolean, "true", out _));

        Assert.Equal(System.Text.Json.JsonValueKind.Number, settings["retries"].ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Number, settings["timeout"].ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.True, settings["enabled"].ValueKind);
    }

    [Fact]
    public void StructuredValues_RejectInvalidJsonAndNonStringArraysWithoutChangingThePriorValue()
    {
        var settings = new Dictionary<string, System.Text.Json.JsonElement> { ["claims"] = System.Text.Json.JsonSerializer.SerializeToElement(new[] { "name" }) };

        Assert.False(DescriptorEditorState.TrySetStructuredValue(settings, "claims", "{broken", true, out var invalidJson));
        Assert.Contains("valid JSON", invalidJson, StringComparison.OrdinalIgnoreCase);
        Assert.False(DescriptorEditorState.TrySetStructuredValue(settings, "claims", "[1]", true, out var wrongShape));
        Assert.Contains("only strings", wrongShape, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, settings["claims"].ValueKind);
        Assert.Equal("name", settings["claims"].EnumerateArray().Single().GetString());
    }

    [Fact]
    public void CustomEditorRegistry_UsesExactCompatibleContractAndOtherwiseFallsBack()
    {
        var registry = new CustomConnectionEditorRegistry([new CustomEditorRegistration("oidc-editor", 2, typeof(TestCustomEditor))]);

        Assert.True(registry.TryResolve(new CustomEditorContract { Key = "oidc-editor", ContractVersion = 2 }, out var selected));
        Assert.Equal(typeof(TestCustomEditor), selected);
        Assert.False(registry.TryResolve(new CustomEditorContract { Key = "oidc-editor", ContractVersion = 1 }, out _));
        Assert.False(registry.TryResolve(null, out _));
    }

    [Fact]
    public void CustomEditorRegistry_RejectsDuplicateContractsAndHelperRegistersTheEditor()
    {
        Assert.Throws<InvalidOperationException>(() => new CustomConnectionEditorRegistry(
        [
            new CustomEditorRegistration("oidc-editor", 1, typeof(TestCustomEditor)),
            new CustomEditorRegistration("oidc-editor", 1, typeof(TestCustomEditor))
        ]));

        var services = new ServiceCollection();
        services.AddExternalAuthenticationCustomEditor<TestCustomEditor>("oidc-editor", 1);
        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetServices<ICustomConnectionEditorRegistration>();
        var registry = new CustomConnectionEditorRegistry(registrations);
        Assert.True(registry.TryResolve(new CustomEditorContract { Key = "oidc-editor", ContractVersion = 1 }, out _));
    }

    [Fact]
    public void AdapterSelection_ResetsAdapterOwnedSettingsAndSecretBindingState()
    {
        var connection = CreateConnection();
        connection.AdapterSettings["oldSetting"] = System.Text.Json.JsonSerializer.SerializeToElement("old");
        connection.SecretBindings["oldSecret"] = new SecretBindingState { IsConfigured = true };
        var mutation = CreateMutation();
        mutation.AdapterSettings["oldSetting"] = System.Text.Json.JsonSerializer.SerializeToElement("old");
        mutation.ConfirmUnsafeSettings = true;
        var replacement = new AdapterDescriptor
        {
            Type = "custom-oauth",
            SettingsVersion = 4,
            Fields = [new ConnectionFieldDescriptor { Name = "enabled", DisplayName = "Enabled", ValueType = "boolean", DefaultValue = System.Text.Json.JsonSerializer.SerializeToElement(true) }]
        };

        ConnectionAdapterSelection.Apply(connection, mutation, replacement);

        Assert.Equal("custom-oauth", mutation.AdapterType);
        Assert.Equal(System.Text.Json.JsonValueKind.True, mutation.AdapterSettings["enabled"].ValueKind);
        Assert.Empty(connection.SecretBindings);
        Assert.DoesNotContain("oldSetting", mutation.AdapterSettings.Keys);
        Assert.False(mutation.ConfirmUnsafeSettings);
    }

    [Fact]
    public void ConnectionListPagingState_NavigatesBothDirectionsAndResetsToTheFirstPage()
    {
        var paging = new ConnectionListPagingState();

        paging.Replace("cursor-2");
        Assert.Equal(1, paging.PageNumber);
        Assert.False(paging.HasPrevious);
        Assert.True(paging.HasNext);
        Assert.True(paging.TryAdvance(out var secondPageCursor));
        Assert.Equal("cursor-2", secondPageCursor);
        Assert.Equal(2, paging.PageNumber);
        paging.SetNext("cursor-3");

        Assert.True(paging.TryGoBack(out var firstPageCursor));
        Assert.Null(firstPageCursor);
        Assert.Equal(1, paging.PageNumber);
        Assert.True(paging.HasNext);
        Assert.True(paging.TryAdvance(out var restoredSecondPageCursor));
        Assert.Equal("cursor-2", restoredSecondPageCursor);
        Assert.Equal(2, paging.PageNumber);
        Assert.True(paging.HasNext);

        paging.Replace("fresh-cursor-2");
        Assert.Equal(1, paging.PageNumber);
        Assert.False(paging.HasPrevious);
        Assert.Equal("fresh-cursor-2", paging.NextCursor);
    }

    [Fact]
    public void ConnectionListRequestState_RejectsAnOutOfOrderResponse()
    {
        var requests = new ConnectionListRequestState();

        var initialRequest = requests.Begin();
        var filteredRequest = requests.Begin();

        Assert.False(requests.IsCurrent(initialRequest));
        Assert.True(requests.IsCurrent(filteredRequest));
    }

    [Fact]
    public void ConnectionListAndLifecycleAffordances_RespectCursorOwnershipAndPermissions()
    {
        var paging = new ConnectionListPagingState();
        paging.Replace("cursor-2");
        Assert.True(paging.TryAdvance(out var cursor));
        Assert.Equal("cursor-2", cursor);
        paging.SetNext(null);
        Assert.False(paging.TryAdvance(out _));

        var configuration = CreateConnection("configuration");
        var database = CreateConnection();
        database.Validity = "valid";
        Assert.False(ConnectionActionAvailability.CanEnableOrDisable(configuration, true));
        Assert.False(ConnectionActionAvailability.CanArchiveOrRestore(configuration, true));
        Assert.True(ConnectionActionAvailability.CanEnableOrDisable(database, true));
        Assert.True(ConnectionActionAvailability.CanArchiveOrRestore(database, true));
        database.Validity = "invalid";
        Assert.False(ConnectionActionAvailability.CanEnableOrDisable(database, true));
        database.Validity = "valid";
        database.Archived = true;
        Assert.False(ConnectionActionAvailability.CanEnableOrDisable(database, true));
        Assert.Equal("\"17\"", ConnectionConcurrency.ToIfMatch(17));
        Assert.True(ConnectionConcurrency.IsConflict(System.Net.HttpStatusCode.PreconditionFailed));
        var changedElsewhere = new ManagementApiException("changed", 412, CreateConnection());
        Assert.True(ConnectionConflictRecovery.TryGetCurrent(changedElsewhere, out var recovered));
        Assert.Equal("connection-1", recovered.Id);
    }

    [Fact]
    public void GenericEditor_PreventsSaveAndDisplaysDescriptorValidationErrors()
    {
        var saved = false;
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.Saved, EventCallback.Factory.Create<ConnectionMutation>(this, _ => saved = true)));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        Assert.False(saved);
        Assert.Contains("Authority is required", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GenericEditor_BlocksSavingAfterInvalidJsonInput()
    {
        var saved = false;
        var mutation = CreateMutation();
        mutation.AdapterSettings["projection"] = System.Text.Json.JsonSerializer.SerializeToElement(new[] { "name" });
        var adapter = new AdapterDescriptor { Type = "custom", DisplayName = "Custom", Fields = [new ConnectionFieldDescriptor { Name = "projection", DisplayName = "Claim projection", ValueType = "json" }] };
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, adapter)
            .Add(component => component.Model, mutation)
            .Add(component => component.Saved, EventCallback.Factory.Create<ConnectionMutation>(this, _ => saved = true)));

        cut.Find("textarea").Change("{broken");
        cut.FindAll("button").Single(button => button.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        Assert.False(saved);
        Assert.Contains("valid JSON", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateShowsStructuredServerValidationDetails()
    {
        var adapter = CreateAdapter();
        adapter.Fields.First().DefaultValue = JsonSerializer.SerializeToElement("https://issuer.example.test");
        _api.Adapters = [adapter];
        _api.CreateException = await CreateApiExceptionAsync(
            HttpStatusCode.BadRequest,
            """
            {
              "error": "validation_failed",
              "message": "The connection is not valid for this operation.",
              "details": {
                "errors": [
                  {
                    "field": "adapterSettings.authority",
                    "code": "invalid",
                    "message": "Authority must use HTTPS."
                  }
                ],
                "warnings": []
              }
            }
            """);

        var cut = Render<ConnectionEdit>();
        cut.WaitForAssertion(() => Assert.Contains("Create identity provider connection", cut.Markup, StringComparison.Ordinal));
        ChangeField("Display name", "Contoso");
        ChangeField("Connection key", "contoso");
        cut.FindAll("button").Single(button => button.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        var snackbar = Services.GetRequiredService<ISnackbar>();
        cut.WaitForAssertion(() =>
        {
            var message = Assert.Single(snackbar.ShownSnackbars).Message;
            Assert.Contains("The connection is not valid for this operation.", message, StringComparison.Ordinal);
            Assert.Contains("adapterSettings.authority: Authority must use HTTPS.", message, StringComparison.Ordinal);
            Assert.DoesNotContain("Response status code does not indicate success", message, StringComparison.Ordinal);
        });

        void ChangeField(string label, string value)
        {
            var fieldLabel = cut.FindAll("label").Single(element => element.TextContent.Contains(label, StringComparison.Ordinal));
            cut.Find($"#{fieldLabel.GetAttribute("for")}").Change(value);
        }
    }

    [Fact]
    public void ConnectionList_UsesTheServerCursorForTheNextPage()
    {
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-2" });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [new ConnectionSummary { Id = "connection-2", Key = "next", DisplayName = "Next", AdapterType = "custom" }], NextCursor = null });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains("Contoso", cut.Markup));
        Assert.Contains("Page 1", cut.Markup, StringComparison.Ordinal);
        var previousPage = cut.FindAll("button").Single(button => button.TextContent.Contains("Previous page", StringComparison.Ordinal));
        Assert.True(previousPage.HasAttribute("disabled"));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Next page", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Next", cut.Markup);
            Assert.Contains("Page 2", cut.Markup, StringComparison.Ordinal);
            Assert.False(cut.FindAll("button").Single(button => button.TextContent.Contains("Previous page", StringComparison.Ordinal)).HasAttribute("disabled"));
        });

        Assert.Equal([null, "cursor-2"], _api.Cursors);
    }

    [Fact]
    public void ConnectionList_ExposesLabeledFiltersStatusAndNamedActions()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        connection.EffectivelyEnabled = true;
        connection.Validity = "valid";
        connection.OverridesConfigurationConnection = true;
        connection.IsPreferred = true;
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Identity provider connections", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Search connections", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Include archived", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Ownership", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Availability", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Studio", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Overrides deployment", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Available", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Enabled · Valid", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Preferred", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(
                cut.FindComponents<MudChip<string>>(),
                chip => chip.Markup.Contains("Preferred", StringComparison.Ordinal));
            Assert.NotEmpty(cut.FindAll("[aria-label=\"Actions\"]"));
            var actions = Assert.Single(cut.FindComponents<MudMenu>());
            Assert.Equal($"Actions for {connection.DisplayName}", actions.Instance.AriaLabel);
        });

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        _popoverProvider.WaitForAssertion(() =>
        {
            Assert.Contains("Manage", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("Disable", _popoverProvider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ConnectionList_PresentsLatestTestAsASemanticStatusAndSecondarySummary()
    {
        const string summary = "Provider metadata was resolved.";
        var connection = CreateConnection();
        connection.LatestObservation = new ConnectionObservation
        {
            Status = "succeeded",
            Summary = summary,
            IsStale = true
        };
        var untestedConnection = CreateConnection();
        untestedConnection.Id = "connection-2";
        untestedConnection.DisplayName = "Untested";
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection, untestedConnection] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            var status = cut.FindComponents<MudChip<string>>()
                .Single(chip => chip.Markup.Contains("Succeeded · Stale", StringComparison.Ordinal));
            Assert.Equal(Color.Warning, status.Instance.Color);
            Assert.Equal(Icons.Material.Outlined.ReportProblem, status.Instance.Icon);

            var summaryText = cut.FindComponents<MudText>()
                .Single(text => text.Markup.Contains(summary, StringComparison.Ordinal));
            Assert.Equal(Color.Secondary, summaryText.Instance.Color);

            var notTested = cut.FindComponents<MudText>()
                .Single(text => text.Markup.Contains("Not tested", StringComparison.Ordinal));
            Assert.Equal(Color.Secondary, notTested.Instance.Color);
            Assert.DoesNotContain(
                cut.FindComponents<MudChip<string>>(),
                chip => chip.Markup.Contains("Not tested", StringComparison.Ordinal));
        });
    }

    [Theory]
    [InlineData("database", false, true, "Studio", "Overrides deployment")]
    [InlineData("configuration", true, false, "Deployment", "Shadowed by Studio")]
    [InlineData("database", true, false, "Studio", "Shadowed by deployment")]
    [InlineData("database", false, false, "Studio", null)]
    public void ConnectionListPresentation_DescribesOwnershipRelationships(
        string source,
        bool shadowed,
        bool overridesDeployment,
        string expectedOwnership,
        string? expectedRelationship)
    {
        var connection = CreateConnection(source);
        connection.Shadowed = shadowed;
        connection.OverridesConfigurationConnection = overridesDeployment;

        Assert.Equal(expectedOwnership, ConnectionListPresentation.OwnershipLabel(connection));
        Assert.Equal(expectedRelationship, ConnectionListPresentation.OwnershipRelationship(connection));
    }

    [Theory]
    [InlineData(false, false, true, true, "valid", "Available", Color.Success)]
    [InlineData(false, false, true, false, "invalid", "Needs attention", Color.Error)]
    [InlineData(false, false, false, false, "valid", "Disabled", Color.Default)]
    [InlineData(true, false, true, true, "valid", "Archived", Color.Default)]
    [InlineData(false, true, true, false, "valid", "Shadowed", Color.Warning)]
    public void ConnectionListPresentation_SummarizesAvailability(
        bool archived,
        bool shadowed,
        bool enabledIntent,
        bool effectivelyEnabled,
        string validity,
        string expectedLabel,
        Color expectedColor)
    {
        var connection = CreateConnection();
        connection.Archived = archived;
        connection.Shadowed = shadowed;
        connection.EnabledIntent = enabledIntent;
        connection.EffectivelyEnabled = effectivelyEnabled;
        connection.Validity = validity;

        Assert.Equal(expectedLabel, ConnectionListPresentation.AvailabilityLabel(connection));
        Assert.Equal(expectedColor, ConnectionListPresentation.AvailabilityColor(connection));
        Assert.False(string.IsNullOrWhiteSpace(ConnectionListPresentation.AvailabilityIcon(connection)));
        Assert.Contains(
            $"{ConnectionStatusPresentation.LifecycleLabel(connection)} · {ConnectionStatusPresentation.ValidityLabel(connection.Validity)}",
            ConnectionListPresentation.AvailabilityDetailLabel(connection),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectionList_DoesNotApplyAnOlderFilterResponseAfterANewerOneCompletes()
    {
        var initial = new TaskCompletionSource<ListConnectionsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var filtered = new TaskCompletionSource<ListConnectionsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _api.PendingListResults.Enqueue(initial.Task);
        _api.PendingListResults.Enqueue(filtered.Task);

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Single(_api.ListRequests));

        var source = cut.FindComponent<MudSelect<string>>().Instance;
        var filterTask = cut.InvokeAsync(async () => await source.ValueChanged.InvokeAsync("database"));
        cut.WaitForAssertion(() => Assert.Equal(2, _api.ListRequests.Count));

        filtered.SetResult(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "new", Key = "new", DisplayName = "Newest result", AdapterType = "custom" }]
        });
        await filterTask;
        cut.WaitForAssertion(() => Assert.Contains("Newest result", cut.Markup, StringComparison.Ordinal));

        initial.SetResult(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "old", Key = "old", DisplayName = "Stale result", AdapterType = "custom" }]
        });
        cut.WaitForAssertion(() => Assert.DoesNotContain("Stale result", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConnectionList_ReloadsAndResetsPagingWhenFiltersChange()
    {
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-2" });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-3" });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = null });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = null });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = null });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Single(_api.ListRequests));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Next page", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Equal(2, _api.ListRequests.Count));
        Assert.Contains("Page 2", cut.Markup, StringComparison.Ordinal);

        var source = cut.FindComponent<MudSelect<string>>().Instance;
        await cut.InvokeAsync(() => source.ValueChanged.InvokeAsync("database"));
        cut.WaitForAssertion(() => Assert.Equal("database", _api.ListRequests[2].Source));
        Assert.Null(_api.ListRequests[2].Cursor);
        Assert.Contains("Page 1", cut.Markup, StringComparison.Ordinal);

        var includeArchived = cut.FindComponent<MudCheckBox<bool>>().Instance;
        await cut.InvokeAsync(() => includeArchived.ValueChanged.InvokeAsync(true));
        cut.WaitForAssertion(() => Assert.Null(_api.ListRequests[3].Archived));
        Assert.Null(_api.ListRequests[3].Cursor);

        var search = cut.FindComponent<MudTextField<string>>().Instance;
        await cut.InvokeAsync(() => search.ValueChanged.InvokeAsync("contoso"));
        cut.WaitForAssertion(() => Assert.Equal("contoso", _api.ListRequests[4].Search));
        Assert.Null(_api.ListRequests[4].Cursor);
    }

    [Fact]
    public void ConnectionList_LabelsShadowedDatabaseStateAsStoredAndExplainsOverrideAvailability()
    {
        var connection = CreateConnection();
        connection.Shadowed = true;
        connection.EnabledIntent = true;
        connection.Validity = "valid";
        var configuration = CreateConnection("configuration");
        configuration.CanCreateOverride = true;
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection, configuration] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Studio", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Shadowed by deployment", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Shadowed", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Stored: Enabled · Valid", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("create or promote a Studio record", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("can only be changed through deployment configuration", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ConfigurationConnection_OffersManagementOfAnExistingStudioOverrideInsteadOfACopy()
    {
        var configuration = CreateConnection("configuration");
        configuration.CanCreateOverride = true;
        _api.GetResult = configuration;
        _api.Adapters = [CreateAdapter()];
        _api.ListResults.Enqueue(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary
            {
                Id = "stored-override",
                Key = configuration.Key,
                Source = "database",
                AdapterType = configuration.AdapterType,
                DisplayName = "Stored override",
                Scope = new ConnectionScope { Kind = "host" },
                Shadowed = true
            }]
        });

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, configuration.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Manage existing Studio record", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Create full Studio override", cut.Markup, StringComparison.Ordinal);
        });

        cut.FindAll("button").Single(button => button.TextContent.Contains("Manage existing Studio record", StringComparison.Ordinal)).Click();
        Assert.EndsWith("/security/external-authentication/connections/stored-override", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigurationConnection_OffersRestorationOfAnArchivedStudioOverrideInsteadOfACopy()
    {
        var configuration = CreateConnection("configuration");
        configuration.CanCreateOverride = true;
        _api.GetResult = configuration;
        _api.Adapters = [CreateAdapter()];
        _api.ListResults.Enqueue(new ListConnectionsResponse
        {
            Items =
            [
                new ConnectionSummary
                {
                    Id = "archived-override",
                    Key = configuration.Key,
                    Source = "database",
                    AdapterType = configuration.AdapterType,
                    DisplayName = "Archived override",
                    Scope = new ConnectionScope { Kind = "host" },
                    Archived = true,
                    Shadowed = true
                }
            ]
        });

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, configuration.Id));

        cut.WaitForAssertion(() =>
        {
            Assert.Null(_api.ListRequests.Single().Archived);
            Assert.Contains("archived Studio record already exists", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Review and restore Studio record", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Create full Studio override", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData("Disable and revoke sessions", true)]
    [InlineData("Disable only", false)]
    public void ConnectionList_RequiresAnExplicitSessionDecisionWhenConfirmingDisable(string action, bool revokeActiveSessions)
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.DoesNotContain("Revoke active sessions when disabling", cut.Markup, StringComparison.Ordinal));

        cut.Find($"button[aria-label=\"Actions for {connection.DisplayName}\"]").Click();
        var disable = _popoverProvider.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Contains("Disable", StringComparison.Ordinal));
        disable.Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains(action, _dialogProvider.Markup, StringComparison.Ordinal));
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains(action, StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Equal(revokeActiveSessions, _operations.RevokeActiveSessions));
    }

    [Fact]
    public void ConnectionEditor_OffersSessionRevocationWhenConfirmingDisable()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        _api.GetResult = connection;
        _api.Adapters = [CreateAdapter()];

        var cut = Render<ConnectionEdit>(parameters => parameters.Add(component => component.ConnectionId, connection.Id));
        cut.WaitForAssertion(() => Assert.Contains("Disable", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Disable").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Disable and revoke sessions", _dialogProvider.Markup, StringComparison.Ordinal));
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains("Disable and revoke sessions", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.True(_operations.RevokeActiveSessions));
    }

    [Fact]
    public async Task SecurityMenu_PlacesIdentityProviderConnectionsFirstWhenReadIsAllowed()
    {
        var menu = new ExternalAuthenticationSecurityMenuContributor(
            new FeatureProvider(true),
            new PermissionSetService(ExternalAuthenticationPermissions.Read));

        var item = Assert.Single(await menu.GetMenuItemsAsync());
        Assert.Equal("security/external-authentication/connections", item.Href);
        Assert.Equal("Identity provider connections", item.Text);
        Assert.Equal(100, item.Order);

        var hidden = await new ExternalAuthenticationSecurityMenuContributor(new FeatureProvider(true), new PermissionService(false)).GetMenuItemsAsync();
        Assert.Empty(hidden);
    }

    [Fact]
    public async Task SecurityMenu_PreservesPermissionGatesAndOrdersIdentityAndAccessItems()
    {
        var menu = new ExternalAuthenticationSecurityMenuContributor(
            new FeatureProvider(true),
            new PermissionSetService(
                ExternalAuthenticationPermissions.Read,
                ExternalAuthenticationPermissions.ManageLinks,
                ExternalAuthenticationPermissions.SessionsRead));

        var items = (await menu.GetMenuItemsAsync()).ToList();
        Assert.Equal(
            ["Identity provider connections", "External identity links", "Authentication sessions"],
            items.Select(x => x.Text));
        Assert.Equal([100f, 200f, 300f], items.Select(x => x.Order));

        var sessionsOnly = await new ExternalAuthenticationSecurityMenuContributor(
                new FeatureProvider(true),
                new PermissionSetService(ExternalAuthenticationPermissions.SessionsRead))
            .GetMenuItemsAsync();
        Assert.Equal("Authentication sessions", Assert.Single(sessionsOnly).Text);
    }

    [Fact]
    public void ConnectionPages_ExposeOnlyCanonicalRoutes()
    {
        Assert.Equal(
            ["/security/external-authentication/connections"],
            RoutesFor<ConnectionIndex>());
        Assert.Equal(
            [
                "/security/external-authentication/connections/new",
                "/security/external-authentication/connections/{ConnectionId}"
            ],
            RoutesFor<ConnectionEdit>());
    }

    private static string[] RoutesFor<T>() =>
        typeof(T)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(x => x.Template)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static async Task<Refit.ApiException> CreateApiExceptionAsync(HttpStatusCode statusCode, string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://elsa.example.test/external-authentication/connections");
        using var response = new HttpResponseMessage(statusCode)
        {
            RequestMessage = request,
            Content = new StringContent(content)
        };
        return await Refit.ApiException.Create(request, HttpMethod.Post, response, new Refit.RefitSettings());
    }

    private static ConnectionDetail CreateConnection(string source = "database") => new()
    {
        Id = "connection-1",
        Source = source,
        DisplayName = "Contoso",
        Key = "contoso",
        AdapterType = "openid-connect",
        Scope = new ConnectionScope { Kind = "host" },
        Revision = 7
    };

    private static AdapterDescriptor CreateAdapter(bool includeSecret = false, bool includeUnsafe = false)
    {
        var descriptor = new AdapterDescriptor
        {
            Type = "openid-connect",
            DisplayName = "OpenID Connect",
            Fields = [new ConnectionFieldDescriptor { Name = "authority", DisplayName = "Authority", IsRequired = true }]
        };

        if (includeSecret)
            descriptor.Fields.Add(new ConnectionFieldDescriptor { Name = "clientSecret", DisplayName = "Client secret", IsSecretBinding = true, IsRequired = true });
        if (includeUnsafe)
            descriptor.Fields.Add(new ConnectionFieldDescriptor { Name = "allowInsecureMetadata", DisplayName = "Allow insecure metadata", IsUnsafe = true });
        return descriptor;
    }

    private static ConnectionMutation CreateMutation() => new()
    {
        Key = "contoso",
        AdapterType = "openid-connect",
        DisplayName = "Contoso"
    };

    private sealed class FeatureProvider(bool enabled) : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(enabled && featureName == Feature.RemoteFeatureName);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private sealed class PermissionService(bool allowed) : IExternalAuthenticationPermissionService
    {
        public bool Allowed { get; set; } = allowed;

        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) => ValueTask.FromResult(Allowed);
        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlySet<string>>(Allowed ? new HashSet<string>(["*"], StringComparer.Ordinal) : new HashSet<string>(StringComparer.Ordinal));
    }

    private sealed class PermissionSetService(params string[] permissions) : IExternalAuthenticationPermissionService
    {
        private readonly IReadOnlySet<string> _permissions = permissions.ToHashSet(StringComparer.Ordinal);

        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_permissions.Contains(permission));

        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_permissions);
    }

    private sealed class CustomEditorRegistration(string key, int contractVersion, Type componentType) : ICustomConnectionEditorRegistration
    {
        public string Key => key;
        public int ContractVersion => contractVersion;
        public Type ComponentType => componentType;
    }

    private sealed class TestCustomEditor : ComponentBase, IConnectionCustomEditor
    {
        [Parameter] public ConnectionDetail Connection { get; set; } = default!;
        [Parameter] public AdapterDescriptor Adapter { get; set; } = default!;
        [Parameter] public ConnectionMutation Model { get; set; } = default!;
        [Parameter] public bool ReadOnly { get; set; }
        [Parameter] public bool CanConfigureUnsafeSettings { get; set; }
        [Parameter] public bool CanCreateOverride { get; set; }
        [Parameter] public ICollection<ManagedSecretResolverDescriptor> ManagedSecretResolvers { get; set; } = [];
        [Parameter] public string? ManagedSecretResolverError { get; set; }
        [Parameter] public EventCallback<ConnectionMutation> Saved { get; set; }
        [Parameter] public EventCallback<(string Field, ManagedSecretMutation Secret)> ManagedSecretChanged { get; set; }
        [Parameter] public EventCallback<string> SecretBindingRemoved { get; set; }
        [Parameter] public EventCallback FullOverrideRequested { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Legacy custom editor");
    }

    private sealed class ChangeTrackingCustomEditor : ComponentBase, IConnectionCustomEditorWithChangeTracking
    {
        [Parameter] public ConnectionDetail Connection { get; set; } = default!;
        [Parameter] public AdapterDescriptor Adapter { get; set; } = default!;
        [Parameter] public ConnectionMutation Model { get; set; } = default!;
        [Parameter] public bool ReadOnly { get; set; }
        [Parameter] public bool CanConfigureUnsafeSettings { get; set; }
        [Parameter] public bool CanCreateOverride { get; set; }
        [Parameter] public ICollection<ManagedSecretResolverDescriptor> ManagedSecretResolvers { get; set; } = [];
        [Parameter] public string? ManagedSecretResolverError { get; set; }
        [Parameter] public EventCallback<ConnectionMutation> Saved { get; set; }
        [Parameter] public EventCallback<(string Field, ManagedSecretMutation Secret)> ManagedSecretChanged { get; set; }
        [Parameter] public EventCallback<string> SecretBindingRemoved { get; set; }
        [Parameter] public EventCallback FullOverrideRequested { get; set; }
        [Parameter] public EventCallback Changed { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Change-tracking custom editor");
    }

    private sealed class TestCustomEditorRegistry : ICustomConnectionEditorRegistry
    {
        public Type? ComponentType { get; set; }

        public bool TryResolve(CustomEditorContract? contract, out Type componentType)
        {
            componentType = ComponentType!;
            return contract is not null && ComponentType is not null;
        }
    }

    private sealed class TestBackendApiClientProvider(
        IExternalAuthenticationConnectionsApi connectionsApi,
        IExternalAuthenticationOperationsApi operationsApi) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/elsa/api/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult(typeof(T) == typeof(IExternalAuthenticationOperationsApi)
                ? (T)(object)operationsApi
                : (T)(object)connectionsApi);
    }

    private sealed class TestOperationsApi : IExternalAuthenticationOperationsApi
    {
        public bool? RevokeActiveSessions { get; private set; }

        public Task DisableWithRecoveryOverrideAsync(
            string connectionId,
            string ifMatch,
            bool confirmFinalLoginPathOverride,
            bool revokeActiveSessions = false,
            CancellationToken cancellationToken = default)
        {
            RevokeActiveSessions = revokeActiveSessions;
            return Task.CompletedTask;
        }

        public Task<ConnectionTestResult> TestAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PreviewInitiation> InitiatePreviewAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PreviewResultDocument> GetPreviewResultAsync(string previewHandle, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ListExternalAuthenticationSessionsResponse> ListSessionsAsync(string? userId = null, string? connectionId = null, string? status = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RevokeSessionAsync(string sessionId, RevokeExternalAuthenticationSessionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestConnectionsApi : IExternalAuthenticationConnectionsApi, IIdentityRolesApi
    {
        public Queue<ListConnectionsResponse> ListResults { get; } = new();
        public Queue<Task<ListConnectionsResponse>> PendingListResults { get; } = new();
        public List<ListRequest> ListRequests { get; } = [];
        public List<string?> Cursors { get; } = [];
        public ConnectionDetail? GetResult { get; set; }
        public ExternalAuthenticationRuntimeDescriptor? Runtime { get; set; } = new()
        {
            ManagementContractVersion = 1,
            ProductVersion = "test",
            InformationalVersion = "test"
        };
        public ICollection<AdapterDescriptor> Adapters { get; set; } = [];
        public ICollection<UnlinkedIdentityPolicyDescriptor> Policies { get; set; } = [];
        public ICollection<IdentityRoleOption> Roles { get; set; } = [];
        public ICollection<ManagedSecretResolverDescriptor> ManagedSecretResolvers { get; set; } =
            [new() { Type = "elsa-secrets", DisplayName = "Elsa Secrets" }];
        public ConnectionValidationResult ValidationResult { get; set; } = new() { Valid = true };
        public Exception? CreateException { get; set; }
        public ConnectionMutation? CreatedRequest { get; private set; }
        public string? UpdatedConnectionId { get; private set; }
        public ConnectionMutation? UpdatedRequest { get; private set; }
        public string? UpdatedIfMatch { get; private set; }

        public Task<ListConnectionsResponse> ListAsync(string? search = null, string? source = null, string? scope = null, string? adapterType = null, bool? enabled = null, bool? valid = null, bool? shadowed = null, bool? archived = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            Cursors.Add(cursor);
            ListRequests.Add(new ListRequest(search, source, archived, cursor));
            if (PendingListResults.TryDequeue(out var pending))
                return pending;

            return Task.FromResult(ListResults.Dequeue());
        }

        public sealed record ListRequest(string? Search, string? Source, bool? Archived, string? Cursor);

        public Task<ConnectionDetail> GetAsync(string connectionId, CancellationToken cancellationToken = default) => Task.FromResult(GetResult ?? throw new NotSupportedException());
        public Task<ExternalAuthenticationRuntimeDescriptor> GetRuntimeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Runtime ?? throw new NotSupportedException());
        public Task<ICollection<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default) => Task.FromResult(Adapters);
        public Task<ICollection<PermissionGrantSourceDescriptor>> GetPermissionSourcesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<UnlinkedIdentityPolicyDescriptor>> GetPoliciesAsync(CancellationToken cancellationToken = default) => Task.FromResult(Policies);
        public Task<ICollection<ExternalUserMatcherDescriptor>> GetUserMatchersAsync(CancellationToken cancellationToken = default) => Task.FromResult<ICollection<ExternalUserMatcherDescriptor>>([]);
        public Task<ManagedSecretResolverCatalog> GetManagedSecretResolversAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ManagedSecretResolverCatalog { Items = ManagedSecretResolvers });
        public Task<ICollection<PermissionDescriptor>> GetPermissionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> CreateAsync(ConnectionMutation request, CancellationToken cancellationToken = default)
        {
            CreatedRequest = request;
            if (CreateException is not null)
                return Task.FromException<ConnectionDetail>(CreateException);
            return Task.FromResult(new ConnectionDetail { Id = "override-1", Key = request.Key, DisplayName = request.DisplayName, AdapterType = request.AdapterType });
        }
        public Task<ConnectionDetail> UpdateAsync(string connectionId, ConnectionMutation request, string ifMatch, CancellationToken cancellationToken = default)
        {
            UpdatedConnectionId = connectionId;
            UpdatedRequest = request;
            UpdatedIfMatch = ifMatch;
            if (GetResult is null)
                throw new NotSupportedException();

            GetResult.OverridesConfigurationConnection = request.OverridesConfigurationConnection;
            GetResult.Shadowed = false;
            return Task.FromResult(GetResult);
        }
        public Task EnableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DisableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ArchiveAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RestoreAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionValidationResult> ValidateAsync(string connectionId, CancellationToken cancellationToken = default) => Task.FromResult(ValidationResult);
        public Task<ConnectionDetail> ReplaceManagedSecretAsync(string connectionId, string fieldName, ManagedSecretMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveSecretBindingAsync(string connectionId, string fieldName, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        Task<IdentityRoleOptionsResponse> IIdentityRolesApi.ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new IdentityRoleOptionsResponse { Roles = Roles });
    }
}
