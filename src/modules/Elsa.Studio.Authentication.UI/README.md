# Elsa Studio Authentication UI

This package owns Elsa Studio's `/login` route and renders an explicit chooser from contributed login-method catalogs.

The shell contains no provider-specific behavior. Authentication packages contribute:

- `ILoginMethodCatalog` for safe, enabled method metadata.
- `ILoginMethodComponentProvider` for a component keyed by method kind.
- `ILoginMethodIconProvider` for trusted, locally supplied SVG icons.

The composition root registers `AddAuthenticationUI()`. Provider packages depend only on
`Elsa.Studio.Authentication.Abstractions`; they do not depend on this package.

Preferred methods receive visual emphasis only. The shell never starts a login flow until the
user selects a method. Unknown icons use an accessible local fallback, and remote icon URLs are
not accepted by the icon contract.
