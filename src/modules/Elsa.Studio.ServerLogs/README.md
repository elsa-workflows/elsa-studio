# Elsa.Studio.ServerLogs

Elsa.Studio.ServerLogs adds an operational log viewer to Elsa Studio. It loads recent backend log events, subscribes to live updates over SignalR, and lets operators filter by severity, text, category, workflow instance, tenant, trace/correlation, time, and backend source.

## Register The Module

Add the module to the Studio host:

```csharp
builder.Services.AddServerLogsModule();
```

Bundled Studio hosts should reference `src/modules/Elsa.Studio.ServerLogs/Elsa.Studio.ServerLogs.csproj` and call `AddServerLogsModule`.

## Backend Requirement

The paired Elsa Core backend must enable live server log streaming and map the diagnostics endpoints:

```csharp
services.AddElsa(elsa => elsa.UseServerLogStreaming());
app.UseServerLogStreaming();
```

The backend must authorize callers with the `read:server-logs` permission.

## Feature Gating

The Studio feature is decorated with the remote feature name `Elsa.ServerLogStreaming`. Studio should show the module only when the active backend advertises that feature.

## Route

Open the log viewer at:

```text
/server-logs
```

Workflow instance screens can deep-link to:

```text
/server-logs?workflowInstanceId={workflowInstanceId}
```

## Operator Workflow

- Recent logs load when the page opens.
- A live SignalR subscription starts after the initial backfill.
- Pause freezes new rows locally without disconnecting.
- Reconnect restarts the live observer.
- Clear removes local rows without clearing backend history.
- Copy selected and copy visible provide fast handoff for support/debugging.
- Source selection defaults to all sources and can focus on one backend process, pod, or container.

## Clustered Deployments

The page treats backend logs as a merged stream by default. Each row carries source metadata, and the source selector can narrow recent and live logs to a single source. Source-change notifications refresh the selector when new backend sources appear.

## Validation

1. Start an Elsa backend with `UseServerLogStreaming` enabled.
2. Start Studio with `AddServerLogsModule`.
3. Open `/server-logs`.
4. Verify recent rows load or the empty state appears.
5. Emit an `ILogger` message from the backend and verify it appears live.
6. Apply a level filter and verify lower-severity rows are hidden.
7. Select a source and verify recent/live rows are scoped to that source.
8. Open a workflow instance and use the Server Logs action to confirm the workflow filter is applied.
