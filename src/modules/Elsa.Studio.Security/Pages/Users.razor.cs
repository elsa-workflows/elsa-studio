using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Security.Pages;

public partial class Users
{
    protected const int PreviewLimit = 3;
    private readonly HashSet<string> _deletingIds = new(StringComparer.Ordinal);
    private readonly List<UserSummary> _users = [];
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

    protected bool CanRead => Has(IdentityPermissions.ReadUser);
    protected bool CanCreate => Has(IdentityPermissions.CreateUser);
    protected bool CanUpdate => Has(IdentityPermissions.UpdateUser);
    protected bool CanDelete => Has(IdentityPermissions.DeleteUser);
    protected IEnumerable<UserSummary> FilteredUsers => string.IsNullOrWhiteSpace(_search)
        ? _users
        : _users.Where(MatchesSearch);
    protected string EmptyTitle => string.IsNullOrWhiteSpace(_search) ? "No users yet" : "No matching users";
    protected string EmptyDescription => string.IsNullOrWhiteSpace(_search)
        ? "Create a user to grant access to Elsa."
        : "Try a different name, role, or tenant.";

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
            var api = await ApiClientProvider.GetApiAsync<IUsersApi>();
            var response = await api.ListAsync();
            _users.Clear();
            _users.AddRange(response.Users.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            _loadError = IdentityApiErrors.ToDisplayMessage(exception, "Users could not be loaded.");
        }
        finally
        {
            _loading = false;
        }
    }

    protected void OpenUser(TableRowClickEventArgs<UserSummary> args)
    {
        if (args.Item != null)
            NavigationManager.NavigateTo(UserUrl(args.Item.Id));
    }

    protected async Task DeleteAsync(UserSummary user)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete user?",
            $"Delete {user.Name}? This cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");
        if (confirmed != true)
            return;

        if (!_deletingIds.Add(user.Id))
            return;
        StateHasChanged();

        try
        {
            var api = await ApiClientProvider.GetApiAsync<IUsersApi>();
            await api.DeleteAsync(user.Id);
            _users.Remove(user);
            Snackbar.Add("User deleted.", Severity.Success);
        }
        catch (Exception exception)
        {
            Snackbar.Add(IdentityApiErrors.ToDisplayMessage(exception, "The user could not be deleted."), Severity.Error);
        }
        finally
        {
            _deletingIds.Remove(user.Id);
        }
    }

    protected async Task CopyUserIdAsync(string userId)
    {
        await Clipboard.CopyText(userId);
        Snackbar.Add("User ID copied.", Severity.Success);
    }

    protected static IEnumerable<string> Preview(ICollection<string> values) =>
        values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Take(PreviewLimit);
    protected static string Scope(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? "Host" : tenantId;
    protected static string UserUrl(string id) => $"security/users/{Uri.EscapeDataString(id)}";
    protected bool IsDeleting(string id) => _deletingIds.Contains(id);

    private bool MatchesSearch(UserSummary user)
    {
        var search = _search!.Trim();
        return user.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
               || user.Id.Contains(search, StringComparison.OrdinalIgnoreCase)
               || user.Roles.Any(x => x.Contains(search, StringComparison.OrdinalIgnoreCase))
               || (user.TenantId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool Has(string permission) => _permissions.Contains("*") || _permissions.Contains(permission);
}
