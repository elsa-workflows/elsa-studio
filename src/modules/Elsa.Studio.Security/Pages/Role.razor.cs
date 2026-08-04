using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Security.Pages;

public partial class Role : IDisposable
{
    private readonly HashSet<string> _rolePermissions = new(StringComparer.Ordinal);
    private IReadOnlySet<string> _permissions = new HashSet<string>();
    private MudForm _form = default!;
    private bool _loading = true;
    private bool _saving;
    private string? _loadError;
    private string? _name;
    private string? _loadedId;
    private bool _hasLoadedParameters;
    private long _loadVersion;
    private bool _disposed;

    [Parameter] public string? Id { get; set; }
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private IIdentityPermissionService PermissionService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected bool IsNew => string.IsNullOrWhiteSpace(Id);
    protected bool CanSave => IsNew ? Has(IdentityPermissions.CreateRole) : Has(IdentityPermissions.UpdateRole);
    protected bool CanDelete => Has(IdentityPermissions.DeleteRole);
    protected string PageTitleText => IsNew ? "Create role" : _name ?? "Role";
    protected string PageDescription => IsNew ? "Create a reusable set of Elsa permissions." : "Manage the permissions granted by this role.";
    protected string SaveLabel => IsNew ? "Create role" : "Save changes";

    protected override async Task OnParametersSetAsync()
    {
        if (_hasLoadedParameters && string.Equals(_loadedId, Id, StringComparison.Ordinal))
            return;

        _hasLoadedParameters = true;
        _loadedId = Id;
        var loadVersion = ++_loadVersion;
        var requestedId = Id;
        var isNew = string.IsNullOrWhiteSpace(requestedId);
        ResetEditorState();

        try
        {
            var permissions = await PermissionService.ListAsync();
            if (!IsCurrentLoad(loadVersion))
                return;
            _permissions = permissions;

            if (isNew && !Has(IdentityPermissions.CreateRole))
            {
                _loadError = "You do not have permission to create roles.";
                return;
            }

            if (!isNew && !Has(IdentityPermissions.ReadRole))
            {
                _loadError = "You do not have permission to view roles.";
                return;
            }

            if (!isNew)
            {
                var api = await ApiClientProvider.GetApiAsync<IRolesApi>();
                if (!IsCurrentLoad(loadVersion))
                    return;
                var roles = await api.ListAsync();
                if (!IsCurrentLoad(loadVersion))
                    return;
                var role = roles.Roles.FirstOrDefault(x => string.Equals(x.Id, requestedId, StringComparison.Ordinal));
                if (role == null)
                {
                    _loadError = "The role was not found.";
                    return;
                }

                _name = role.Name;
                _rolePermissions.UnionWith(role.Permissions);
            }
        }
        catch (Exception exception)
        {
            if (IsCurrentLoad(loadVersion))
                _loadError = IdentityApiErrors.ToDisplayMessage(exception, "The role editor could not be loaded.");
        }
        finally
        {
            if (IsCurrentLoad(loadVersion))
                _loading = false;
        }
    }

    protected async Task SaveAsync()
    {
        var operationVersion = _loadVersion;
        var operationId = Id;
        var isNew = string.IsNullOrWhiteSpace(operationId);
        await _form.Validate();
        if (!IsCurrentLoad(operationVersion) || !_form.IsValid || !CanSave)
            return;

        var name = _name!.Trim();
        var permissions = _rolePermissions.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        _saving = true;
        StateHasChanged();
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IRolesApi>();
            if (!IsCurrentLoad(operationVersion))
                return;

            if (isNew)
            {
                var created = await api.CreateAsync(new() { Name = name, Permissions = permissions });
                if (!IsCurrentLoad(operationVersion))
                    return;
                Snackbar.Add("Role created.", Severity.Success);
                NavigationManager.NavigateTo($"security/roles/{Uri.EscapeDataString(created.Id)}");
            }
            else
            {
                var updated = await api.UpdateAsync(operationId!, new() { Name = name, Permissions = permissions });
                if (!IsCurrentLoad(operationVersion))
                    return;
                _name = updated.Name;
                Snackbar.Add("Role updated.", Severity.Success);
            }
        }
        catch (Exception exception)
        {
            if (IsCurrentLoad(operationVersion))
                Snackbar.Add(IdentityApiErrors.ToDisplayMessage(exception, isNew ? "The role could not be created." : "The role could not be updated."), Severity.Error);
        }
        finally
        {
            if (IsCurrentLoad(operationVersion))
                _saving = false;
        }
    }

    protected async Task DeleteAsync()
    {
        var operationVersion = _loadVersion;
        var operationId = Id;
        var operationName = _name;
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete role?",
            $"Delete {operationName}? Users and policies may depend on it.",
            yesText: "Delete",
            cancelText: "Cancel");
        if (confirmed != true || !IsCurrentLoad(operationVersion))
            return;

        _saving = true;
        StateHasChanged();
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IRolesApi>();
            if (!IsCurrentLoad(operationVersion))
                return;
            await api.DeleteAsync(operationId!);
            if (!IsCurrentLoad(operationVersion))
                return;
            Snackbar.Add("Role deleted.", Severity.Success);
            NavigationManager.NavigateTo("security/roles");
        }
        catch (Exception exception)
        {
            if (IsCurrentLoad(operationVersion))
                Snackbar.Add(IdentityApiErrors.ToDisplayMessage(exception, "The role could not be deleted because it may still be in use."), Severity.Error);
        }
        finally
        {
            if (IsCurrentLoad(operationVersion))
                _saving = false;
        }
    }

    protected async Task OnPermissionsChanged() => await _form.Validate();

    private void ResetEditorState()
    {
        _loading = true;
        _saving = false;
        _loadError = null;
        _name = null;
        _permissions = new HashSet<string>();
        _rolePermissions.Clear();
    }

    private bool Has(string permission) => _permissions.Contains("*") || _permissions.Contains(permission);
    private bool IsCurrentLoad(long version) => !_disposed && version == _loadVersion;

    public void Dispose()
    {
        _disposed = true;
        _loadVersion++;
    }
}
