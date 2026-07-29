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

        ApplyDefaults(mutation, adapter);
    }

    public static void ApplyDefaults(ConnectionMutation mutation, AdapterDescriptor adapter)
    {
        foreach (var field in adapter.Fields.Where(field => !field.IsSecretBinding && field.DefaultValue is not null))
            mutation.AdapterSettings[field.Name] = field.DefaultValue!.Value;

        ApplyDocumentedDefault(mutation.AdapterSettings, adapter.Fields, "clientAuthenticationMethod", "client_secret_basic");
        ApplyDocumentedDefault(mutation.AdapterSettings, adapter.Fields, "mode", "discovery");
    }

    private static void ApplyDocumentedDefault(
        IDictionary<string, JsonElement> settings,
        IEnumerable<ConnectionFieldDescriptor> fields,
        string fieldName,
        string value)
    {
        var field = fields.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (field is not null &&
            !settings.ContainsKey(field.Name) &&
            field.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
            settings[field.Name] = JsonSerializer.SerializeToElement(value);
    }
}

public sealed class ConnectionListPagingState
{
    private readonly List<Page> _pages = [new(null, null)];
    private int _pageIndex;

    public string? NextCursor => CurrentPage.NextCursor;
    public string? CurrentCursor => CurrentPage.Cursor;
    public int PageNumber => _pageIndex + 1;
    public bool HasPrevious => _pageIndex > 0;
    public bool HasNext => !string.IsNullOrWhiteSpace(NextCursor);

    public void Replace(string? nextCursor)
    {
        _pages.Clear();
        _pages.Add(new Page(null, nextCursor));
        _pageIndex = 0;
    }

    public bool TryAdvance(out string cursor)
    {
        cursor = NextCursor ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        if (_pageIndex + 1 < _pages.Count && string.Equals(_pages[_pageIndex + 1].Cursor, cursor, StringComparison.Ordinal))
            _pageIndex++;
        else
        {
            _pages.RemoveRange(_pageIndex + 1, _pages.Count - _pageIndex - 1);
            _pages.Add(new Page(cursor, null));
            _pageIndex++;
        }

        return true;
    }

    public bool TryGoBack(out string? cursor)
    {
        cursor = null;
        if (!HasPrevious)
            return false;

        _pageIndex--;
        cursor = CurrentCursor;
        return true;
    }

    public void SetNext(string? nextCursor) => CurrentPage.NextCursor = nextCursor;

    private Page CurrentPage => _pages[_pageIndex];

    private sealed class Page(string? cursor, string? nextCursor)
    {
        public string? Cursor { get; } = cursor;
        public string? NextCursor { get; set; } = nextCursor;
    }
}

public sealed class ConnectionListRequestState
{
    private long _latestRequest;

    public long Begin() => Interlocked.Increment(ref _latestRequest);
    public bool IsCurrent(long request) => Volatile.Read(ref _latestRequest) == request;
}

public static class ConnectionOverrideDiscovery
{
    public static ConnectionSummary? FindExisting(
        IEnumerable<ConnectionSummary> connections,
        string key,
        ConnectionScope? scope = null) =>
        connections
            .Where(connection =>
                !connection.IsConfigurationOwned &&
                string.Equals(connection.Key, key, StringComparison.Ordinal) &&
                (scope is null || HasSameScope(connection.Scope, scope)))
            .OrderBy(connection => connection.Archived)
            .FirstOrDefault();

    private static bool HasSameScope(ConnectionScope left, ConnectionScope right) =>
        string.Equals(left.Kind, right.Kind, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(left.TenantId, right.TenantId, StringComparison.Ordinal);
}

public static class ConnectionActionAvailability
{
    public static bool CanMutate(ConnectionSummary connection, bool permission) => permission && !connection.IsConfigurationOwned;
    public static bool CanEnableOrDisable(ConnectionSummary connection, bool canUpdate) =>
        !connection.Archived &&
        CanMutate(connection, canUpdate) &&
        (connection.EnabledIntent || string.Equals(connection.Validity, "valid", StringComparison.OrdinalIgnoreCase));
    public static bool CanArchiveOrRestore(ConnectionSummary connection, bool canArchive) => CanMutate(connection, canArchive);
}

public static class DescriptorValuePresentation
{
    public static string GetDisplayName(ConnectionFieldDescriptor field, string value)
    {
        if (IsNamed(field, "clientAuthenticationMethod", "clientAuthentication"))
        {
            return value switch
            {
                "client_secret_basic" => "Client secret (basic authentication)",
                "client_secret_post" => "Client secret (form post)",
                "private_key_jwt" => "Private key JWT",
                "none" => "None (public client)",
                _ => Humanize(value)
            };
        }

        if (IsNamed(field, "mode", "trustMode"))
        {
            return value switch
            {
                "discovery" => "Discovery",
                "manual" => "Manual endpoints and signing keys",
                _ => Humanize(value)
            };
        }

        return Humanize(value);
    }

    private static bool IsNamed(ConnectionFieldDescriptor field, params string[] names) =>
        names.Any(name => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));

    private static string Humanize(string value) =>
        string.Join(" ", value.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => string.Equals(part, "pkce", StringComparison.OrdinalIgnoreCase)
                ? "PKCE"
                : string.Equals(part, "jwt", StringComparison.OrdinalIgnoreCase)
                    ? "JWT"
                    : char.ToUpperInvariant(part[0]) + part[1..]));
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

    public static string LifecycleDisplayLabel(ConnectionSummary connection) =>
        $"{StoredPrefix(connection)}{LifecycleLabel(connection)}";

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

    public static string ValidityDisplayLabel(ConnectionSummary connection) =>
        $"{StoredPrefix(connection)}{ValidityLabel(connection.Validity)}";

    private static string StoredPrefix(ConnectionSummary connection) =>
        !connection.Shadowed
            ? string.Empty
            : connection.IsConfigurationOwned
                ? "Deployment: "
                : "Stored: ";
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
            var correlationId = IsSafeCorrelationId(document.CorrelationId) ? document.CorrelationId : null;
            return new ConnectionManagementErrorInfo(
                statusCode,
                document.Error ?? string.Empty,
                string.IsNullOrWhiteSpace(document.Message) ? fallbackMessage : document.Message,
                errors,
                warnings,
                conflictCode,
                currentRevision,
                correlationId);
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

    private static bool IsSafeCorrelationId(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 128 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    private sealed class ManagementErrorDocument
    {
        public string? Error { get; init; }
        public string? Message { get; init; }
        public string? CorrelationId { get; init; }
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
    long? CurrentRevision,
    string? CorrelationId)
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

    public string OperationalDisplayMessage
    {
        get
        {
            var references = new List<string>();
            if (!string.IsNullOrWhiteSpace(Code))
                references.Add($"Error: {Code}.");
            if (!string.IsNullOrWhiteSpace(CorrelationId))
                references.Add($"Correlation ID: {CorrelationId}.");
            return references.Count == 0 ? DisplayMessage : $"{DisplayMessage} {string.Join(" ", references)}";
        }
    }

    public static ConnectionManagementErrorInfo Fallback(HttpStatusCode statusCode, string fallbackMessage) =>
        new(statusCode, string.Empty, fallbackMessage, [], [], null, null, null);
}
