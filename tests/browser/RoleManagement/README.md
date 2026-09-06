# Role-management browser tests

This suite is a black-box Playwright suite for the role-management journey. It
does not start Studio or Core and it does not use a fake backend. Start an
isolated current-main Core host and the matching Studio host first:

- Server Studio (`https://localhost:7113`) against Modular Core
  (`https://localhost:7294/elsa/api`).
- WebAssembly Studio (`https://localhost:7052`) against Classic Core
  (`https://localhost:5001/elsa/api`).

The four projects per host cover 320, 768, 1024, and 1440 pixel viewports.
Only local development URLs are accepted. The browser context and API context
are ephemeral; no Playwright `storageState`, trace, video, screenshot, cookie,
token, or credential artifact is written. Console/network diagnostics retain
counts only, never message text, headers, bodies, or query strings.

## Host topology

Run each host in its own terminal from a clean, isolated development database.
The launch profiles provide the ports below; the Studio backend settings use
the `/elsa/api` prefix shown here.

```bash
# Current-main Core + Server Studio
dotnet run --project /Users/sipke/Projects/Elsa/elsa-core-main/src/apps/Elsa.ModularServer.Web/Elsa.ModularServer.Web.csproj --launch-profile https
dotnet run --project /Users/sipke/.codex/worktrees/role-lifecycle-browser-989/src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj --launch-profile https

# Current-main Core + WebAssembly Studio (use a separate Core database)
dotnet run --project /Users/sipke/Projects/Elsa/elsa-core-main/src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj --launch-profile Elsa.WorkflowServer.Web
dotnet run --project /Users/sipke/.codex/worktrees/role-lifecycle-browser-989/src/hosts/Elsa.Studio.Host.Wasm/Elsa.Studio.Host.Wasm.csproj --launch-profile https
```

The resulting default URLs are Server Studio `https://localhost:7113` →
Modular Core `https://localhost:7294/elsa/api`, and WebAssembly Studio
`https://localhost:7052` → Classic Core `https://localhost:5001/elsa/api`.
Do not copy development bootstrap credentials from Core configuration into the
test environment; inject a dedicated actor through the operator's secret
channel instead. If either Studio host has a local appsettings override, set
its backend URL to the matching current-main Core URL before starting it.

## Run

Provide actor credentials through an operator-controlled secret channel. Do not
put them in shell history, committed files, test titles, or CI logs. The suite
requires an administrator with role view/create/update/delete and permission
catalog access:

```bash
cd tests/browser/RoleManagement
npm install
npx playwright install chromium

# Inject these values from the local secret manager or an interactive shell:
# ROLE_E2E_ADMIN_USERNAME, ROLE_E2E_ADMIN_PASSWORD
# ROLE_E2E_SERVER_STUDIO_URL, ROLE_E2E_SERVER_BACKEND_URL
# ROLE_E2E_WASM_STUDIO_URL, ROLE_E2E_WASM_BACKEND_URL
npm test
```

For a single host, set only its matching Studio/Core URL pair. Supplying both
pairs runs both topologies and all four viewport projects. To exercise the
real load-error recovery path, open Roles, stop only the matching Core process
with Ctrl-C in its own terminal, reload and verify the `Roles could not be
loaded` card, restart that same command, then activate `Try again`. This is a
manual gate because intercepting a successful browser response would no longer
be a real-host proof.

For restricted-user proof, also inject
`ROLE_E2E_RESTRICTED_USERNAME` and `ROLE_E2E_RESTRICTED_PASSWORD`. The suite
checks read-only rendering and sends a create request with that actor to prove
Core returns 403; the response body is never read or logged.

## Optional deletion fixtures

The current Core hosts do not ship deterministic configuration-blocked,
editable-remediation, or incomplete-remediation role fixtures. The suite
therefore skips these cases unless the operator supplies IDs from a dedicated
isolated fixture database:

- `ROLE_E2E_BLOCKED_ROLE_ID`: configuration-owned JIT policy reference; the
  test verifies the blocker and absence of an apply action.
- `ROLE_E2E_REMEDIABLE_ROLE_ID`: database-owned editable JIT policy references;
  the test submits the inspected dependency version and confirmations.
- `ROLE_E2E_CONFLICT_ROLE_ID` plus
  `ROLE_E2E_CONFLICT_TRIGGER_URL`: a local-only fixture hook that changes a
  dependency revision after impact inspection and before submit.
- `ROLE_E2E_INCOMPLETE_ROLE_ID` plus
  `ROLE_E2E_INCOMPLETE_TRIGGER_URL`: a local-only fixture hook that causes a
  contributor failure or remaining dependency during remediation.
- `ROLE_E2E_UNRESOLVED_ROLE_ID`: a database-seeded role containing a legacy
  grant that the current catalog cannot resolve.
- `ROLE_E2E_REQUIRE_UNVERIFIED_CATALOG=true`: require the current Core catalog
  to expose at least one `verified:false` descriptor and verify that it can be
  selected.

Trigger URLs must be `localhost`, `127.0.0.1`, or `::1`. The test posts only the
role ID and never adds an authorization header. A trigger is a harness seam,
not a production Core endpoint; do not weaken production code to make these
states appear. Without a repeatable local hook, record conflict/incomplete as
unproven rather than substituting an intercepted response.

Core rejects unknown concrete permissions on both create and update, so an
unresolved-grant role cannot be created through the public API. It must be
seeded in the isolated fixture database (or supplied by a non-production Core
fixture tool); the test preserves and then explicitly replaces the grant. The
current Modular and Classic sample hosts do not register a `verified:false`
descriptor by default. That gate is therefore opt-in and remains a visible
skip until a fixture host provides one.

Ordinary test roles are created with a short-lived in-memory Core API session
and deleted in fixture teardown. If a host is interrupted, remove only the
explicitly named isolated fixture role/database; do not clean broad paths or a
shared development database.

## Scope of evidence

The suite exercises the real Studio DOM and observes the role API methods and
status codes without reading response bodies containing authentication data.
It covers:

- create, save, reload, update, reload, safe impact, and 204 delete;
- exact and wildcard grants, visible token-refresh messaging;
- restricted rendering and server-side create rejection;
- keyboard focus, axe serious/critical checks, and responsive list layout;
- opt-in configuration blocking, editable remediation, dependency conflict,
  incomplete-retention outcomes, and unresolved-grant repair.

The existing External Authentication browser suite remains separate:
`tests/browser/ExternalAuthentication`.

## Coverage and known gates

| Required behavior | Harness proof | Default state |
| --- | --- | --- |
| Server and WASM against current Core | host-specific base URL and API URL projects | Runnable when local hosts and admin actor are injected |
| Authorized lifecycle | real UI create/update/delete; observed Core `200`/`204` responses | Covered |
| Exact, category bulk, wildcard, reach | category action, sibling advanced tab, wildcard reach card | Covered |
| Search, empty, responsive, keyboard, accessibility | real list search, filtered-empty card, four widths, focus and axe | Covered |
| Restricted rendering and mutation authorization | restricted browser session plus separate restricted Core API request expecting `403` | Requires restricted actor |
| Safe deletion and token-refresh note | impact modal, confirmation, `204`, refresh/reissuance wording | Covered |
| Configuration blocker | isolated role ID referenced by startup configuration | Opt-in; skipped until seeded |
| Editable remediation | isolated database policy references, explicit checkboxes, `204` remediation | Opt-in; skipped until seeded |
| Conflict / incomplete retention | local-only post-inspection fixture hook | Opt-in; skipped until a repeatable hook exists |
| Unresolved legacy repair | isolated DB-seeded role; original text, disabled save, explicit replacement, persisted reload | Opt-in; Core public API cannot seed it |
| `verified:false` catalog entry | isolated Core catalog descriptor; marker and selectable checkbox | Opt-in; sample hosts have none |
| Core outage/error and retry | operator stop Core after navigation, observe error card, restart Core, click Try again | Manual runbook gate; no network interception is accepted |
| Identity feature absent | no supported toggle in shipped Server/Modular or WASM/Classic hosts | Blocked until a separate host composition omits Identity; restricted direct-route proof remains covered |

Skipped optional tests are intentional failed gates for release review, not
passes. A complete M3 run requires recording an explicit fixture ID/hook and
the resulting observed status for each optional row. Actual permission claim
change for an already issued token is an operator-controlled follow-up: use a
second ephemeral browser context, reissue/refresh through the host, and verify
the effective permission without recording a token, cookie, URL query, or
response body.
