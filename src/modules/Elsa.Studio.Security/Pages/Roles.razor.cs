using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Security.Pages;

public partial class Roles
{
    protected const int PreviewLimit = 3;
    private readonly HashSet<string> _deletingIds = new(StringComparer.Ordinal);
    private readonly List<RoleSummary> _roles = [];
    private IReadOnlySet<string> _permissions = new HashSet<string>();
    private bool _permissionsLoaded;
    private bool _loading;
    private string? _loadError;
    private string? _search;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private IIdentityPermissionService PermissionService { get; set; } = default!;
    [Inject] private IClipboard Clipboard { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected bool CanRead => Has(IdentityPermissions.ReadRole);
    protected bool CanCreate => Has(IdentityPermissions.CreateRole);
    protected bool CanUpdate => Has(IdentityPermissions.UpdateRole);
    protected bool CanDelete => Has(IdentityPermissions.DeleteRole);
    protected IEnumerable<RoleSummary> FilteredRoles => string.IsNullOrWhiteSpace(_search)
        ? _roles
        : _roles.Where(MatchesSearch);
    protected string EmptyTitle => string.IsNullOrWhiteSpace(_search) ? "No roles yet" : "No matching roles";
    protected string EmptyDescription => string.IsNullOrWhiteSpace(_search)
        ? "Create a role to group permissions for users."
        : "Try a different name, permission, or tenant.";

    protected override async Task OnInitializedAsync()
    {
        _permissions = await PermissionService.ListAsync();
        _permissionsLoaded = true;
        if (CanRead)
            await LoadAsync();
    }

    protected async Task LoadAsync()
    {
        _loading = true;
        _loadError = null;
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IRolesApi>();
            var response = await api.ListAsync();
            _roles.Clear();
            _roles.AddRange(response.Roles.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            _loadError = IdentityApiErrors.ToDisplayMessage(exception, "Roles could not be loaded.");
        }
        finally
        {
            _loading = false;
        }
    }

    protected void OpenRole(TableRowClickEventArgs<RoleSummary> args)
    {
        if (args.Item != null)
            NavigationManager.NavigateTo(RoleUrl(args.Item.Id));
    }

    protected async Task DeleteAsync(RoleSummary role)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete role?",
            $"Delete {role.Name}? Users and policies may depend on it.",
            yesText: "Delete",
            cancelText: "Cancel");
        if (confirmed != true)
            return;

        if (!_deletingIds.Add(role.Id))
            return;
        StateHasChanged();

        try
        {
            var api = await ApiClientProvider.GetApiAsync<IRolesApi>();
            await api.DeleteAsync(role.Id);
            _roles.Remove(role);
            Snackbar.Add("Role deleted.", Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add(IdentityApiErrors.ToDisplayMessage(exception, "The role could not be deleted because it may still be in use."), Severity.Error);
        }
        finally
        {
            _deletingIds.Remove(role.Id);
        }
    }

    protected async Task CopyRoleIdAsync(string roleId)
    {
        await Clipboard.CopyText(roleId);
        Snackbar.Add("Role ID copied.", Severity.Success);
    }

    protected static IEnumerable<string> Preview(ICollection<string> values) =>
        values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Take(PreviewLimit);
    protected static string Scope(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? "Host" : tenantId;
    protected static string RoleUrl(string id) => $"security/roles/{Uri.EscapeDataString(id)}";
    protected bool IsDeleting(string id) => _deletingIds.Contains(id);

    private bool MatchesSearch(RoleSummary role)
    {
        var search = _search!.Trim();
        return role.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
               || role.Id.Contains(search, StringComparison.OrdinalIgnoreCase)
               || role.Permissions.Any(x => x.Contains(search, StringComparison.OrdinalIgnoreCase))
               || (role.TenantId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool Has(string permission) => _permissions.Contains("*") || _permissions.Contains(permission);
}
