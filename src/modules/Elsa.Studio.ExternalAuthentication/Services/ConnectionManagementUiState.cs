using Elsa.Studio.ExternalAuthentication.Models;
using MudBlazor;

namespace Elsa.Studio.ExternalAuthentication.Services;

public static class ConnectionAdapterSelection
{
    public static void Apply(ConnectionDetail connection, ConnectionMutation mutation, AdapterDescriptor adapter)
    {
        connection.AdapterType = adapter.Type;
        connection.AdapterSettingsVersion = adapter.SettingsVersion;
        connection.AdapterSettings.Clear();
        connection.SecretBindings.Clear();
        mutation.AdapterType = adapter.Type;
        mutation.AdapterSettingsVersion = adapter.SettingsVersion;
        mutation.AdapterSettings.Clear();
        mutation.ConfirmUnsafeSettings = false;

        foreach (var field in adapter.Fields.Where(field => !field.IsSecretBinding && field.DefaultValue is not null))
            mutation.AdapterSettings[field.Name] = field.DefaultValue!.Value;
    }
}

public sealed class ConnectionListPagingState
{
    public string? NextCursor { get; private set; }
    public string? CurrentCursor { get; private set; }

    public void Replace(string? nextCursor)
    {
        CurrentCursor = null;
        NextCursor = nextCursor;
    }

    public bool TryAdvance(out string cursor)
    {
        cursor = NextCursor ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;
        CurrentCursor = cursor;
        return true;
    }

    public void SetNext(string? nextCursor) => NextCursor = nextCursor;
}

public static class ConnectionActionAvailability
{
    public static bool CanMutate(ConnectionSummary connection, bool permission) => permission && !connection.IsConfigurationOwned;
    public static bool CanEnableOrDisable(ConnectionSummary connection, bool canUpdate) => !connection.Archived && CanMutate(connection, canUpdate);
    public static bool CanArchiveOrRestore(ConnectionSummary connection, bool canArchive) => CanMutate(connection, canArchive);
}

public enum ConnectionDisableDecision
{
    KeepActiveSessions,
    RevokeActiveSessions
}

public static class ConnectionDisableConfirmation
{
    public static async Task<ConnectionDisableDecision?> ShowAsync(
        IDialogService dialogs,
        string connectionDisplayName,
        bool canRevokeSessions)
    {
        if (!canRevokeSessions)
        {
            var confirmed = await dialogs.ShowMessageBoxAsync(
                "Disable connection?",
                $"Disable {connectionDisplayName}. Existing external authentication sessions will remain active until they expire or are separately revoked.",
                yesText: "Disable",
                cancelText: "Cancel");
            return confirmed == true ? ConnectionDisableDecision.KeepActiveSessions : null;
        }

        var revokeSessions = await dialogs.ShowMessageBoxAsync(
            "Disable connection?",
            $"Disable {connectionDisplayName}. Existing external authentication sessions can remain active until they expire, or be revoked now. Revoked users will need to authenticate again.",
            yesText: "Disable and revoke sessions",
            noText: "Disable only",
            cancelText: "Cancel");

        return revokeSessions switch
        {
            true => ConnectionDisableDecision.RevokeActiveSessions,
            false => ConnectionDisableDecision.KeepActiveSessions,
            null => null
        };
    }
}

public static class ConnectionConcurrency
{
    public static string ToIfMatch(long revision) => $"\"{revision}\"";
    public static bool IsConflict(System.Net.HttpStatusCode statusCode) => statusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.PreconditionFailed;
}

public static class ConnectionConflictRecovery
{
    /// <summary>Uses a server-supplied current document when available, avoiding a second read after a failed ETag mutation.</summary>
    public static bool TryGetCurrent(ManagementApiException exception, out ConnectionDetail current)
    {
        current = exception.Current!;
        return exception.IsConcurrencyConflict && exception.Current is not null;
    }
}

public static class SecretBindingRemovalPrompt
{
    public static string GetMessage(SecretBindingState? binding) =>
        string.Equals(binding?.Ownership, "managed", StringComparison.OrdinalIgnoreCase)
            ? "The managed secret value and its connection binding will be deleted. This cannot be undone."
            : "The external resolver reference will be removed from this connection. The external secret itself is not deleted.";
}

public static class ConnectionManagementError
{
    public static bool IsFinalLoginPathGuard(System.Net.HttpStatusCode statusCode, string? content) =>
        statusCode == System.Net.HttpStatusCode.Conflict &&
        content?.Contains("final_login_path_guard", StringComparison.Ordinal) == true;
}
