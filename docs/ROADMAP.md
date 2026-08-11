# TeamHub — Status & Roadmap

Last reviewed: 2026-08-08. Update the "Current Status" section whenever you
pick this project back up or wrap a session — it's meant to answer "where
did I leave off?" in under a minute.

## Current Status

The environment is reconciled, Auth/Dashboard are verified end-to-end, Teams
is implemented and verified end-to-end, and the Integrations module (with a
real GitHub connector) is now implemented and verified end-to-end too.

- ✅ **Integrations implemented, GitHub connector proven out (2026-08-08)**:
  `IModuleConnector` (`Modules/Integrations/IModuleConnector.cs`) now exists
  in code, matching `docs/ARCHITECTURE.md`'s documented shape exactly
  (`GetConfigAsync`/`GetDataAsync`/`InvokeActionAsync`, plus normalized
  `ModuleConfig`/`ModuleData`/`ModuleDataItem` result types). Generic
  `IIntegrationService`/`IntegrationEndpoints`/`IntegrationDtos`
  (`Modules/Integrations/`) provide CRUD for a team's configured
  integrations — owner manages config (create/update/delete), any member
  can view and trigger data/action calls — and dispatch by
  `IntegrationType` to whichever `IModuleConnector` is registered in DI;
  types with no connector yet (Jira/Slack/Calendar/CI-CD/InfraMonitoring)
  return `501 Integration.NotSupported` instead of a crash.
  `Modules/Integrations/GitHub/` is the first real connector: lists an
  org's repos via the real GitHub REST API
  (`GET /orgs/{org}/repos`), auth is a per-team optional personal access
  token read from `Integration.ConfigurationData` (unauthenticated works
  for public orgs at a lower rate limit), wrapped in Polly retry
  (3 attempts, exponential backoff) + circuit-breaker (opens 30s after 5
  consecutive failures) per `docs/ARCHITECTURE.md` "Resilience & Testing"
  — `Microsoft.Extensions.Http.Polly` added to `TeamHub.Server.csproj`.
  `MapIntegrationEndpoints()` wired into `Program.cs` (previously
  commented out); enums (`IntegrationType`/`IntegrationStatus`) now
  serialize as strings API-wide (`JsonStringEnumConverter` in
  `Program.cs`) rather than raw ints. Verified two ways: `dotnet test
  teamhub.sln` (43 unit + 7 integration tests, including
  `GitHubApiClientTests` against a stubbed `HttpMessageHandler` and
  `GitHubConnectorTests`/`IntegrationServiceTests` against a mocked
  connector — no network calls in the suite), and a live manual run
  (`dotnet run`, real server, real JWT login) creating a team + a GitHub
  integration configured against the real public `octokit` org and
  confirming actual repo data came back, the `sync` action flipped status
  to `Connected`, and an unimplemented type correctly 501'd. The seeded
  "Growth" team's GitHub integration
  (`Infrastructure/Data/DbInitializer.cs`) is now pre-configured against
  `octokit` too, so frontend widget work has real sample data without
  minting a PAT. See `Modules/Integrations/README.md` and
  `Modules/Integrations/GitHub/README.md` for full detail, including how
  to copy the pattern for Jira/Slack/Calendar next.
- ✅ **Teams implemented (2026-08-08)**: `ITeamService`/`TeamService`
  (`Modules/Teams/`) support creating a team (caller becomes owner + first
  member), getting a team's details, listing members, updating team
  settings, adding an existing user by email, and removing a member — all
  via the `Result<T>`/`Error` pattern with codes like `Team.Forbidden`,
  `Team.AlreadyMember`, `Team.CannotRemoveOwner`. Two roles: **owner**
  (`Team.OwnerId`, singular — matches the existing EF config) and
  **member** (`Team.Members`, includes the owner, matching the
  `DbInitializer` seed convention). Only the owner can update settings,
  add members, or remove members; the owner can't be removed via the
  remove-member endpoint (no ownership-transfer feature yet). Viewing a
  team or its member list requires being a member (owner or not) —
  non-members get `Team.Forbidden` (403), not a 404, so team existence
  does leak to any authenticated user who knows the ID; revisit if that
  becomes a real concern. `TeamEndpoints.MapTeamEndpoints()` is wired into
  `Program.cs`, `ITeamService` registered in
  `ServiceCollectionExtensions.AddFeatures`. Verified via
  `dotnet test teamhub.sln` (16 new unit tests in
  `TeamHub.Server.Tests/Services/TeamServiceTests.cs`, 6 new integration
  tests in `TeamHub.Server.IntegrationTests/TeamEndpointsIntegrationTests.cs`
  hitting a real running server through `WebApplicationFactory`) — 23/23
  passing total.
- ✅ `dotnet build teamhub.sln` succeeds — 0 errors.
- ✅ **This machine now has a .NET 8 runtime** (installed via
  `brew install dotnet@8`, per
  [ADR 0003](adr/0003-target-net8-and-install-the-runtime.md)) —
  `dotnet run`/`dotnet test` work via `scripts/setup-dev.sh` /
  `DOTNET=<path> scripts/start-dev.sh`; see that ADR for the Homebrew
  keg-only quirk that setup script papers over.
- ✅ **Auth verified working end-to-end for real** (dev-mode only):
  `POST /api/auth/login` and `/register` issue JWTs, confirmed by actually
  running the server and curling `register` → `login` → `GET /api/dashboard`
  with the returned token.
- ✅ **Password hashing implemented** (2026-08-05): `IPasswordHasher`/
  `PasswordHasher` (`Infrastructure/Security/`) wrap
  `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2, no new NuGet
  dependency — it ships in the `Microsoft.NET.Sdk.Web` shared framework).
  `AuthService.RegisterAsync` hashes on write, `LoginAsync` verifies and
  rejects wrong passwords (previously accepted any password). Seeded dev
  users (`DbInitializer`) now use a real hashed password — see
  `DbInitializer.SeedUserPassword` (`password123`) — and
  `server/TeamHub.Server/README.md` was updated to match.
- ✅ **Password validation implemented** (2026-08-06): `IPasswordValidator`/
  `PasswordValidator` (`Infrastructure/Security/`) enforce 8-128 characters
  plus at least one letter and one digit (no special-character requirement,
  deliberately — see the doc comment on `PasswordValidator`, based on
  NIST SP 800-63B guidance that length matters more than forced
  complexity). `AuthService.RegisterAsync` validates before checking for a
  duplicate email or hashing, returning `Auth.WeakPassword` with a combined
  message on failure. Verified both via `dotnet test` (new
  `RegisterAsync_WithValidPassword_ReturnsSuccessResult` /
  `RegisterAsync_WithWeakPassword_ReturnsFailure` cases) and a live
  `register` call against a running server with a weak and a strong
  password.
- ✅ **Found and fixed a real bug while verifying this**:
  `Infrastructure/Data/Configurations/*.cs` were empty stubs, so EF Core
  couldn't resolve the `Team.Owner`/`Team.Members`/`User.Teams`
  relationships and threw `InvalidOperationException` on the first
  `register`/`login` call. This had never been caught before because
  `dotnet run` had never successfully executed on a dev machine until the
  runtime gap above was closed. Now fixed with minimal Fluent API config —
  see [ADR 0002](adr/0002-in-memory-database-for-now.md).
- ✅ **Dashboard works**: `GET /api/dashboard` (JWT-protected) returns user
  info, per-integration status, and team/project/integration counts.
- ✅ Blazor frontend still builds and runs, has a real landing page — but
  isn't wired up to call the new `/api/auth` or `/api/dashboard` endpoints yet.
- ✅ **JWT secret setup is now scripted**: `./scripts/setup-dev.sh` sets dev
  `user-secrets` idempotently (still required — `AddInfrastructure` throws
  at startup without them, intentionally, since real secrets aren't
  committed).
- ✅ **Database is formally in-memory now, not a documentation gap** — see
  [ADR 0002](adr/0002-in-memory-database-for-now.md). `docker-compose.yml`
  no longer runs a Postgres `db` service.
- ✅ **Both test projects compile, are wired into `teamhub.sln`, and now
  actually run and pass** (`TeamHub.Server.Tests`: 23 tests;
  `TeamHub.Server.IntegrationTests`: 7 tests) via `scripts/run-tests.sh` /
  the .NET 8 runtime from ADR 0003.
- ✅ **One canonical solution file**: `teamhub.sln` at the repo root,
  covering frontend + backend + shared + both test projects.
- ✅ **`docker-compose.yml` and `scripts/*.sh` now match the real repo
  layout** (`server/TeamHub.Server`, `frontend/BlazorApp`) — the speculative
  `teamhub-frontend` Node/Vite service is gone (Blazor Server was always the
  real frontend). Added the missing server `Dockerfile` (mirrors
  `frontend/BlazorApp/Dockerfile`) and a root `.env.example` for
  `JWT_SECRET`. Not smoke-tested against a real `docker compose up --build`
  in this session (no Docker daemon available) — do that before trusting it
  fully.
- ✅ **Dead code removed**: the leftover `/weatherforecast` endpoint in
  `Program.cs`, and `WebApplicationExtensions` (`InitializeDatabaseAsync`/
  `UseApiMiddleware`, unused duplicates of what `Program.cs` already did
  inline). `ServiceCollectionExtensions.AddApiDocumentation` (previously
  unused, more complete than the inline Swagger setup — it adds the JWT
  bearer scheme to Swagger UI) is now the one wired up in `Program.cs`.
- ✅ **Dev database now seeds itself with realistic test data**:
  `Infrastructure/Data/DbInitializer.cs` (previously an empty stub) creates
  3 users, 2 teams (with owner + members), 3 projects, and 4 integrations
  at startup (Development only, idempotent). `POST /api/dev/reseed` (dev-only,
  unauthenticated) resets and reseeds without restarting the process;
  `./scripts/seed-db.sh` wraps it. The old `AppDbContext` `HasData` seed
  (one user, no relationships) is gone — `HasData` can't express `Team.Owner`/
  `Team.Members` cleanly, so seeding moved to runtime.
- ✅ **Fixed a data-model mismatch found while scoping the Integrations
  work**: `Integration` was owned by `User` (`UserId`), but
  `docs/ARCHITECTURE.md`'s `IModuleConnector` interface and
  `Integrations/README.md` both assume integrations are configured
  per-team. `Integration` now has `TeamId`/`Team` (via `Team.Integrations`),
  and `Type`/`Status` are real enums (`IntegrationType`/`IntegrationStatus`)
  instead of unchecked strings — `DbInitializer` and `DashboardService`
  updated to match, verified via `dotnet test` and a live
  `register → login → /api/dashboard` run. Also removed two empty,
  undocumented top-level `Security/`/`Services/` folders (unrelated to
  `Infrastructure/Security/`, which still holds the real password-hashing
  stubs) — leftover scaffolding, never referenced anywhere.
- ✅ **Three structural decisions resolved (2026-08-06)**, closing out the
  open questions in `docs/ARCHITECTURE.md` § "Reality check":
  - **`/Modules` nesting formally adopted** — `Auth`, `Dashboard`, `Teams`,
    `Users`, `Projects`, `Integrations` moved (via `git mv`, history
    preserved) from flat top-level folders to
    `server/TeamHub.Server/Modules/<Name>/`; `Auth`/`Dashboard` namespaces
    corrected from `TeamHub.Server.Features.*` to
    `TeamHub.Server.Modules.*` to match. `Domain`/`Infrastructure`/
    `Extensions`/`Middleware` stay top-level (shared kernel, not modules).
    Verified via `dotnet build teamhub.sln` (0 errors) and `dotnet test
    teamhub.sln` (8/8 passing). See
    [ADR 0004](adr/0004-modules-folder-nesting.md).
  - **Auth/Users boundary decided**: `Auth` owns credentials/sessions/
    tokens, `Users` owns profile data (display name, avatar, preferences)
    — both still on the single `User` entity for now. See
    [ADR 0005](adr/0005-auth-users-boundary.md).
  - **`Projects` defined**: team-scoped (`Project.TeamId`/`Team`) and meant
    to aggregate data from that team's integrations (Jira, GitHub, infra
    monitoring) once real connectors exist — not a pure Jira mirror, not a
    fully generic tracker with no integration data. The exact
    `Project` ↔ integration-data linking shape is left for whoever builds
    the first real integration. See
    [ADR 0006](adr/0006-projects-definition.md).
- ❌ `Projects` and `Users` modules are still empty stubs — `Teams` and
  `Integrations` are done (see above); their boundaries are decided, so
  building them out is unblocked.
- ❌ Jira/Slack/Calendar connectors not started (deliberately — GitHub was
  built first to prove out `IModuleConnector`'s shape; see "Immediate Next
  Steps" below for the copy-the-pattern task breakdown).
- ❌ Auth still has no refresh tokens, no email verification — password
  hashing and validation are both done (see above).

Full detail on every item above: [ARCHITECTURE.md § Reality check](ARCHITECTURE.md#reality-check--where-the-code-has-diverged-from-this-plan).

## Local setup (streamlined)

```bash
./scripts/setup-dev.sh   # checks .NET runtime, sets dev JWT secrets, builds
./scripts/start-dev.sh   # runs API + Blazor frontend together (auto-seeds test data)
./scripts/run-tests.sh   # dotnet test teamhub.sln
./scripts/seed-db.sh     # resets the running API's test data on demand
```

All three accept a `DOTNET=/path/to/dotnet` override if your default
`dotnet` can't run `net8.0` apps — see
[ADR 0003](adr/0003-target-net8-and-install-the-runtime.md).

## Immediate Next Steps (in order)

The environment is now runnable and testable — the priority shifts to
making Auth trustworthy, then resuming feature work.

1. ~~Add password hashing~~ — done (2026-08-05), see Current Status above.
2. ~~Add password strength/length validation on register~~ — done (2026-08-06), see Current Status above.
3. ~~Revisit the `/Modules` vs. flat-folder question~~ — done (2026-08-06), see [ADR 0004](adr/0004-modules-folder-nesting.md).
4. ~~Decide the `Projects` vs. `Users` vs. `Auth` boundary~~ — done (2026-08-06), see [ADR 0005](adr/0005-auth-users-boundary.md) and [ADR 0006](adr/0006-projects-definition.md).
5. ~~Implement Teams~~ (create team, add/remove/invite members, list members, owner/member permission checks) — done (2026-08-08), see Current Status above.
6. ~~Pick one integration to prove out the pattern~~ — done (2026-08-08): GitHub connector built and verified end-to-end, see Current Status above. `IModuleConnector`'s shape is validated; **not yet done**: attaching its data to a `Project` per [ADR 0006](adr/0006-projects-definition.md) — `Projects` is still a stub, so this is blocked on building that module first.
7. **Copy the GitHub pattern to Jira and Slack** (Calendar after). Do these one at a time, not in parallel — GitHub was step 6 specifically so the shape gets validated once before multiplying it. For each: new `Modules/Integrations/<Name>/` folder (`I<Name>ApiClient`/`<Name>ApiClient`/`<Name>Connector`/DTOs, copy `GitHub/`'s shape), register the typed HTTP client with Polly + the connector as `IModuleConnector` in `ServiceCollectionExtensions.AddFeatures` (copy the GitHub block) — `IntegrationService`/`IntegrationEndpoints` pick it up automatically, no other code to touch. See `server/TeamHub.Server/Modules/Integrations/README.md` § "Adding the next connector".
8. **Smoke-test `docker compose up --build`** against the reconciled paths above on a machine with Docker running — this session fixed the paths and added the server `Dockerfile` but couldn't verify the full build (no Docker daemon available here).

## Not Yet — Deliberately Deferred

Per [ADR 0001](adr/0001-modular-monolith-architecture.md), the following are
explicitly out of scope until there's a concrete reason to need them:
Kubernetes/AKS deployment, message broker (RabbitMQ/Kafka), Redis caching,
Prometheus/Grafana, multi-service extraction. Revisit only if a real scaling
or ownership problem shows up.
