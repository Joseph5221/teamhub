# Integrations Module

**Status:** implemented and verified — generic CRUD + `IModuleConnector`
dispatch, with one real connector (GitHub) proving the shape out. See
`GitHub/README.md` for the connector itself.

## Responsibilities

Each third-party integration (GitHub, Jira, Calendar, Slack) is its own
submodule under `Integrations/<Name>/` implementing the shared
`IModuleConnector` interface (`IModuleConnector.cs`), so `IntegrationService`
and the frontend can treat all integrations interchangeably:

```csharp
public interface IModuleConnector
{
    IntegrationType Type { get; }
    Task<ModuleConfig> GetConfigAsync(Guid teamId);
    Task<ModuleData> GetDataAsync(Guid teamId, DateTime? since = null);
    Task InvokeActionAsync(Guid teamId, string action);
}
```

`ModuleConfig`/`ModuleData`/`ModuleDataItem` (also in `IModuleConnector.cs`)
are the normalized shapes every connector returns — generic enough to cover
GitHub repos today and Jira issues/Slack messages later without a frontend
rewrite per connector.

## What's here

- `IIntegrationService`/`IntegrationService` — CRUD for a team's configured
  integrations (`Integration` entity), owner-only for
  create/update/delete, any member for read + triggering data/action calls
  (mirrors `Modules/Teams`' owner/member split). Dispatches
  `GetDataAsync`/`InvokeActionAsync` to the right connector by
  `Integration.Type`, via connectors registered in DI as `IModuleConnector`
  (`IntegrationService` builds a `Type → connector` lookup from
  `IEnumerable<IModuleConnector>` in its constructor — no switch statement
  to maintain as connectors are added).
- `IntegrationEndpoints` — REST endpoints under
  `/api/teams/{teamId}/integrations`, see
  `server/TeamHub.Server/README.md` for the full route list.
- `IntegrationDtos` — request/response records. `ConfigurationData` (which
  may hold secrets like a PAT) is never echoed back; responses only expose
  `IsConfigured`.
- `ModuleConnectorException` — thrown by a connector for expected,
  user-facing failures (bad config, upstream API error); `IntegrationService`
  catches it and turns it into a `Result` failure (and marks the integration
  `Failed`) instead of a 500.
- `GitHub/` — the first real connector. See `GitHub/README.md`.

## Adding the next connector (Jira, Slack, Calendar)

Per `docs/ROADMAP.md`, GitHub was built first specifically to validate this
shape before copying the pattern. To add another:

1. New folder `Integrations/<Name>/` with an `I<Name>ApiClient`/`<Name>ApiClient`
   typed HTTP client wrapping the third-party REST API, a
   `<Name>Connector : IModuleConnector`, and DTOs for that API's JSON shape.
2. Register the typed client with Polly retry/circuit-breaker and the
   connector as `services.AddScoped<IModuleConnector, <Name>Connector>()` in
   `ServiceCollectionExtensions.AddFeatures` — copy the GitHub block.
3. Nothing else changes — `IntegrationService`/`IntegrationEndpoints` pick
   the new connector up automatically via DI once it's registered.

## Notes

- Depends on `Teams` (integrations are configured per-team, permission
  checks mirror `TeamService`).
- Once a real connector exists for a type, it should surface its data
  through `Modules/Projects` rather than inventing its own project concept
  — see [ADR 0006](../../../../docs/adr/0006-projects-definition.md). Not
  done yet for GitHub — `Projects` is still a stub.
