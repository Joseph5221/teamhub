# TeamHub — Architecture

> Condensed from the original "TeamHub High-Level Architecture" document, with
> a reality-check section added below reflecting what's actually in the repo
> as of 2026-08-01. See [docs/adr/0001-modular-monolith-architecture.md](adr/0001-modular-monolith-architecture.md)
> for the decision record.

## Goals

- Modular structure — each domain (Auth, Teams, Integrations, etc.) isolated
  in code, even though everything runs in one process.
- Simple deployment/local dev — one database, one container, one service to
  run.
- Clear module boundaries/APIs so a future split into microservices stays
  possible without a rewrite.
- Single entry point for frontend (Blazor Server) and internal modules.
- Security, observability, and extensibility from day one.

## Major Components

**Frontend** — Blazor Server (component-driven UI, real-time via SignalR),
served from its own ASP.NET Core host. Blazor WebAssembly is a possible future
option if distributed hosting is needed later.

**Backend (`TeamHub.Server`)** — a single ASP.NET Core application containing
feature modules. Each module exposes its own endpoints/service interfaces but
runs under one host:

- **Auth** — authentication, OAuth linking (Google/GitHub/Microsoft), JWT/session issuance.
- **Teams** — team configuration, membership, permissions.
- **Integrations** — each integration (Jira, Calendar, Slack, ...) is a
  submodule implementing a shared connector interface for easy plug-in/out.
- **InfraWatch** — background checks for uptime/cost reporting.
- **Shared** — cross-cutting services: logging, configuration, DTOs.

**Data Layer** — single PostgreSQL database, separate schema per module
(`auth.*`, `team.*`, `integration.*`) for isolation. Optional Redis for
caching/rate-limiting. EF Core with one `DbContext` per module.

**Infrastructure & Tooling** — Docker Compose for local dev, GitHub Actions
for CI/CD, Serilog + OpenTelemetry for logging/tracing, optional
Prometheus/Grafana for metrics, optional Azure Container Apps / single-node
AKS for cloud deployment later.

## Module Interface Contract

Integrations are meant to implement a shared connector interface so the
frontend/orchestration layer can treat all of them interchangeably:

```csharp
public interface IModuleConnector
{
    Task<ModuleConfig> GetConfigAsync(Guid teamId);
    Task<ModuleData> GetDataAsync(Guid teamId, DateTime? since = null);
    Task InvokeActionAsync(Guid teamId, string action);
}
```

## Data Flow Examples

**Team board load**: user loads `/team/{id}` → Blazor Server calls internal
services (`TeamService`, `IntegrationService`) → each module queries its own
schema/cache and returns normalized data → UI renders widgets.

**Jira webhook**: Jira → `Integrations.Jira` controller → normalized into an
internal event (e.g. `IssueUpdatedEvent`) → published to an in-memory event
bus (e.g. MediatR) → `Teams` and `InfraWatch` react to it.

**Background task**: `InfraWatch` runs a periodic hosted background service →
writes metrics to DB/Redis → frontend reads precomputed summaries for fast
dashboard loads.

## Resilience & Testing

- Retry/circuit-breaker via Polly for external integrations; graceful
  fallback to cached data on connector failure.
- Unit tests per module, integration tests across module boundaries (e.g.
  Auth → Teams), end-to-end tests for full user flows.

## Scaling

Runs as a single process/container to start. Each module can be extracted
and scaled independently later if needed — that's the whole point of keeping
module boundaries clean now.

---

## Reality check — where the code has diverged from this plan

This section exists so future you (or an AI assistant) doesn't assume the code
matches the plan above just because the plan document exists. It's kept
current against the actual repo state — last updated 2026-08-01.

The actual layout is:

```
server/TeamHub.Server/
  Dashboard/        Domain/          Extensions/       Features/Auth/
  Infrastructure/    Integrations/    Middleware/       Projects/
  Security/          Services/        Teams/            Users/
```

Differences from the planned architecture doc:

1. **No `/Modules` nesting.** The plan called for
   `/Modules/{Auth,Teams,Integrations/{Jira,Calendar,Slack},InfraWatch}`.
   The actual repo uses flat, top-level feature folders instead
   (`Auth` lives under `Features/`, everything else is a top-level folder).
   This is a reasonable, common alternative ("vertical slice" folder-per-feature)
   but it's a real deviation, not just a naming difference — decide
   deliberately whether to restructure into `/Modules` or formally adopt the
   flat layout (see [ADR 0001](adr/0001-modular-monolith-architecture.md)).
2. **Extra `Users` and `Projects` modules** exist in code but aren't in either
   planning document. `Projects` presumably maps to Jira-style issue tracking
   and `Users` overlaps with `Auth` — worth clarifying the boundary between
   them before building either out further.
3. **No `InfraWatch`, `Jira`, `Calendar`, or `Slack` folders yet** — only a
   single generic `Integrations` folder exists, unimplemented.
4. **`IModuleConnector` doesn't exist in code yet.** No interface unifies the
   integrations.
5. **`Auth` and `Dashboard` are now real implementations.**
   Login/register issue JWTs, `GET /api/dashboard` returns user info +
   per-integration TODO status + team/project/integration counts. Auth is
   explicitly dev-only right now: **login accepts any password**, and
   register stores the raw password string as `PasswordHash` with no
   hashing — both called out as `TODO` in `AuthService.cs`. Don't treat this
   as real auth; it needs `IPasswordHasher` (still a stub) before it's
   anything but a placeholder.
6. **`Teams`, `Projects`, `Integrations`, `Users` are still empty stubs.**
   Their `I<Module>Service`/`<Module>Service`/`<Module>Endpoints`/`<Module>Dtos`
   files remain 0 bytes, and nothing in `Program.cs` maps their endpoints
   (commented out: `// app.MapTeamEndpoints();` etc.).
7. **The server project compiles.**
   `dotnet build teamhub.sln` succeeds with 0 errors. `TeamHub.Server.csproj`
   references EF Core (`InMemory`, `Sqlite`, `Design`) and
   `Microsoft.AspNetCore.Authentication.JwtBearer`. `Program.cs` wires up
   `AddInfrastructure`/`AddFeatures`/`AddApiDocumentation` and maps
   `MapAuthEndpoints`/`MapDashboardEndpoints`. The leftover `/weatherforecast`
   minimal API endpoint and the unused `WebApplicationExtensions`
   (`InitializeDatabaseAsync`/`UseApiMiddleware`, duplicating what
   `Program.cs` did inline) have been removed — `AddApiDocumentation`
   (previously also unused, but more complete than the inline Swagger setup
   it duplicated — it adds the JWT bearer scheme to Swagger UI) is now the
   one actually wired up.
8. **The database is in-memory, not PostgreSQL — now a deliberate, written-
   down decision**, not just an observed gap. See
   [ADR 0002](adr/0002-in-memory-database-for-now.md).
   `ServiceCollectionExtensions.AddInfrastructure` calls
   `options.UseInMemoryDatabase("TeamHubDb")` with the Sqlite/Postgres path
   commented out — data resets every restart, no migrations exist.
   `Infrastructure/Data/Configurations/*.cs` (EF entity configs) were empty
   stubs until this session — that was an actual bug, not just a gap:
   `ApplyConfigurationsFromAssembly` picked up nothing, so EF Core couldn't
   resolve `Team.Owner`/`Team.Members`/`User.Teams` and threw at startup.
   Now filled in with the minimal Fluent API config needed. `DbInitializer.cs`
   is still an empty stub (seeding happens inline in `AppDbContext.OnModelCreating`
   instead).
9. **The app will not start without a JWT secret configured — by design.**
   `AddInfrastructure` throws `InvalidOperationException` at startup if
   `Jwt:Secret`/`Issuer`/`Audience` are empty — and they *are* empty in both
   `appsettings.json` and `appsettings.Development.json` (checked into git
   as blank strings, which is the right way to avoid committing a real
   secret). `scripts/setup-dev.sh` now sets these via `dotnet user-secrets`
   automatically; confirmed by actually running `dotnet run` and exercising
   `register` → `login` → `/api/dashboard` end-to-end (see item 12 below for
   what it took to get `dotnet run` working at all).
10. **`docker-compose.yml` and `scripts/*.sh` now match the real repo
    layout.** The `api` service builds from `./server/TeamHub.Server` and
    `frontend` from `./frontend/BlazorApp` (a real `Dockerfile` was added
    there, mirroring `frontend/BlazorApp/Dockerfile`). The old `teamhub-frontend`
    Node/Vite service — which never matched this repo (the real frontend is
    Blazor Server, not Node) — is gone. `scripts/start-dev.sh`,
    `scripts/run-tests.sh`, and `scripts/reset-db.sh` were updated to match;
    `reset-db.sh` is now a no-op explainer since there's nothing to migrate
    against an in-memory database. Not smoke-tested against a real `docker
    compose up --build` (no Docker daemon available in the session that made
    these changes) — verify that before trusting it fully.
11. **`docker-compose.yml` now only needs a `JWT_SECRET` env var**
    (`${JWT_SECRET}`) — `DB_PASSWORD` was removed along with the Postgres
    `db` service (see item 8). A root `.env.example` is checked in covering
    `JWT_SECRET`; copy it to `.env` before `docker compose up`.
12. **This machine had no .NET 8.0 runtime — now resolved, see
    [ADR 0003](adr/0003-target-net8-and-install-the-runtime.md).**
    `dotnet build` always succeeded (build only needs reference assemblies),
    but `dotnet run`/`dotnet test` failed with a framework-version mismatch
    until a .NET 8 runtime was installed (`brew install dotnet@8` on this
    machine). Note the Homebrew keg-only quirk documented in that ADR: the
    installed `dotnet@8` doesn't automatically become the default `dotnet`
    on a machine that already has a different one earlier on `PATH`, so
    `scripts/setup-dev.sh`/`start-dev.sh`/`run-tests.sh` accept a `DOTNET=`
    override pointing at it directly. This was the first time `dotnet run`
    had actually succeeded on any machine this project had been touched on
    — which is how the EF configuration bug in item 8 was found.
13. **Frontend** (`BlazorApp`) still builds. It has a real landing page
    (`Components/Pages/Landing.razor`) referencing `/dashboard` and `/login`
    routes that don't exist yet, plus the default Blazor template pages
    (`Counter.razor`, `Weather.razor`) still present and unremoved. Nothing
    in the frontend calls the new `/api/auth/*` or `/api/dashboard`
    endpoints yet — frontend and backend haven't been connected.
14. **A second, more detailed `server/TeamHub.Server/README.md`** exists
    with its own quickstart, API docs, and "next steps" list. Treat it as
    the dev-quickstart doc for that one project (how to run it, test
    credentials, endpoint list) and this file / `docs/ROADMAP.md` as the
    whole-repo planning view — don't let the two next-steps lists drift
    apart; update both when priorities change.

None of this is unusual for a project mid-restart — real progress has
happened (working Auth/Dashboard with JWT, now actually verified end-to-end,
not just built), alongside some rougher edges (Teams/Projects/Integrations/
Users still stubs, dev-only Auth) typical of AI-assisted or late-night
scaffolding that didn't get a final pass. The environment-reconciliation work
(JWT secret setup, .NET runtime, docker-compose/scripts paths, in-memory-DB
decision) is done — see [ADR 0002](adr/0002-in-memory-database-for-now.md) and
[ADR 0003](adr/0003-target-net8-and-install-the-runtime.md). "Next steps"
should prioritize password hashing before adding new features. See
[ROADMAP.md](ROADMAP.md).
