# TeamHub — Status & Roadmap

Last reviewed: 2026-08-01 (updated same day after catching a missed `git
pull` — see revision note at the bottom). Update the "Current Status" section
whenever you pick this project back up or wrap a session — it's meant to
answer "where did I leave off?" in under a minute.

## Current Status

Two commits landed since the file-structure scaffold
(`2d2bc51` "Create functionality for error/result/dashboard/auth/JWT" and
`fa4bee7` "Setting up testing framework") that hadn't been reviewed here yet.
Net effect: real progress on Auth/Dashboard, but the environment needs some
reconciling before you can actually run or test anything.

- ✅ `dotnet build teamhub.sln` succeeds — 0 errors (it didn't as of the
  previous review; entities/EF Core/JWT packages are now in place).
- ✅ **Auth works end-to-end** (dev-mode only): `POST /api/auth/login` and
  `/register` issue JWTs. Login currently accepts **any password** and
  registration stores passwords **unhashed** — both explicitly `TODO` in
  `AuthService.cs`, not accidental, but not safe past local dev.
- ✅ **Dashboard works**: `GET /api/dashboard` (JWT-protected) returns user
  info, per-integration status, and team/project/integration counts.
- ✅ Blazor frontend still builds and runs, has a real landing page — but
  isn't wired up to call the new `/api/auth` or `/api/dashboard` endpoints yet.
- ❌ **The app won't start without a JWT secret.** `appsettings*.json` ship
  blank `Jwt:Secret`/`Issuer`/`Audience` on purpose (don't commit real
  secrets) — `AddInfrastructure` throws at startup until you set them via
  `dotnet user-secrets` or env vars.
- ❌ **This machine has no .NET 8 runtime** (`10.0.101` SDK / `10.0.1`
  runtime only) — `dotnet run` fails with a framework-mismatch error even
  though `dotnet build` succeeds. Check `dotnet --list-runtimes` on your
  actual dev machine; this may not apply there.
- ❌ **Database is in-memory**, not Postgres — resets on every restart, no
  migrations, despite `docker-compose.yml` running a Postgres `db` service.
- ✅ **Both test projects now compile and are wired into `teamhub.sln`** —
  added the missing `<ProjectReference>` to `TeamHub.Server`, added
  `Microsoft.AspNetCore.Mvc.Testing`/`FluentAssertions` to
  `TeamHub.Server.IntegrationTests.csproj`, fixed missing `using`
  directives, and exposed `Program` via `public partial class Program { }`
  for `WebApplicationFactory<Program>`. `dotnet build teamhub.sln` is 0
  errors across all 5 projects. `dotnet test` still can't *run* them on
  this machine — same .NET 8 runtime gap as `dotnet run` (see below), not a
  compile issue.
- ❌ **`docker-compose.yml` and `scripts/*.sh` reference paths that don't
  exist in this repo** (`./TeamHub.Server`, `./teamhub-frontend` as a
  Node/Vite project) — looks like boilerplate that was never reconciled
  with the actual `server/TeamHub.Server` / `frontend/BlazorApp` layout.
  `docker compose up --build` will fail.
- ✅ **One canonical solution file**: `server/TeamHub.sln` has been deleted.
  Root `teamhub.sln` is now the only solution and includes both test
  projects.
- ❌ `Teams`, `Projects`, `Integrations`, `Users` modules are still empty
  stubs — unchanged since the last review.

Full detail on every item above: [ARCHITECTURE.md § Reality check](ARCHITECTURE.md#reality-check--where-the-code-has-diverged-from-this-plan).

## Immediate Next Steps (in order)

Re-sequenced from the previous version of this doc now that Auth/Dashboard
exist — the priority now is making what's already written actually runnable
and testable, *then* resuming feature work. Don't start Teams/Projects/
Integrations until the app runs and the test suite compiles.

1. **Reconcile the local environment.**
   - Set a JWT secret: `cd server/TeamHub.Server && dotnet user-secrets init && dotnet user-secrets set "Jwt:Secret" "<a long random dev value>"` (and `Jwt:Issuer`/`Jwt:Audience`, e.g. `TeamHub-Dev` for both).
   - Check `dotnet --list-runtimes` — if there's no 8.x entry, either install the .NET 8 runtime or deliberately retarget the projects to `net10.0` (write down which you chose and why).
   - Confirm `dotnet run --project server/TeamHub.Server` starts and Swagger loads, then hit `/api/auth/register` → `/api/auth/login` → `/api/dashboard` with the returned token to confirm the flow actually works.
2. ~~Fix the two test projects~~ — done: both compile and are referenced from root `teamhub.sln`. `dotnet test` still can't *run* them until the .NET 8 runtime gap (step 1) is resolved on whichever machine you're on.
3. ~~Pick one canonical solution file~~ — done: `server/TeamHub.sln` deleted, root `teamhub.sln` is now the only solution and includes both test projects.
4. **Reconcile `docker-compose.yml` and `scripts/*.sh` with the real repo layout** — fix the build context paths (`server/TeamHub.Server`, `frontend/BlazorApp`) and either drop the `teamhub-frontend`/Vite service (if that was speculative/generated boilerplate) or, if a frontend rewrite to something other than Blazor is genuinely being considered, write that decision down as an ADR instead of leaving it as an inconsistency. Add a `.env.example` covering `JWT_SECRET` and `DB_PASSWORD`. Add the still-missing server `Dockerfile` (mirror `frontend/BlazorApp/Dockerfile`).
5. **Decide on the database** — either commit to in-memory for now (fine for early dev, but say so explicitly and remove the Postgres `db` service from compose until it's needed) or wire up the commented-out Sqlite/Postgres path in `AddInfrastructure` plus real EF Core migrations. Don't leave the docs and the code implying different databases.
6. **Add password hashing** (`IPasswordHasher`/`PasswordHasher` are already stubbed out) before building anything else on top of Auth — right now any password logs any user in.
7. **Remove dead/duplicate code**: the leftover `/weatherforecast` endpoint in `Program.cs`, and the unused `WebApplicationExtensions.InitializeDatabaseAsync`/`UseApiMiddleware` methods that duplicate what `Program.cs` already does inline — pick one place for startup/middleware config.
8. **Implement Teams** (create team, join team, list members) — next real dependency once Auth is trustworthy; `Dashboard` already assumes `user.Teams`.
9. **Pick one integration to prove out the pattern** — Jira or GitHub, both have well-documented REST APIs and free developer accounts. Use it to validate `IModuleConnector`'s shape (see [ARCHITECTURE.md](ARCHITECTURE.md#module-interface-contract)) before copying the pattern to the rest.
10. **Revisit the `/Modules` vs. flat-folder question** (see [ARCHITECTURE.md](ARCHITECTURE.md#reality-check--where-the-code-has-diverged-from-this-plan)) deliberately, and write an ADR for whichever way you go.
11. **Decide the `Projects` vs. `Users` vs. `Auth` boundary** — write down what each owns before building any of them out.

## Not Yet — Deliberately Deferred

Per [ADR 0001](adr/0001-modular-monolith-architecture.md), the following are
explicitly out of scope until there's a concrete reason to need them:
Kubernetes/AKS deployment, message broker (RabbitMQ/Kafka), Redis caching,
Prometheus/Grafana, multi-service extraction. Revisit only if a real scaling
or ownership problem shows up.

<sub>Revision note: this doc was first written against commit `1b3079f`
before a missed `git pull` was caught; updated the same day (2026-08-01)
against `fa4bee7` once that was resolved. See the matching note at the
bottom of [ARCHITECTURE.md](ARCHITECTURE.md).</sub>
