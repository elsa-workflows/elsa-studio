using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Security.Pages;

public partial class User : IDisposable
{
    private readonly List<RoleSummary> _roleOptions = [];
    private readonly HashSet<string> _originalRoles = new(StringComparer.Ordinal);
    private IReadOnlySet<string> _permissions = new HashSet<string>();
    private IReadOnlyCollection<string> _selectedRoles = [];
    private MudForm _form = default!;
    private bool _loading = true;
    private bool _saving;
    private string? _loadError;
    private string? _name;
    private string? _password;
    private string? _passwordConfirmation;
    private string? _loadedId;
    private bool _hasLoadedParameters;
    private bool _rolesChanged;
    private long _loadVersion;
    private bool _disposed;

    [Parameter] public string? Id { get; set; }
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private IIdentityPermissionService PermissionService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected bool IsNew => string.IsNullOrWhiteSpace(Id);
    protected bool CanReadRoles => Has(IdentityClaimPermissions.ReadRole);
    protected bool CanSave => IsNew ? Has(IdentityClaimPermissions.CreateUser) : Has(IdentityClaimPermissions.UpdateUser);
    protected bool CanDelete => Has(IdentityClaimPermissions.DeleteUser);
    protected string PageTitleText => IsNew ? "Create user" : _name ?? "User";
    protected string PageDescription => IsNew ? "Create an Elsa account and assign its roles." : "Manage role assignments and account credentials.";
    protected string SaveLabel => IsNew ? "Create user" : "Save changes";
    protected string PasswordHeading => IsNew ? "Password" : "Change password";
    protected string PasswordLabel => IsNew ? "Password (optional)" : "New password (optional)";
    protected string CredentialGuidance => IsNew
        ? "Leave blank to let Elsa generate a one-time password."
        : "Leave both fields blank to keep the current password.";

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

            if (isNew && !Has(IdentityClaimPermissions.CreateUser))
            {
                _loadError = "You do not have permission to create users.";
                return;
            }

            if (!isNew && !Has(IdentityClaimPermissions.ReadUser))
            {
                _loadError = "You do not have permission to view users.";
                return;
            }

            var usersApi = await ApiClientProvider.GetApiAsync<IUsersApi>();
            if (!IsCurrentLoad(loadVersion))
                return;
            var rolesApi = CanReadRoles ? await ApiClientProvider.GetApiAsync<IRolesApi>() : null;
            if (!IsCurrentLoad(loadVersion))
                return;
            var rolesTask = rolesApi?.ListAsync();

            if (!isNew)
            {
                var users = await usersApi.ListAsync();
                if (!IsCurrentLoad(loadVersion))
                    return;
                var user = users.Users.FirstOrDefault(x => string.Equals(x.Id, requestedId, StringComparison.Ordinal));
                if (user == null)
                {
                    _loadError = "The user was not found.";
                    return;
                }

                _name = user.Name;
                _originalRoles.UnionWith(user.Roles);
                _selectedRoles = _originalRoles.ToHashSet(StringComparer.Ordinal);
            }

            if (rolesTask != null)
            {
                var roles = await rolesTask;
                if (!IsCurrentLoad(loadVersion))
                    return;
                _roleOptions.AddRange(roles.Roles.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
            }
        }
        catch (Exception exception)
        {
            if (IsCurrentLoad(loadVersion))
                _loadError = IdentityApiErrors.ToDisplayMessage(exception, "The user editor could not be loaded.");
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
        var password = _password;
        var suppliedPassword = !string.IsNullOrWhiteSpace(password);
        var roles = _selectedRoles.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var rolesChanged = _rolesChanged;
        _saving = true;
        StateHasChanged();
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IUsersApi>();
            if (!IsCurrentLoad(operationVersion))
                return;

            if (isNew)
            {
                var created = await api.CreateAsync(new()
                {
                    Name = name,
                    Password = suppliedPassword ? password : null,
                    Roles = roles
                });
                if (!IsCurrentLoad(operationVersion))
                    return;

                Snackbar.Add("User created.", Severity.Success);
                if (!suppliedPassword && !string.IsNullOrWhiteSpace(created.Password))
                    await ShowGeneratedPasswordAsync(created.Password);
                if (!IsCurrentLoad(operationVersion))
                    return;
                NavigationManager.NavigateTo($"security/users/{Uri.EscapeDataString(created.Id)}");
            }
            else
            {
                await api.UpdateAsync(operationId!, new()
                {
                    Password = suppliedPassword ? password : null,
                    Roles = rolesChanged ? roles : null
                });
                if (!IsCurrentLoad(operationVersion))
                    return;
                _originalRoles.Clear();
                _originalRoles.UnionWith(roles);
                _rolesChanged = false;
                _password = null;
                _passwordConfirmation = null;
                Snackbar.Add("User updated.", Severity.Success);
            }
        }
        catch (Exception exception)
        {
            if (IsCurrentLoad(operationVersion))
                Snackbar.Add(IdentityApiErrors.ToDisplayMessage(exception, isNew ? "The user could not be created." : "The user could not be updated."), Severity.Error);
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
            "Delete user?",
            $"Delete {operationName}? This cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");
        if (confirmed != true || !IsCurrentLoad(operationVersion))
            return;

        _saving = true;
        StateHasChanged();
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IUsersApi>();
            if (!IsCurrentLoad(operationVersion))
                return;
            await api.DeleteAsync(operationId!);
            if (!IsCurrentLoad(operationVersion))
                return;
            Snackbar.Add("User deleted.", Severity.Success);
            NavigationManager.NavigateTo("security/users");
        }
        catch (Exception exception)
        {
            if (IsCurrentLoad(operationVersion))
                Snackbar.Add(IdentityApiErrors.ToDisplayMessage(exception, "The user could not be deleted."), Severity.Error);
        }
        finally
        {
            if (IsCurrentLoad(operationVersion))
                _saving = false;
        }
    }

    protected string? ValidatePasswordConfirmation(string? value)
    {
        if (string.IsNullOrEmpty(_password) && string.IsNullOrEmpty(value))
            return null;
        return string.Equals(_password, value, StringComparison.Ordinal) ? null : "Passwords do not match.";
    }

    protected Task OnSelectedRolesChanged(IReadOnlyCollection<string> roles)
    {
        var selectedRoles = roles.ToHashSet(StringComparer.Ordinal);
        _selectedRoles = selectedRoles;
        _rolesChanged = !_originalRoles.SetEquals(selectedRoles);
        return Task.CompletedTask;
    }

    private void ResetEditorState()
    {
        _loading = true;
        _saving = false;
        _loadError = null;
        _name = null;
        _password = null;
        _passwordConfirmation = null;
        _permissions = new HashSet<string>();
        _selectedRoles = [];
        _roleOptions.Clear();
        _originalRoles.Clear();
        _rolesChanged = false;
    }

    private async Task ShowGeneratedPasswordAsync(string password)
    {
        var parameters = new DialogParameters<GeneratedPasswordDialog> { { x => x.Password, password } };
        var options = new DialogOptions
        {
            BackdropClick = false,
            CloseButton = false,
            CloseOnEscapeKey = false,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };
        var dialog = await DialogService.ShowAsync<GeneratedPasswordDialog>("Save generated password", parameters, options);
        await dialog.Result;
    }

    private bool Has(string permission) => _permissions.Contains("*") || _permissions.Contains(permission);
    private bool IsCurrentLoad(long version) => !_disposed && version == _loadVersion;

    public void Dispose()
    {
        _disposed = true;
        _loadVersion++;
    }
}
