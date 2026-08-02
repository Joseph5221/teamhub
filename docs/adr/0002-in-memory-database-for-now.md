# 0002. Commit to EF Core's in-memory provider for now, drop Postgres from compose

Date: 2026-08-01

## Status

Accepted

## Context

The code (`ServiceCollectionExtensions.AddInfrastructure`) has used
`UseInMemoryDatabase("TeamHubDb")` since Auth/Dashboard were built, with the
Sqlite/Postgres path commented out. `docker-compose.yml`, however, still ran
a Postgres `db` service, and the docs described both as if they were both
real — which they weren't. No migrations exist, `DbInitializer.cs` and
`Infrastructure/Data/Configurations/*.cs` were empty stubs, and nobody had
actually run the app against Postgres.

Wiring up real Postgres now (connection string, migrations, entity
configurations, docker volume) is real work with no immediate payoff: Teams/
Projects/Integrations/Users are all still stubs, so there's no data model
stable enough yet to migrate against.

## Decision

Formally adopt the in-memory provider as the deliberate choice for local dev
until a module actually needs data to survive a restart. `docker-compose.yml`
no longer runs a `db` service. `.env.example` no longer references
`DB_PASSWORD`.

While making the in-memory provider actually work (`dotnet run` had never
succeeded on a dev machine before — see
[0003](0003-target-net8-and-install-the-runtime.md)), we also found and fixed
a real bug this decision doesn't change: `Infrastructure/Data/Configurations/
*.cs` were empty (0-byte) `IEntityTypeConfiguration<T>` stubs, so EF Core
couldn't resolve the `Team.Owner` / `Team.Members` / `User.Teams`
relationships and threw at startup on the first request. Those are now filled
in with the minimal Fluent API needed (`Team.Owner` as a one-to-many via
`OwnerId`, `Team.Members`/`User.Teams` as an explicit many-to-many, `Project`
and `Integration` foreign keys) — this would have been needed regardless of
which database provider is in use.

## Consequences

- **Easier**: `docker compose up` no longer needs a `DB_PASSWORD` secret or a
  Postgres container just to run the API. Local dev has one less moving
  part.
- **Harder**: data doesn't survive a restart, and there's no path yet from
  in-memory to Postgres without doing the migration work that's being
  deferred here.
- **Revisit when**: any module needs data to persist across restarts, or
  when Teams/Projects get real usage — at that point wire up the commented-
  out Sqlite/Postgres path in `AddInfrastructure`, add EF Core migrations,
  and bring the `db` service back into `docker-compose.yml`.
