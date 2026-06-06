# Elsa.Studio

This is a bundle package that includes the following packages:

- Elsa.Studio.Dashboard
- Elsa.Studio.Workflows
- Elsa.Studio.Diagnostics.ConsoleLogs
- Elsa.Studio.Diagnostics.OpenTelemetry
- Elsa.Studio.Diagnostics.StructuredLogs
- Elsa.Studio.Secrets
- Elsa.Studio.Shell

The dashboard package provides the shell. Workflow and diagnostics dashboard widgets are registered by their companion modules only when the selected backend advertises the matching dashboard shell features.
