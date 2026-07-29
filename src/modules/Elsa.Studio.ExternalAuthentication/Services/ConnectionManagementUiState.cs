using System.Net;
using System.Text.Json;
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

public static class ConnectionStatusPresentation
{
    public static string LifecycleLabel(ConnectionSummary connection) =>
        connection.Archived ? "Archived" : connection.EnabledIntent ? "Enabled" : "Disabled";

    public static Color LifecycleColor(ConnectionSummary connection) =>
        connection.Archived
            ? Color.Default
            : connection.EffectivelyEnabled
                ? Color.Success
                : connection.EnabledIntent
                    ? Color.Warning
                    : Color.Default;

    public static string ValidityLabel(string validity) =>
        string.Equals(validity, "valid", StringComparison.OrdinalIgnoreCase)
            ? "Valid"
            : string.Equals(validity, "invalid", StringComparison.OrdinalIgnoreCase)
                ? "Invalid"
                : "Not validated";

    public static Color ValidityColor(string validity) =>
        string.Equals(validity, "valid", StringComparison.OrdinalIgnoreCase)
            ? Color.Success
            : string.Equals(validity, "invalid", StringComparison.OrdinalIgnoreCase)
                ? Color.Error
                : Color.Default;
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
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static ConnectionManagementErrorInfo Parse(HttpStatusCode statusCode, string? content, string fallbackMessage)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ConnectionManagementErrorInfo.Fallback(statusCode, fallbackMessage);

        try
        {
            var document = JsonSerializer.Deserialize<ManagementErrorDocument>(content, SerializerOptions);
            if (document is null)
                return ConnectionManagementErrorInfo.Fallback(statusCode, fallbackMessage);

            var errors = ReadValidationErrors(document.Details);
            var warnings = ReadWarnings(document.Details);
            var conflictCode = ReadString(document.Details, "code");
            var currentRevision = ReadInt64(document.Details, "currentRevision");
            return new ConnectionManagementErrorInfo(
                statusCode,
                document.Error ?? string.Empty,
                string.IsNullOrWhiteSpace(document.Message) ? fallbackMessage : document.Message,
                errors,
                warnings,
                conflictCode,
                currentRevision);
        }
        catch (JsonException)
        {
            return ConnectionManagementErrorInfo.Fallback(statusCode, fallbackMessage);
        }
    }

    public static bool IsFinalLoginPathGuard(System.Net.HttpStatusCode statusCode, string? content) =>
        statusCode == System.Net.HttpStatusCode.Conflict &&
        string.Equals(
            Parse(statusCode, content, string.Empty).ConflictCode,
            "final_login_path_guard",
            StringComparison.Ordinal);

    private static IReadOnlyCollection<ConnectionValidationMessage> ReadValidationErrors(JsonElement details)
    {
        if (details.ValueKind != JsonValueKind.Object ||
            !details.TryGetProperty("errors", out var errors) ||
            errors.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<ConnectionValidationMessage>();
        foreach (var item in errors.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            try
            {
                var error = item.Deserialize<ConnectionValidationMessage>(SerializerOptions);
                if (error is not null)
                    result.Add(error);
            }
            catch (JsonException)
            {
                // Preserve valid sibling details when a server or proxy adds a malformed entry.
            }
        }

        return result;
    }

    private static IReadOnlyCollection<string> ReadWarnings(JsonElement details)
    {
        if (details.ValueKind != JsonValueKind.Object ||
            !details.TryGetProperty("warnings", out var warnings) ||
            warnings.ValueKind != JsonValueKind.Array)
            return [];

        return warnings
            .EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToArray();
    }

    private static string? ReadString(JsonElement details, string propertyName) =>
        details.ValueKind == JsonValueKind.Object &&
        details.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static long? ReadInt64(JsonElement details, string propertyName) =>
        details.ValueKind == JsonValueKind.Object &&
        details.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.Number &&
        property.TryGetInt64(out var value)
            ? value
            : null;

    private sealed class ManagementErrorDocument
    {
        public string? Error { get; init; }
        public string? Message { get; init; }
        public JsonElement Details { get; init; }
    }
}

public sealed record ConnectionManagementErrorInfo(
    HttpStatusCode StatusCode,
    string Code,
    string Message,
    IReadOnlyCollection<ConnectionValidationMessage> Errors,
    IReadOnlyCollection<string> Warnings,
    string? ConflictCode,
    long? CurrentRevision)
{
    public string DisplayMessage
    {
        get
        {
            var details = Errors.Select(error => $"{error.Field}: {error.Message}")
                .Concat(Warnings.Select(warning => $"Warning: {warning}"))
                .ToArray();
            return details.Length == 0 ? Message : $"{Message} {string.Join(" ", details)}";
        }
    }

    public static ConnectionManagementErrorInfo Fallback(HttpStatusCode statusCode, string fallbackMessage) =>
        new(statusCode, string.Empty, fallbackMessage, [], [], null, null);
}
