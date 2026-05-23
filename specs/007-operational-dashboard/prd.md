# PRD: Elsa Studio Operational Dashboard

## Summary

Replace the current simple Elsa Studio dashboard with an operational MudBlazor dashboard that helps users understand backend health, recent workflow activity, active issues, and diagnostics status at a glance. The dashboard should be dense, practical, and linked to existing Studio workflow and diagnostics pages.

The first screen should answer:

- Is the selected Elsa backend connected and accepting work?
- How many workflow instances are running, completed, faulted, suspended, or incident-bearing?
- What changed over the selected time range?
- What needs attention right now?
- Which recent workflow instances should I inspect?
- Are structured logs and console logs healthy?

## Problem

The current dashboard page is a static welcome surface with a documentation link. It does not help operators, administrators, or developers understand the state of the backend. Studio already has rich workflow, structured log, and console pages, but users need a first-stop overview that summarizes those areas and routes them to the right drill-down.

Without a dashboard-shaped backend API, Studio would need to issue many small API calls and approximate aggregates client-side. The preferred solution is for Studio to consume a backend dashboard API module from Elsa Core, while keeping graceful fallback states for older backends.

## Goals

- Build a useful operational dashboard as the Studio home page.
- Use MudBlazor components and existing Studio layout/menu conventions.
- Consume dashboard-shaped backend endpoints when available.
- Show clear unavailable/unauthorized states when the backend does not expose dashboard data.
- Link metric cards, findings, and tables to existing Workflow Instances, Structured Logs, and Console pages.
- Keep the interface compact and work-focused rather than marketing-oriented.
- Support selected time ranges such as `1h`, `24h`, and `7d`.
- Refresh data manually and optionally at a conservative interval.

## Non-Goals

- Do not add workflow write actions directly to the dashboard in the first slice.
- Do not duplicate full workflow instance filtering or log viewing on the dashboard.
- Do not require the diagnostics modules to be installed.
- Do not build a custom charting library.
- Do not make the dashboard a landing page or documentation page.
- Do not display raw workflow variables, inputs, outputs, or raw console lines.

## Users

- Workflow operators checking production or staging health.
- Administrators triaging workflow incidents.
- Developers validating local backend behavior.
- Support engineers looking for a fast route to workflow and diagnostics pages.

## Backend Dependency

Preferred backend module: `Elsa.Dashboard.Api`

Preferred endpoints:

- `GET /dashboard/overview`
- `POST /dashboard/workflow-trends`
- `GET /dashboard/needs-attention`
- `GET /dashboard/recent-activity`
- `POST /dashboard/workflow-hotspots`

The Studio module should detect the backend capability through installed features or a guarded API call. If the dashboard API is unavailable, Studio may show a limited fallback using existing workflow APIs, but this fallback should be explicitly marked limited.

## Existing Studio Context

Current dashboard module:

```text
src/modules/Elsa.Studio.Dashboard/
├── Feature.cs
├── Extensions/ServiceCollectionExtensions.cs
├── Menu/DashboardMenu.cs
└── Pages/Index.razor
```

The current page contains only:

- Page heading: Dashboard
- Text: Manage all the things
- Documentation alert

The replacement should keep the route `/` and the existing dashboard menu item.

## Information Architecture

### Header

Content:

- Page title: `Dashboard`
- Backend/environment indicator when available
- Runtime status chip:
  - `Accepting work`
  - `Paused`
  - `Draining`
  - `Unavailable`
- Last refreshed timestamp
- Refresh icon button
- Time range segmented control:
  - `1h`
  - `24h`
  - `7d`

MudBlazor components:

- `MudStack`
- `MudText`
- `MudChip`
- `MudToggleGroup`
- `MudToggleItem`
- `MudIconButton`
- `MudTooltip`

### Metric Row

Cards:

- Running
- Completed in range
- Faulted in range
- Suspended
- Average duration
- Warnings / needs attention

Each card should include:

- Label
- Value
- Optional delta or range caption
- Icon
- Severity color only when meaningful
- Click target where a drill-down exists

MudBlazor components:

- `MudGrid`
- `MudItem`
- `MudPaper`
- `MudIcon`
- `MudText`

### Needs Attention

Prioritized list of findings returned by the backend.

Examples:

- `12 workflows faulted in the last 24 hours`
- `Runtime is paused`
- `1 ingress source failed to pause`
- `3 console log sources are stale`
- `Structured log storage dropped writes`
- `stderr volume increased`

Behavior:

- Empty state: quiet success state, not a large illustration.
- Findings link to relevant pages.
- Severity shown with compact color and icon.
- Limited to a practical number, e.g. 8.

MudBlazor components:

- `MudList`
- `MudListItem`
- `MudIcon`
- `MudChip`
- `MudButton`

### Execution Trend

Line or stacked chart showing activity over the selected time range.

Series:

- Created or started
- Finished
- Faulted
- Suspended or incident-bearing, if useful

MudBlazor components:

- `MudChart`
- `MudPaper`
- `MudSkeleton` while loading

### Recent Workflow Activity

Compact table of recent workflow instances.

Columns:

- Workflow / instance
- Status
- Sub-status
- Incidents
- Duration
- Updated
- Action icon

Behavior:

- Click row opens the workflow instance viewer.
- Faulted/interrupted rows should be visually scannable.
- Use dense mode.
- Avoid full workflow state loading on the dashboard.

MudBlazor components:

- `MudTable`
- `MudChip`
- `MudIconButton`
- `MudTooltip`

### Diagnostics Snapshot

Small panel for logs and sources.

Content:

- Structured log source count and stale count.
- Recent error/critical count.
- Structured log dropped event/write count.
- Console log source count and stale count.
- Recent stderr count.
- Console dropped-line count.
- Links to Structured Logs and Console pages when available.

Behavior:

- If diagnostics modules are absent, show `Not installed`.
- If unauthorized, show `No access`.
- Do not display raw log lines.

### Workflow Hotspots

Optional lower-priority panel showing top workflows by selected metric:

- Faults
- Executions
- Incidents
- Duration

This panel is part of the dashboard contract, but should render only when the backend exposes the hotspot endpoint. If a backend does not support hotspots yet, the dashboard should omit this panel rather than showing an error.

## Visual Design Direction

The dashboard should feel like an operational admin tool:

- Dense but not crowded.
- White or neutral background with outlined panels.
- 8px or smaller border radius, unless existing Studio theme dictates otherwise.
- Color used primarily for semantic severity.
- Avoid marketing hero treatment.
- Avoid decorative gradients and large empty cards.
- Keep cards at one level only; do not nest cards inside cards.
- Use icons in buttons and status surfaces.
- Use compact typography inside cards and tables.

## Component Plan

Suggested Studio additions:

```text
src/modules/Elsa.Studio.Dashboard/
├── Client/
│   └── IDashboardApi.cs
├── Models/
│   ├── DashboardOverview.cs
│   ├── DashboardRange.cs
│   ├── DashboardCapabilityStatus.cs
│   ├── DashboardWorkflowInstanceMetrics.cs
│   ├── DashboardRuntimeStatus.cs
│   ├── DashboardDiagnosticsSummary.cs
│   ├── DashboardFinding.cs
│   ├── DashboardNeedsAttentionResponse.cs
│   ├── DashboardTrendRequest.cs
│   ├── DashboardTrendResponse.cs
│   ├── DashboardTrendBucket.cs
│   ├── DashboardRecentActivityItem.cs
│   ├── DashboardRecentActivityResponse.cs
│   ├── DashboardHotspot.cs
│   ├── DashboardWorkflowHotspotsRequest.cs
│   └── DashboardWorkflowHotspotsResponse.cs
├── Services/
│   ├── DashboardService.cs
│   ├── DashboardNavigationTargetMapper.cs
│   └── DashboardRangeMapper.cs
├── Components/
│   ├── DashboardMetricCard.razor
│   ├── DashboardNeedsAttention.razor
│   ├── DashboardTrendChart.razor
│   ├── DashboardRecentActivityTable.razor
│   ├── DashboardDiagnosticsSnapshot.razor
│   ├── DashboardWorkflowHotspotsPanel.razor
│   └── DashboardRuntimeChip.razor
└── Pages/
    ├── Index.razor
    ├── Index.razor.cs
    └── Index.razor.css
```

Response and request model boundaries:

- `DashboardTrendRequest`: selected range, bucket granularity, and `includeSystem`.
- `DashboardTrendResponse`: ordered trend buckets plus the applied range and granularity.
- `DashboardNeedsAttentionResponse`: ordered findings plus source capability metadata.
- `DashboardRecentActivityResponse`: ordered recent workflow activity items plus the applied range.
- `DashboardWorkflowHotspotsRequest`: selected range, metric, take count, and `includeSystem`.
- `DashboardWorkflowHotspotsResponse`: ordered hotspot rows plus the applied range and metric.

## Client API Contract

Add a Refit-style Studio client:

```csharp
public interface IDashboardApi
{
    [Get("/dashboard/overview")]
    Task<DashboardOverview> GetOverviewAsync(
        string? range = null,
        bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Post("/dashboard/workflow-trends")]
    Task<DashboardTrendResponse> GetWorkflowTrendsAsync(
        [Body] DashboardTrendRequest request,
        CancellationToken cancellationToken = default);

    [Get("/dashboard/needs-attention")]
    Task<DashboardNeedsAttentionResponse> GetNeedsAttentionAsync(
        string? range = null,
        int take = 8,
        bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Get("/dashboard/recent-activity")]
    Task<DashboardRecentActivityResponse> GetRecentActivityAsync(
        string? range = null,
        int take = 20,
        bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Post("/dashboard/workflow-hotspots")]
    Task<DashboardWorkflowHotspotsResponse> GetWorkflowHotspotsAsync(
        [Body] DashboardWorkflowHotspotsRequest request,
        CancellationToken cancellationToken = default);
}
```

## Page States

The dashboard must handle:

- Initial loading.
- Successful full data load.
- Partial capability data.
- Dashboard API unavailable.
- Unauthorized dashboard API.
- Backend disconnected.
- Empty workflow data.
- Refresh failure after previous successful data.

Do not replace the whole page with an error if stale data exists; show a compact error alert and keep the last successful snapshot.

## Navigation

Metric and finding click targets:

- Running workflows -> Workflow Instances filtered by `Status=Running`.
- Faulted workflows -> Workflow Instances filtered by `SubStatus=Faulted` or `HasIncidents=true`.
- Suspended workflows -> Workflow Instances filtered by `SubStatus=Suspended`.
- Interrupted workflows -> Workflow Instances filtered by `SubStatus=Interrupted`.
- Recent activity row -> Workflow Instance viewer.
- Structured log errors -> Structured Logs page with level/time filters.
- Console stderr -> Console page with stream/time filters.

If URL filter support is missing in the destination page, linking should still navigate to the relevant page and preserve a TODO for later filter deep links.

## Functional Requirements

- The dashboard route remains `/`.
- The dashboard menu label remains `Dashboard`.
- The page loads overview, needs-attention, trend, and recent activity data for the selected range.
- The page loads workflow hotspots for the selected range when the backend exposes the hotspot endpoint.
- The user can switch range between `1h`, `24h`, and `7d`.
- The user can refresh manually.
- The page shows last refreshed time.
- Metric cards render zero values explicitly.
- Needs-attention findings are ordered by backend priority.
- Recent activity table uses dense layout and links to instance details.
- Diagnostics snapshot clearly distinguishes available, unavailable, and unauthorized capabilities.
- The page remains usable on laptop and mobile widths.

## Non-Functional Requirements

- First meaningful dashboard render should happen within 2 seconds against a local backend.
- The dashboard should avoid excessive polling; default to manual refresh unless product direction asks for auto-refresh.
- Use cancellation tokens for all API calls.
- Dispose any timers or cancellation resources.
- Avoid layout shift by reserving stable card/table dimensions.
- Do not use raw strings for route construction where Studio has existing route helpers.
- Keep components small and testable.

## Accessibility

- Icon-only buttons require tooltips and aria labels.
- Severity must not rely only on color.
- Tables need clear column labels.
- Loading skeletons must not obscure focus.
- Refresh and range controls must be keyboard usable.

## Testing

Unit tests:

- Range mapping to backend duration values.
- Capability state mapping.
- Navigation target mapping.
- Metric formatting for zero, large values, durations, and unavailable values.
- Needs-attention severity/icon mapping.

Component/manual verification:

- Full data.
- Empty data.
- Dashboard API unavailable.
- Unauthorized.
- Partial diagnostics availability.
- Faulted/interrupted/suspended findings.
- Mobile and desktop layout.

## Rollout

Phase 1:

- Add dashboard client/service/models.
- Replace static dashboard page with operational layout.
- Render overview, needs-attention, trends, recent activity, diagnostics snapshot, and workflow hotspots when supported.
- Gracefully handle unavailable backend dashboard API.

Phase 2:

- Add deep link filters to destination pages where missing.
- Add optional auto-refresh setting.

Phase 3:

- Add tenant/environment-aware filters if backend supports them.
- Add user customization, if product demand appears.

## Acceptance Criteria

- The Studio home page is an operational dashboard, not a welcome/documentation page.
- The dashboard uses MudBlazor and existing Studio conventions.
- A dashboard-capable backend renders metrics, trend chart, needs-attention list, recent activity, runtime state, and diagnostics snapshot.
- A backend without dashboard API renders a clear limited/unavailable state.
- Unauthorized dashboard data is handled without a broken page.
- Clicking metric cards or findings navigates to relevant Studio pages.
- The layout is coherent on desktop and mobile.
- Tests cover core mapping and formatting logic.

## Open Questions

- Should Studio include a limited fallback mode using existing workflow APIs before the backend dashboard API lands?
- Should dashboard data auto-refresh by default, or stay manual for predictable backend load?
- Should the dashboard expose an `include system workflows` toggle in the first version?
- Should runtime status be visible to every dashboard reader or only users with workflow instance read permission?
