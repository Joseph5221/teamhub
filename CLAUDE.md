# CLAUDE.md

Guidance for Claude Code (or any AI assistant) working in this repository.

## What this is

TeamHub is a team dashboard (modular monolith) unifying Jira, Calendar,
GitHub, CI/CD, and infra-monitoring data into one Blazor Server UI. It's
early-stage / restarting after a hiatus — read the "Current State" section
below before assuming anything works.

Full context lives in `docs/`:
- [docs/PROJECT_OVERVIEW.md](docs/PROJECT_OVERVIEW.md) — product vision, features, target users.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — technical architecture, **including a "reality check" section on where the code diverges from the original plan.**
- [docs/adr/](docs/adr/) — Architecture Decision Records (why key decisions were made).
- [docs/ROADMAP.md](docs/ROADMAP.md) — current status and next steps. **Check this first** — it's the fastest way to know what's actually done vs. stubbed.

## Current State (as of 2026-08-01, commit `fa4bee7`) — read before coding

- `frontend/BlazorApp` builds and runs. `shared/Shared` builds.
- **`server/TeamHub.Server` now compiles** (`dotnet build teamhub.sln` — 0
  errors) and **Auth + Dashboard are implemented**: `POST /api/auth/login`,
  `POST /api/auth/register`, and `GET /api/dashboard` (JWT-protected) all
  work. `Teams`, `Projects`, `Integrations`, `Users` are still empty stubs —
  don't assume a method has behavior just because the file exists; check
  line count / open it.
- **Auth is dev-only, not real auth yet**: login accepts *any* password, and
  registration stores the password unhashed. Both explicitly `TODO` in
  `AuthService.cs` — don't build anything security-sensitive on top of this
  until `IPasswordHasher` is implemented.
- **The app won't start without a JWT secret.** `Jwt:Secret`/`Issuer`/
  `Audience` are blank in both `appsettings*.json` (intentionally — don't
  commit real secrets) and `AddInfrastructure` throws at startup until you
  set them: `dotnet user-secrets set "Jwt:Secret" "<dev value>"` (and
  `Issuer`/`Audience`) from `server/TeamHub.Server/`.
- **This machine has no .NET 8 runtime** — only `10.0.101` SDK / `10.0.1`
  runtime are installed, so `dotnet build` succeeds but `dotnet run` fails
  with a framework-mismatch error. Run `dotnet --list-runtimes` to check
  before assuming this is fixed on whatever machine you're on.
- **Database is in-memory** (`UseInMemoryDatabase`), not PostgreSQL, despite
  `docker-compose.yml` running a Postgres `db` service — data resets every
  restart, no migrations exist yet.
- **Both test projects now compile and build clean from `teamhub.sln`** —
  `TeamHub.Server.Tests` and `TeamHub.Server.IntegrationTests` had a missing
  `<ProjectReference>` to `TeamHub.Server.csproj` (fixed), the integration
  tests were also missing `Microsoft.AspNetCore.Mvc.Testing` and
  `FluentAssertions` (fixed), and `Program.cs` needed `public partial class
  Program { }` for `WebApplicationFactory<Program>` to see it (fixed).
  `dotnet build teamhub.sln` is 0 errors across all 5 projects. `dotnet
  test` still can't *run* on this machine — same .NET 8 runtime gap noted
  below, not a compile issue.
- **Only one solution file now**: `server/TeamHub.sln` has been deleted.
  `teamhub.sln` at the repo root is canonical and includes everything —
  `BlazorApp`, `Shared`, `TeamHub.Server`, and both test projects.
- **`docker-compose.yml` and `scripts/*.sh` reference paths that don't exist
  in this repo** (`./TeamHub.Server`, and a `./teamhub-frontend` Node/Vite
  service — the real frontend is `frontend/BlazorApp`, Blazor Server, not
  Node). `docker compose up --build` will fail. Don't trust these files at
  face value; see `docs/ARCHITECTURE.md` reality-check items 12–13 before
  touching Docker setup.
- Before claiming any server-side task "done," run `dotnet build teamhub.sln`
  (and, once fixed, `dotnet test`) and confirm both are clean.

Full detail: `docs/ARCHITECTURE.md` § "Reality check" (16 numbered items) and
`docs/ROADMAP.md` (sequenced next steps) — both updated same day as this file.

## Tech Stack

- **Backend**: ASP.NET Core (.NET 8, targets `net8.0`; SDK on this machine is `10.0.101`, no matching runtime — see above), Minimal APIs.
- **Frontend**: Blazor Server (interactive server render mode, SignalR).
- **Auth**: JWT bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`), dev-mode-only validation (see Current State).
- **Database**: EF Core, currently `UseInMemoryDatabase` for dev. `Sqlite` and `Design` packages are referenced but unused; PostgreSQL is the eventual planned target (per `docs/ARCHITECTURE.md`) but nothing points at it yet.
- **Testing**: xUnit + FluentAssertions + Moq (`TeamHub.Server.Tests`), xUnit + `WebApplicationFactory` (`TeamHub.Server.IntegrationTests`) — both compile and build from `teamhub.sln` now; running them is blocked by the missing .NET 8 runtime, see above.
- **Planned but not yet added**: MediatR (in-process event bus), Serilog + OpenTelemetry, Polly (resilience), real password hashing.

## Repo Layout

```
teamhub.sln                    Root solution, the only one — BlazorApp + Shared + TeamHub.Server + both test projects (builds clean)
docker-compose.yml              References paths that don't match this repo — do not trust as-is
scripts/                        start-dev.sh / reset-db.sh / run-tests.sh — same path mismatch issue
docs/                          Planning & architecture docs (see above)
frontend/BlazorApp/            Blazor Server frontend
server/TeamHub.Server/         ASP.NET Core backend — feature-folder modules:
  Features/Auth/                 login/register/JWT — implemented (dev-mode only)
  Dashboard/                     user info + integration status + counts — implemented
  Teams/                         team config, membership, permissions (stub)
  Users/                         user profile data — boundary vs Auth undecided (stub)
  Projects/                      not in original plan — purpose undecided (stub)
  Integrations/                  Jira/Calendar/Slack/GitHub connectors (stub, no submodules yet)
  Domain/                        entities, enums, Result<T>/Error pattern — implemented
  Infrastructure/                EF Core DbContext (in-memory), JWT token service — implemented; security/email helpers still stub
  Extensions/                    ServiceCollectionExtensions (wired up) + WebApplicationExtensions (written but unused dead code — see Current State)
  Middleware/                    exception/logging middleware (stub, unused)
server/TeamHub.Server.Tests/            unit tests — currently fails to compile
server/TeamHub.Server.IntegrationTests/ integration tests — currently fails to compile
shared/Shared/                 Shared types between frontend/backend (near-empty)
```

Each module folder has its own `README.md` with status + responsibilities —
check it before working in that folder. Note the actual layout is flatter
than what the original architecture doc proposed (no `/Modules/<Domain>`
nesting) — see the "reality check" section in `docs/ARCHITECTURE.md`.
`server/TeamHub.Server/README.md` is also a real, more detailed dev-quickstart
doc for that project specifically (test credentials, endpoint list) — keep it
and `docs/ROADMAP.md` in sync when priorities change.

## Build & Run

```bash
# Build everything (works — 0 errors)
dotnet build teamhub.sln

# Run frontend only (works)
dotnet run --project frontend/BlazorApp

# Run backend — needs a JWT secret set first (see Current State), and a
# matching .NET 8 runtime installed (check `dotnet --list-runtimes`)
cd server/TeamHub.Server
dotnet user-secrets set "Jwt:Secret" "<dev value>"
dotnet user-secrets set "Jwt:Issuer" "TeamHub-Dev"
dotnet user-secrets set "Jwt:Audience" "TeamHub-Dev"
dotnet run

# Tests — currently broken, fix project references first (see Current State)
cd server && dotnet test TeamHub.Server.Tests
cd server && dotnet test TeamHub.Server.IntegrationTests

# Full stack via Docker — currently broken, paths don't match this repo
docker compose up --build
```

## Conventions

- Nullable reference types and implicit usings are enabled (`<Nullable>enable</Nullable>`) — don't disable these to avoid warnings; fix the warning instead.
- `Domain/Common/Result<T>` + `Error` is the established pattern for service-layer success/failure returns (not exceptions for expected failure cases) — `AuthService`/`DashboardService` already follow it; match it in new services rather than introducing a second pattern.
- Each module exposes DTOs, an `I<Module>Service` interface, a `<Module>Service` implementation, and `<Module>Endpoints` for its Minimal API routes (see `Features/Auth/` or `Dashboard/` as the reference implementation) — keep new modules consistent with this shape unless there's a documented reason not to (write an ADR if you change it).
- Module boundaries are convention-only right now (nothing prevents one module's service from reaching into another's internals) — don't add cross-module direct dependencies without a reason; go through the module's public service interface.
- Don't leave two versions of the same setup code around (see the `WebApplicationExtensions` vs. inline `Program.cs` issue above) — if you refactor startup/middleware config, delete the old version in the same change.

## Working in this repo

- Prefer fixing the environment/tooling issues above (docker-compose paths, the .NET 8 runtime gap) and getting the existing Auth/Dashboard flow fully runnable over adding more stub modules — see `docs/ROADMAP.md` for the sequenced next steps.
- **Always run `git pull` (or at least `git fetch` + check `git log HEAD..origin/main`) at the start of a session before trusting any status doc**, including this file — that's exactly how this file went stale once already (see the `docs/ARCHITECTURE.md` and `docs/ROADMAP.md` revision notes).
- When you finish or pause a work session, update `docs/ROADMAP.md`'s "Current Status" section so the next session (human or AI) knows where things stand.
- When you make a real architectural decision (not just an implementation detail), write it up as a new file in `docs/adr/` using `docs/adr/template.md`, rather than only describing it in a commit message or chat.
