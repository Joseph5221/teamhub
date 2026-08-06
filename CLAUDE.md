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

## Current State (as of 2026-08-05) — read before coding

- **Local setup is now one command: `./scripts/setup-dev.sh`** — checks for
  a .NET 8 runtime (installs one via Homebrew if missing), sets dev JWT
  `user-secrets` idempotently, and builds. `./scripts/start-dev.sh` runs the
  API + Blazor frontend together; `./scripts/run-tests.sh` runs
  `dotnet test teamhub.sln`; `./scripts/seed-db.sh` resets the running API's
  test data. See `docs/adr/0003-target-net8-and-install-the-runtime.md`
  for why (and the Homebrew keg-only quirk those scripts paper over via a
  `DOTNET=` override).
- **The API seeds itself with test data on every startup** (Development
  only, idempotent): 3 users (`test@teamhub.com`, `bob@teamhub.com`,
  `carol@teamhub.com`, password `password123` for all — see
  `DbInitializer.SeedUserPassword`), 2 teams, 3 projects, 4 integrations —
  see `Infrastructure/Data/DbInitializer.cs`. `POST /api/dev/reseed`
  (dev-only, no auth) resets it without restarting the process.
- `frontend/BlazorApp` builds and runs. `shared/Shared` builds.
- **`server/TeamHub.Server` compiles** (`dotnet build teamhub.sln` — 0
  errors) and **Auth + Dashboard are implemented and verified end-to-end**:
  `POST /api/auth/login`, `POST /api/auth/register`, and `GET /api/dashboard`
  (JWT-protected) were actually exercised against a running server, not just
  built. `Teams`, `Projects`, `Integrations`, `Users` are still empty stubs —
  don't assume a method has behavior just because the file exists; check
  line count / open it.
- **Password hashing is implemented**: `AuthService.RegisterAsync` hashes on
  write and `LoginAsync` verifies via `IPasswordHasher`/`PasswordHasher`
  (`Infrastructure/Security/`, wraps
  `Microsoft.AspNetCore.Identity.PasswordHasher<T>` — PBKDF2, no new NuGet
  dependency). Login now rejects wrong passwords. Still missing:
  password strength/length validation on register, refresh tokens, email
  verification — see `docs/ROADMAP.md`'s next steps.
- **The app won't start without a JWT secret** (by design — `appsettings*.json`
  ship blank `Jwt:Secret`/`Issuer`/`Audience` on purpose, don't commit real
  secrets). `scripts/setup-dev.sh` sets dev ones via `user-secrets`
  automatically; do that by hand only if you need to.
- **`Infrastructure/Data/Configurations/*.cs` are filled in now** — they
  were empty stubs until this session, which meant EF Core couldn't resolve
  the `Team.Owner`/`Team.Members`/`User.Teams` relationships and threw at
  startup on the first `register`/`login` call. This had never been caught
  before because `dotnet run` had never actually succeeded on a dev machine
  (see the runtime item below) — don't assume "the docs say it works" means
  it's been run; check `docs/ROADMAP.md`'s Current Status for what's
  actually been verified vs. just built.
- **Database is in-memory** (`UseInMemoryDatabase`) — now a deliberate,
  written-down decision, not a doc/code mismatch. See
  `docs/adr/0002-in-memory-database-for-now.md`. `docker-compose.yml` no
  longer runs a Postgres `db` service.
- **`teamhub.sln` is the only solution file** and includes everything —
  `BlazorApp`, `Shared`, `TeamHub.Server`, `TeamHub.Server.Tests`, and
  `TeamHub.Server.IntegrationTests` — all building with 0 errors, and now
  actually passing (`dotnet test teamhub.sln`, or `scripts/run-tests.sh`).
- **`docker-compose.yml` and `scripts/*.sh` now match the real repo layout**
  (`server/TeamHub.Server`, `frontend/BlazorApp`) — the old `./TeamHub.Server`
  / `./teamhub-frontend` Node/Vite paths are gone, a real server `Dockerfile`
  was added, and there's a root `.env.example` for `JWT_SECRET`. Not
  smoke-tested against a real `docker compose up --build` (no Docker daemon
  available when this was fixed) — verify before trusting it fully. See
  `docs/ARCHITECTURE.md` § "Reality check" for detail.
- Before claiming any server-side task "done," run `dotnet build teamhub.sln`
  and confirm 0 errors. Run `dotnet test teamhub.sln` too — it now actually
  works, so there's no excuse to skip it.

Full detail: `docs/ARCHITECTURE.md` § "Reality check" and `docs/ROADMAP.md`
(sequenced next steps).

## Tech Stack

- **Backend**: ASP.NET Core (.NET 8, targets `net8.0` — see `docs/adr/0003-target-net8-and-install-the-runtime.md` for why that's deliberate, not an oversight), Minimal APIs.
- **Frontend**: Blazor Server (interactive server render mode, SignalR).
- **Auth**: JWT bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`), dev-mode-only validation (see Current State).
- **Database**: EF Core, currently `UseInMemoryDatabase` for dev — deliberate for now, see `docs/adr/0002-in-memory-database-for-now.md`. `Sqlite` and `Design` packages are referenced but unused; PostgreSQL is the eventual planned target (per `docs/ARCHITECTURE.md`) but nothing points at it yet.
- **Testing**: xUnit + FluentAssertions + Moq (`TeamHub.Server.Tests`), xUnit + `WebApplicationFactory` (`TeamHub.Server.IntegrationTests`) — both build from `teamhub.sln` and now actually run (`dotnet test teamhub.sln` or `scripts/run-tests.sh`).
- **Planned but not yet added**: MediatR (in-process event bus), Serilog + OpenTelemetry, Polly (resilience), real password hashing.

## Repo Layout

```
teamhub.sln                    Root solution, the only one — BlazorApp + Shared + TeamHub.Server + both test projects (builds clean)
docker-compose.yml              Matches this repo's layout now (server/TeamHub.Server, frontend/BlazorApp) — not smoke-tested against a real Docker daemon, see Current State
scripts/                        setup-dev.sh (one-command setup) / start-dev.sh / run-tests.sh / seed-db.sh (reset test data) / reset-db.sh (migration no-op while DB is in-memory)
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
  Infrastructure/                EF Core DbContext (in-memory), entity configs (now filled in), DbInitializer (dev seed data), JWT token service — implemented; security/email helpers still stub
  Extensions/                    ServiceCollectionExtensions (wired up, incl. AddApiDocumentation) — WebApplicationExtensions (unused dead code) removed
  Middleware/                    exception/logging middleware (stub, unused)
server/TeamHub.Server.Tests/            unit tests — builds and passes
server/TeamHub.Server.IntegrationTests/ integration tests — builds and passes
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
# One-time setup: checks/installs a .NET 8 runtime, sets dev JWT secrets, builds
./scripts/setup-dev.sh

# Runs API + Blazor frontend together
./scripts/start-dev.sh

# Or by hand:
dotnet build teamhub.sln              # build everything (0 errors)
dotnet run --project frontend/BlazorApp
cd server/TeamHub.Server && dotnet run  # needs the JWT secrets from setup-dev.sh

# Tests
./scripts/run-tests.sh                # or: dotnet test teamhub.sln

# Full stack via Docker (paths fixed, not smoke-tested against a real daemon)
cp .env.example .env   # fill in JWT_SECRET
docker compose up --build
```

If your default `dotnet` can't run `net8.0` apps (see
`docs/adr/0003-target-net8-and-install-the-runtime.md`), every script above
accepts a `DOTNET=/path/to/dotnet` override.

## Conventions

- Nullable reference types and implicit usings are enabled (`<Nullable>enable</Nullable>`) — don't disable these to avoid warnings; fix the warning instead.
- `Domain/Common/Result<T>` + `Error` is the established pattern for service-layer success/failure returns (not exceptions for expected failure cases) — `AuthService`/`DashboardService` already follow it; match it in new services rather than introducing a second pattern.
- Each module exposes DTOs, an `I<Module>Service` interface, a `<Module>Service` implementation, and `<Module>Endpoints` for its Minimal API routes (see `Features/Auth/` or `Dashboard/` as the reference implementation) — keep new modules consistent with this shape unless there's a documented reason not to (write an ADR if you change it).
- Module boundaries are convention-only right now (nothing prevents one module's service from reaching into another's internals) — don't add cross-module direct dependencies without a reason; go through the module's public service interface.
- Don't leave two versions of the same setup code around (this bit us once already — see item 7 in `docs/ARCHITECTURE.md` § "Reality check" for the `WebApplicationExtensions` vs. inline `Program.cs` history) — if you refactor startup/middleware config, delete the old version in the same change.

## Working in this repo

- The environment/tooling reconciliation (docker-compose paths, the .NET 8 runtime gap, JWT secret setup, the in-memory-DB decision) is done — see `docs/ROADMAP.md` for the current sequenced next steps (password hashing first).
- **Always run `git pull` (or at least `git fetch` + check `git log HEAD..origin/main`) at the start of a session before trusting any status doc**, including this file — status docs go stale the moment someone else pushes.
- When you finish or pause a work session, update `docs/ROADMAP.md`'s "Current Status" section so the next session (human or AI) knows where things stand.
- When you make a real architectural decision (not just an implementation detail), write it up as a new file in `docs/adr/` using `docs/adr/template.md`, rather than only describing it in a commit message or chat.
