# Elsa Studio Interface Design System

Status: Approved baseline

Last updated: 2026-07-30

## Direction

Elsa Studio is a technical administration workspace for people configuring,
diagnosing, and operating workflow infrastructure. Interfaces should feel calm,
precise, and operationally trustworthy: dense enough for expert work, but
structured so that state, source, and available actions are immediately clear.

Domain concepts include configuration, deployment, runtime state, validation,
diagnostics, lifecycle, secrets, revisions, and effective versus persisted
records.

The signature pattern is explicit operational context. When displayed data can
come from multiple sources, the interface must distinguish the effective source
from the record being viewed. Stored-record status must never appear to describe
an effective deployment-owned record.

## Foundation

- Use MudBlazor components and theme tokens first.
- Add custom CSS only when composition and responsive utility classes cannot
  express the required behavior.
- Preserve the existing Elsa typography and icon family.
- Use color to communicate brand, state, warning, success, and destructive
  intent; do not use color decoratively.
- Prefer progressive disclosure for advanced and operational concerns.

## Palette

Use the configured MudBlazor theme rather than hard-coded colors:

- Primary: Elsa navigation, selected state, and routine primary actions.
- Info: deployment-owned or explanatory context.
- Success: effective and valid states.
- Warning: enabled-but-not-effective, shadowed, or attention states.
- Error: destructive actions and failed validation.
- Default/neutral: structure, inactive state, metadata, and borders.

## Depth and Surfaces

Use a borders-only depth strategy for administration screens:

- Page canvas: theme background.
- Workspace and summary surfaces: `MudPaper Outlined="true"`.
- Alerts: MudBlazor semantic alert surfaces.
- Avoid decorative shadows, nested card stacks, and dramatic surface shifts.
- Use dividers only where they clarify a change in responsibility or action
  severity.

## Spacing

MudBlazor's 4px unit is the implementation base. Use an 8px primary rhythm and
the following scale:

- 4px: icon or micro-text adjustment.
- 8px: related inline controls and compact stack gaps.
- 12px: ordinary field or page-stack separation when 8px is too tight.
- 16px: card padding, mobile panel inset, and standard grid gutter.
- 24px: desktop workspace inset and major separation.
- 32px: separation between independent page sections when needed.

Keep padding symmetrical unless the content itself requires asymmetry. Do not
let grids or form controls touch an outlined workspace boundary.

## Connection Workspace Pattern

The approved reference is Concept 2, “Connection Workspace,” selected on
2026-07-29.

### Page composition

- Use `MudContainer MaxWidth="MaxWidth.Large"` for complex connection editing.
- Keep the back action at natural width with `align-self-start`.
- Present connection identity and persistent operational context in one compact,
  outlined header.
- Allow header status chips and actions to wrap rather than collide.
- Place global source or shadowing alerts above the tab strip so their context
  remains visible across tasks.

### Tabs

- Separate Configuration, Provisioning & linking, and Diagnostics.
- Use `KeepPanelsAlive="true"` so custom editors retain transient state.
- Apply responsive inset to the shared panel container with
  `TabPanelsClass="pa-4 pa-sm-6"`: 16px on narrow screens and 24px from the
  small breakpoint upward.
- Use a 16px grid gutter (`MudGrid Spacing="4"`) for two-column panel layouts.
- Ensure all tab bodies use the shared inset; do not pad only the initially
  active tab.

### Information hierarchy

- Configuration owns provider settings and the validated save action.
- Provisioning & linking owns user creation, matching, and role assignment.
- Diagnostics owns tests, previews, validation results, and lifecycle actions.
- A supporting “At a glance” surface may summarize effective source, record
  source, validity, provisioning policy, and latest test.
- Keep supporting surfaces secondary to the editable form.

### Operational states

- Label the effective source explicitly.
- For shadowed records, prefix lifecycle and validity with “Stored” and label
  test information as stored-record data.
- Configuration-owned records are read-only and must not advertise unavailable
  lifecycle actions.
- Draft connections omit persisted-only diagnostics and lifecycle state.
- Keep destructive lifecycle actions separated from routine configuration.

### Calls to action

- Keep one clear primary path for each task.
- When an action changes tabs rather than saving, label it accordingly, such as
  “Review configuration.”
- In informational callouts, stack explanatory copy above the action with an
  8px gap. Do not append a button directly to a sentence.

## Secret Detail Workspace Pattern

The approved reference is the Secrets detail page implemented on 2026-07-30.
Use this pattern for detail pages that combine immutable identity, editable
metadata, lifecycle context, and a sensitive operational action.

### Page composition

- Use `MudContainer MaxWidth="MaxWidth.Large"` and a `MudStack Spacing="3"`
  with `py-4`.
- Keep the back action at natural width with `align-self-start`.
- Present the resource icon, display name, technical name, status, type, store,
  and version in one compact outlined header.
- Use the primary color for resource identity and routine actions. Reserve
  success, warning, and error colors for lifecycle meaning.
- Technical identifiers may be long: use a monospace treatment,
  `overflow-wrap: anywhere`, and wrapping header chips.

### Task separation

- Use an outlined tab workspace when overview and mutation tasks have different
  risk or cognitive load.
- Keep identity, description, metadata, and lifecycle summary under Overview.
- Put credential replacement and optional expiry under Rotation.
- Use `KeepPanelsAlive="true"` and
  `TabPanelsClass="pa-4 pa-sm-6"` so transient form values survive task
  navigation and every panel receives the same responsive inset.
- Use `MudGrid Spacing="4"` with an 8/4 main-to-supporting column split on
  desktop and a single-column stack on narrow screens.

### Operational context

- Provide one secondary “At a glance” outlined surface for status, current
  version, store, and expiry.
- Explain rotation outcomes before the form. Never imply that a stored secret
  value can be retrieved or displayed.
- Pair mutation controls with the current version and expiry context needed to
  evaluate the action.
- Keep routine rotation visually primary but contained within its own task.

### Destructive actions

- Name the action directly, such as “Revoke secret”; avoid a generic
  “Danger zone” heading when a more precise label exists.
- Separate destructive actions from routine mutations by task, spacing, and an
  error-semantic border rather than a decorative background.
- Explain the operational consequence immediately before the destructive
  button.
- Keep the destructive button at natural width and require confirmation before
  executing the action.

### Responsive and theme behavior

- At narrow widths, stack metadata columns, supporting context, date/time
  controls, and action groups without horizontal scrolling.
- Keep short action labels on one line; allow technical names and status chips
  to wrap.
- Use theme tokens and outlined surfaces so the same structure works in light
  and dark modes without page-specific color overrides.
- Verify Overview and Rotation independently at desktop and mobile widths,
  including long technical names, focus order, disabled time-before-date
  behavior, and semantic lifecycle colors.

## Defaults to Avoid

- One uninterrupted settings form: replace with task-oriented tabs while
  preserving editor state.
- Ambiguous source/status badges: replace with effective-versus-stored language.
- Edge-to-edge tab content: replace with the shared responsive panel inset.
- Dashboard-like card grids for ordinary settings: use one primary form and only
  the supporting surfaces needed for context.
- One-off CSS for spacing: use MudBlazor spacing and responsive utilities first.

## Diagnostics Content

- Diagnostics should contain actionable information only: what failed or is at
  risk, why it happened, and what the operator can do next.
- Status summaries and diagnostic details must use the same authoritative
  result. Never show a valid/invalid badge that the Diagnostics view cannot
  explain.
- Omit implementation metadata such as management-contract and backend versions
  unless it directly affects compatibility or the recommended action. Put
  support-only metadata behind progressive disclosure or in an export.
- Keep successful results concise. Show correlation identifiers and other
  troubleshooting metadata as secondary, copyable details rather than primary
  content.

## Review Checklist

- Check desktop and narrow layouts.
- Activate every tab; do not validate only the default tab.
- Confirm panel padding survives MudGrid negative gutters.
- Confirm text actions do not collapse into adjacent prose.
- Confirm read-only, draft, shadowed, archived, loading, validation-error, and
  empty states.
- Confirm custom editor instances survive tab navigation.
- Run the relevant bUnit tests and the full module test suite.
