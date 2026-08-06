# TeamHub — Status & Roadmap

Last reviewed: 2026-08-05. Update the "Current Status" section whenever you
pick this project back up or wrap a session — it's meant to answer "where
did I leave off?" in under a minute.

## Current Status

The environment is now reconciled and the Auth/Dashboard flow has actually
been verified end-to-end for the first time (not just built).

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
  `server/TeamHub.Server/README.md` was updated to match. Still true:
  registration has no password strength/length validation yet — that's
  next, not this change.
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
  actually run and pass** (`TeamHub.Server.Tests`: 3 tests;
  `TeamHub.Server.IntegrationTests`: 1 test) via `scripts/run-tests.sh` /
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
- ❌ `Teams`, `Projects`, `Integrations`, `Users` modules are still empty
  stubs.
- ❌ Auth still has no password strength/length validation on register, no
  refresh tokens, no email verification — password hashing itself is done
  (see above).

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
2. **Add password strength/length validation on register** — natural next Auth-hardening step now that hashing is in place.
3. **Implement Teams** (create team, join team, list members) — next real dependency once Auth is trustworthy; `Dashboard` already assumes `user.Teams`, and the `Team.Members`/`Team.Owner` EF relationships are now configured and ready to use.
4. **Pick one integration to prove out the pattern** — Jira or GitHub, both have well-documented REST APIs and free developer accounts. Use it to validate `IModuleConnector`'s shape (see [ARCHITECTURE.md](ARCHITECTURE.md#module-interface-contract)) before copying the pattern to the rest.
5. **Revisit the `/Modules` vs. flat-folder question** (see [ARCHITECTURE.md](ARCHITECTURE.md#reality-check--where-the-code-has-diverged-from-this-plan)) deliberately, and write an ADR for whichever way you go.
6. **Decide the `Projects` vs. `Users` vs. `Auth` boundary** — write down what each owns before building any of them out.
7. **Smoke-test `docker compose up --build`** against the reconciled paths above on a machine with Docker running — this session fixed the paths and added the server `Dockerfile` but couldn't verify the full build (no Docker daemon available here).

## Not Yet — Deliberately Deferred

Per [ADR 0001](adr/0001-modular-monolith-architecture.md), the following are
explicitly out of scope until there's a concrete reason to need them:
Kubernetes/AKS deployment, message broker (RabbitMQ/Kafka), Redis caching,
Prometheus/Grafana, multi-service extraction. Revisit only if a real scaling
or ownership problem shows up.
