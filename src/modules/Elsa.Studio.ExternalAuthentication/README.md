# Elsa.Studio.ExternalAuthentication

This module provides the shared login-method chooser and administration UI for Elsa Server's External Authentication broker. Host-specific packages provide the credential boundary:

- `Elsa.Studio.ExternalAuthentication.BlazorServer` uses a confidential Authentication Client, exchanges codes on the server, retains refresh credentials server-side, and establishes an HTTP-only browser session.
- `Elsa.Studio.ExternalAuthentication.BlazorWasm` uses a public Authentication Client with mandatory PKCE. Credentials are memory-only by default; session or durable browser storage is an explicit warned deployment option.

Brokered authentication is opt-in. Existing direct `Elsa.Studio.Authentication.OpenIdConnect` behavior remains available and unchanged. A Studio host selects exactly one mode through `Authentication:Provider`; direct and brokered OpenID Connect must not be enabled together.

The broker-local login endpoint is also additive. It does not replace Elsa's existing direct `/identity/login` or refresh contracts.

See [the Studio migration guide](../../../docs/migrations/external-authentication.md) for setting mappings, rollout, and rollback.

## Routes and permissions

The shared module contributes:

- `/login` for the accessible Login Method chooser.
- `/security/external-authentication` for connection management.
- `/security/external-authentication/identity-links` for tenant-scoped prelink and unlink operations.
- `/security/external-authentication/sessions` for optional session list and revocation.

Menus and buttons are permission-gated usability affordances; Elsa API authorization remains authoritative. Connection test and Preview Sign-in use the current revision. Preview opens in a separate tab and returns a one-time redacted result without creating a user, link, credential, or normal session.

## Management contract seam

The Studio module owns host-local Refit contracts under `Client/` so the independently versioned Studio packages can consume the frozen External Authentication REST shape. Generated `Elsa.Api.Client` resources expose the equivalent Core contract to other .NET clients. The Studio models intentionally mirror descriptor fields, adapter capabilities, connection operations, identity links, and session metadata.

Studio sends no secret values and displays neither secret values nor binding references on read-only views. Preview, test, and session models contain only the redacted fields documented by the Core REST contract.
