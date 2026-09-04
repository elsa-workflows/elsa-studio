using System.Text.Json;
using Refit;

namespace Elsa.Studio.Security.Models;

public static class IdentityPermissions
{
    public const string ReadUser = "read:user";
    public const string CreateUser = "create:user";
    public const string UpdateUser = "update:user";
    public const string DeleteUser = "delete:user";
    public const string ReadRole = "read:role";
    public const string CreateRole = "create:role";
    public const string UpdateRole = "update:role";
    public const string DeleteRole = "delete:role";
}

public class UserSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<string> Roles { get; set; } = [];
    public string? TenantId { get; set; }
}

public sealed class ListUsersResponse
{
    public ICollection<UserSummary> Users { get; set; } = [];
}

public sealed class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Password { get; set; }
    public ICollection<string> Roles { get; set; } = [];
}

public sealed class CreateUserResponse : UserSummary
{
    public string Password { get; set; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    public string? Password { get; set; }
    public ICollection<string>? Roles { get; set; }
}

public sealed class RoleSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<string> Permissions { get; set; } = [];
    public string? TenantId { get; set; }
}

public sealed class ListRolesResponse
{
    public ICollection<RoleSummary> Roles { get; set; } = [];
}

public sealed class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public ICollection<string> Permissions { get; set; } = [];
}

public sealed class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public ICollection<string> Permissions { get; set; } = [];
}

public static class IdentityApiErrors
{
    public static string ToDisplayMessage(Exception exception, string fallback)
    {
        if (exception is not ApiException { Content: { Length: > 0 } content })
            return string.IsNullOrWhiteSpace(exception.Message) ? fallback : exception.Message;

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()))
                return message.GetString()!;

            if (root.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString()))
                return title.GetString()!;
        }
        catch (JsonException)
        {
            // The API returned a non-JSON error body. Use the stable fallback below.
        }

        return fallback;
    }
}
