# Integrations Module

**Status:** stub — no implementation yet (`IntegrationDtos.cs`, `IntegrationEndpoints.cs`, `IntegrationService.cs`, `IIntegrationService.cs` are all empty).

## Responsibilities

Each third-party integration (Jira, Calendar, Slack, GitHub) is meant to be
its own submodule implementing a shared connector interface, so the frontend
and orchestration code can treat all integrations interchangeably:

```csharp
public interface IModuleConnector
{
    Task<ModuleConfig> GetConfigAsync(Guid teamId);
    Task<ModuleData> GetDataAsync(Guid teamId, DateTime? since = null);
    Task InvokeActionAsync(Guid teamId, string action);
}
```

## Notes

- This interface doesn't exist in code yet — only the generic
  `Integrations/` folder does, with no `Jira`/`Calendar`/`Slack` subfolders.
- Per [docs/ROADMAP.md](../../../docs/ROADMAP.md), build **one** integration
  first (Jira or GitHub) to validate this interface shape before copying the
  pattern to the others.
- Depends on `Teams` (integrations are configured per-team) and should use
  Polly for retry/circuit-breaking per [docs/ARCHITECTURE.md](../../../docs/ARCHITECTURE.md).
