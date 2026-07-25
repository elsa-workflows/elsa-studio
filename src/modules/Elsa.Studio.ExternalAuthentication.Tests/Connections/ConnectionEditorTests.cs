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
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Connections;

public sealed class ConnectionEditorTests : BunitContext, IAsyncLifetime
{
    private readonly TestConnectionsApi _api = new();

    public ConnectionEditorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true).SetVoidResult();
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true).SetVoidResult();
        JSInterop.Setup<int>("mudpopoverHelper.countProviders").SetResult(1);
        Services.AddSingleton<IBackendApiClientProvider>(new TestBackendApiClientProvider(_api));
        Services.AddSingleton<IExternalAuthenticationPermissionService>(new PermissionService(true));
        Services.AddSingleton<ICustomConnectionEditorRegistry>(new CustomConnectionEditorRegistry([]));
        Render<MudPopoverProvider>();
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

    [Fact]
    public void ShadowedConnection_ExplainsWhyThePersistedRecordIsNotEffective()
    {
        var connection = CreateConnection();
        connection.Shadowed = true;
        var cut = Render<ConnectionEditor>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter())
            .Add(component => component.Model, CreateMutation())
            .Add(component => component.ReadOnly, true));

        Assert.Contains("shadowed by deployment configuration", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
        Assert.False(ConnectionActionAvailability.CanEnableOrDisable(configuration, true));
        Assert.False(ConnectionActionAvailability.CanArchiveOrRestore(configuration, true));
        Assert.True(ConnectionActionAvailability.CanEnableOrDisable(database, true));
        Assert.True(ConnectionActionAvailability.CanArchiveOrRestore(database, true));
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
    public void ConnectionList_UsesTheServerCursorForTheNextPage()
    {
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [CreateConnection()], NextCursor = "cursor-2" });
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [new ConnectionSummary { Id = "connection-2", Key = "next", DisplayName = "Next", AdapterType = "custom" }], NextCursor = null });

        var cut = Render<ConnectionIndex>();
        cut.WaitForAssertion(() => Assert.Contains("Contoso", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Next page", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("Next", cut.Markup));

        Assert.Equal([null, "cursor-2"], _api.Cursors);
    }

    [Fact]
    public void ConnectionList_ExposesLabeledFiltersStatusAndNamedActions()
    {
        var connection = CreateConnection();
        connection.EnabledIntent = true;
        connection.Validity = "valid";
        connection.OverridesConfigurationConnection = true;
        _api.ListResults.Enqueue(new ListConnectionsResponse { Items = [connection] });

        var cut = Render<ConnectionIndex>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("SSO connections", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Search connections", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Include archived", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Enabled", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("valid", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Studio override", cut.Markup, StringComparison.Ordinal);
            Assert.NotEmpty(cut.FindAll("[aria-label=\"Actions\"]"));
            Assert.Contains(cut.FindAll("button"), button => button.TextContent.Contains("Manage", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Menu_IsVisibleOnlyWhenFeatureAndReadPermissionAreAvailable()
    {
        var menu = new ExternalAuthenticationSettingsSectionProvider(new FeatureProvider(true), new PermissionService(true));

        var visible = await menu.GetSectionsAsync();
        var item = Assert.Single(visible, candidate => candidate.Href == "settings/sso-connections");
        Assert.Equal("settings/sso-connections", item.Href);

        var hidden = await new ExternalAuthenticationSettingsSectionProvider(new FeatureProvider(true), new PermissionService(false)).GetSectionsAsync();
        Assert.Empty(hidden);
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
        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) => ValueTask.FromResult(allowed);
        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlySet<string>>(allowed ? new HashSet<string>(["*"], StringComparer.Ordinal) : new HashSet<string>(StringComparer.Ordinal));
    }

    private sealed class CustomEditorRegistration(string key, int contractVersion, Type componentType) : ICustomConnectionEditorRegistration
    {
        public string Key => key;
        public int ContractVersion => contractVersion;
        public Type ComponentType => componentType;
    }

    private sealed class TestCustomEditor : ComponentBase, IConnectionCustomEditor;

    private sealed class TestBackendApiClientProvider(IExternalAuthenticationConnectionsApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/elsa/api/");
        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => ValueTask.FromResult((T)api);
    }

    private sealed class TestConnectionsApi : IExternalAuthenticationConnectionsApi, IIdentityRolesApi
    {
        public Queue<ListConnectionsResponse> ListResults { get; } = new();
        public List<string?> Cursors { get; } = [];
        public ConnectionDetail? GetResult { get; set; }
        public ICollection<AdapterDescriptor> Adapters { get; set; } = [];
        public ConnectionMutation? CreatedRequest { get; private set; }

        public Task<ListConnectionsResponse> ListAsync(string? search = null, string? source = null, string? scope = null, string? adapterType = null, bool? enabled = null, bool? valid = null, bool? shadowed = null, bool? archived = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            Cursors.Add(cursor);
            return Task.FromResult(ListResults.Dequeue());
        }

        public Task<ConnectionDetail> GetAsync(string connectionId, CancellationToken cancellationToken = default) => Task.FromResult(GetResult ?? throw new NotSupportedException());
        public Task<ICollection<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default) => Task.FromResult(Adapters);
        public Task<ICollection<PermissionGrantSourceDescriptor>> GetPermissionSourcesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<UnlinkedIdentityPolicyDescriptor>> GetPoliciesAsync(CancellationToken cancellationToken = default) => Task.FromResult<ICollection<UnlinkedIdentityPolicyDescriptor>>([]);
        public Task<ICollection<ExternalUserMatcherDescriptor>> GetUserMatchersAsync(CancellationToken cancellationToken = default) => Task.FromResult<ICollection<ExternalUserMatcherDescriptor>>([]);
        public Task<ManagedSecretResolverCatalog> GetManagedSecretResolversAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ManagedSecretResolverCatalog { Items = [new ManagedSecretResolverDescriptor { Type = "elsa-secrets", DisplayName = "Elsa Secrets" }] });
        public Task<ICollection<PermissionDescriptor>> GetPermissionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> CreateAsync(ConnectionMutation request, CancellationToken cancellationToken = default)
        {
            CreatedRequest = request;
            return Task.FromResult(new ConnectionDetail { Id = "override-1", Key = request.Key, DisplayName = request.DisplayName, AdapterType = request.AdapterType });
        }
        public Task<ConnectionDetail> UpdateAsync(string connectionId, ConnectionMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task EnableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DisableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ArchiveAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RestoreAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionValidationResult> ValidateAsync(string connectionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> ReplaceManagedSecretAsync(string connectionId, string fieldName, ManagedSecretMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveSecretBindingAsync(string connectionId, string fieldName, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        Task<IdentityRoleOptionsResponse> IIdentityRolesApi.ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new IdentityRoleOptionsResponse());
    }
}
