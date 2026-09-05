# Milestone validation — Elsa Studio Role Permission Management

## M1 — Program and contract foundation

### Promised outcome

Studio has a verified, permission-aware Security module foundation aligned with the live Core contract.

### Demonstration

1. Start Server and WebAssembly hosts against an Identity-enabled Core backend.
2. Confirm the module loads through normal host registration.
3. Sign in as a user with and without `identity/roles:view`.
4. Confirm Roles navigation and direct routes fail closed appropriately.
5. Exercise client-contract tests for all required Identity routes and error shapes.

### Result

Pending.

## M2 — Role authoring

### Promised outcome

An authorized administrator can browse, create, reload, edit, and understand exact and advanced grants without losing stored security data.

### Demonstration

1. Sign in and open Security → Roles.
2. Search the complete list and open a role.
3. Create a role with exact and wildcard grants.
4. Save and reload; verify stored direct grants and derived coverage.
5. Update and reload.
6. Verify unresolved grants block Save and explicit repair persists.
7. Repeat critical interactions at desktop and mobile widths with keyboard navigation.

### Result

Pending.

## M3 — Safe deletion and release proof

### Promised outcome

An authorized administrator can delete a safe role or remediate editable dependencies without bypassing configuration blockers or stale impact.

### Demonstration

1. Delete a dependency-free role after confirmation.
2. Inspect a configuration-blocked role and confirm no mutation is offered.
3. Remediate editable policy references with explicit confirmation and dependency version.
4. Force a version conflict; verify impact refresh and renewed confirmation.
5. Force an incomplete outcome; verify changed and remaining owners and retained role.
6. Verify restricted-user controls and server-side rejection of unauthorized mutations.
7. Record that existing tokens reflect permission changes only after refresh or reissuance.

### Result

Pending.
