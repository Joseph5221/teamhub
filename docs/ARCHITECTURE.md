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
current against the actual repo state — last updated 2026-08-01 against
commit `fa4bee7` ("Setting up testing framework"), which is two commits ahead
of the `1b3079f` snapshot this section originally described (a `git pull` was
missed before the first pass at this doc — see revision note at the bottom).

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
5. **`Auth` and `Dashboard` are now real implementations** (added in commit
   `2d2bc51`, "Create functionality for error/result/dashboard/auth/JWT").
   Login/register issue JWTs, `GET /api/dashboard` returns user info +
   per-integration TODO status + team/project/integration counts. Auth is
   explicitly dev-only right now: **login accepts any password**, and
   register stores the raw password string as `PasswordHash` with no
   hashing — both called out as `TODO` in `AuthService.cs`. Don't treat this
   as real auth; it needs `IPasswordHasher` (still a stub) before it's
   anything but a placeholder.
6. **`Teams`, `Projects`, `Integrations`, `Users` are still empty stubs** —
   the January 19 commit only touched `Auth` and `Dashboard`. Their
   `I<Module>Service`/`<Module>Service`/`<Module>Endpoints`/`<Module>Dtos`
   files remain 0 bytes, and nothing in `Program.cs` maps their endpoints
   (commented out: `// app.MapTeamEndpoints();` etc.).
7. **The server project compiles now** (it didn't as of `1b3079f`).
   `dotnet build teamhub.sln` succeeds with 0 errors. `TeamHub.Server.csproj`
   now references EF Core (`InMemory`, `Sqlite`, `Design`) and
   `Microsoft.AspNetCore.Authentication.JwtBearer`. `Program.cs` is no
   longer the default weather-forecast template — it wires up
   `AddInfrastructure`/`AddFeatures` and maps `MapAuthEndpoints`/
   `MapDashboardEndpoints` — though the leftover `/weatherforecast` minimal
   API endpoint is still present too and should be deleted.
8. **The database is in-memory, not PostgreSQL**, despite the plan and
   `docker-compose.yml`'s `db` service both assuming Postgres.
   `ServiceCollectionExtensions.AddInfrastructure` calls
   `options.UseInMemoryDatabase("TeamHubDb")` with the Sqlite/Postgres path
   commented out — data resets every restart, no migrations exist, and
   `Infrastructure/Data/Configurations/*.cs` (EF entity configs) and
   `DbInitializer.cs` are still empty stubs. `WebApplicationExtensions.cs`
   (added in the same commit) defines `InitializeDatabaseAsync` and
   `UseApiMiddleware` helpers that would call `EnsureCreatedAsync` and wire
   up CORS/Swagger/auth — but neither is actually called from `Program.cs`,
   which duplicates the Swagger/auth setup inline instead. That's dead code
   right now, not a bug, but worth resolving (delete or wire up) rather than
   leaving both versions around.
9. **The app will not start without a JWT secret configured.**
   `AddInfrastructure` throws `InvalidOperationException` at startup if
   `Jwt:Secret`/`Issuer`/`Audience` are empty — and they *are* empty in both
   `appsettings.json` and `appsettings.Development.json` (checked into git
   as blank strings, which is the right way to avoid committing a real
   secret — but it means the app cannot run until you set them locally via
   `dotnet user-secrets` or environment variables). Confirmed by actually
   running `dotnet run` — it built, then... (see point 12).
10. **Two solution files now exist and disagree.** Root `teamhub.sln`
    includes `BlazorApp`, `Shared`, and `TeamHub.Server` (builds clean, 0
    errors). A second `server/TeamHub.sln` (added in `fa4bee7`) includes
    *only* `TeamHub.Server` — the two new test projects,
    `TeamHub.Server.Tests` and `TeamHub.Server.IntegrationTests`, aren't
    referenced by *either* `.sln`. Pick one canonical solution file (root is
    the natural choice since it covers frontend+backend+shared) and either
    delete `server/TeamHub.sln` or add the test projects to it.
11. **Both new test projects fail to compile.** `TeamHub.Server.Tests.csproj`
    and `TeamHub.Server.IntegrationTests.csproj` have no
    `<ProjectReference>` to `TeamHub.Server.csproj`, so `AuthServiceTests.cs`
    can't resolve `AuthService`/`AppDbContext`/`ITokenService`, and
    `IntegrationTests.csproj` is additionally missing the
    `Microsoft.AspNetCore.Mvc.Testing` package needed for
    `WebApplicationFactory<Program>`. Confirmed via
    `dotnet test TeamHub.Server.Tests` and `dotnet test
    TeamHub.Server.IntegrationTests` from `server/` — both fail at compile,
    not at assertion. `scripts/run-tests.sh` will currently fail outright.
12. **`docker-compose.yml` and `scripts/*.sh` were rewritten to reference a
    repo layout that doesn't exist.** The new compose file builds an `api`
    service from `./TeamHub.Server` (actual path: `server/TeamHub.Server`)
    and a `frontend` service from `./teamhub-frontend` running `npm run
    dev`/Vite on port 3000 (there is no `teamhub-frontend` directory — the
    real frontend is `frontend/BlazorApp`, a Blazor Server app, not a Node
    project). `scripts/start-dev.sh` and `scripts/reset-db.sh` have the same
    assumption (`cd TeamHub.Server`, `cd teamhub-frontend`). This reads like
    boilerplate generated against a different/hypothetical project layout
    (or a planned React rewrite) that was never reconciled with this repo's
    actual structure — `docker compose up --build` will fail outright. Needs
    a decision: fix the paths to match the real repo (and drop the
    Node/Vite assumption unless a frontend rewrite is actually planned), or
    intentionally restructure the repo to match. Either way, write it down.
13. **`docker-compose.yml` now expects `JWT_SECRET` and `DB_PASSWORD` env
    vars** (`${JWT_SECRET}`, `${DB_PASSWORD}`) with no `.env` or
    `.env.example` checked in — `docker compose up` will substitute empty
    strings today.
14. **This machine's installed .NET runtime is 10.0.101 (SDK) / 10.0.1
    (runtime) only — no .NET 8.0 runtime.** `dotnet build` succeeds (build
    only needs reference assemblies), but `dotnet run` on `TeamHub.Server`
    fails immediately with "You must install or update .NET to run this
    application" (framework version mismatch, confirmed by actually running
    it). This may be specific to whatever machine you're reading this on —
    check `dotnet --list-runtimes` before assuming it's fixed — but if it's
    also true on your main dev machine, either install the .NET 8.0 runtime
    or retarget the projects to `net10.0` (a deliberate decision either way,
    not a silent fix).
15. **Frontend** (`BlazorApp`) still builds. It has a real landing page
    (`Components/Pages/Landing.razor`) referencing `/dashboard` and `/login`
    routes that don't exist yet, plus the default Blazor template pages
    (`Counter.razor`, `Weather.razor`) still present and unremoved. Nothing
    in the frontend calls the new `/api/auth/*` or `/api/dashboard`
    endpoints yet — frontend and backend haven't been connected.
16. **A second, more detailed `server/TeamHub.Server/README.md`** was added
    in `2d2bc51` with its own quickstart, API docs, and "next steps" list.
    Treat it as the dev-quickstart doc for that one project (how to run it,
    test credentials, endpoint list) and this file / `docs/ROADMAP.md` as
    the whole-repo planning view — don't let the two next-steps lists drift
    apart; update both when priorities change.

None of this is unusual for a project mid-restart — a decent chunk of real
progress happened in the two commits pulled in here (working Auth/Dashboard
with JWT), alongside some rougher edges (broken tests, mismatched
compose/scripts, in-memory DB standing in for Postgres) typical of
AI-assisted or late-night scaffolding that didn't get a final pass. "Next
steps" should prioritize reconciling the environment (JWT secret, .NET
runtime, docker-compose paths) and fixing the test projects before adding
new features. See [ROADMAP.md](ROADMAP.md).

<sub>Revision note: this section originally described commit `1b3079f` (the
"Setting up new file structure" commit) as HEAD, written before a `git pull`
that brought in `2d2bc51` and `fa4bee7`. Updated 2026-08-01 once the missed
pull was caught. Kept the original numbered points that are still true;
renumbered and added new ones for what changed.</sub>
