# TeamHub

A team dashboard (modular monolith) unifying Jira, Calendar, GitHub, CI/CD,
and infra-monitoring data into one Blazor Server UI. Early-stage / restarting
after a hiatus — see [CLAUDE.md](CLAUDE.md) and [docs/ROADMAP.md](docs/ROADMAP.md)
for what's actually done vs. stubbed.

## Quickstart

```bash
./scripts/setup-dev.sh   # checks your .NET runtime, sets dev JWT secrets, builds
./scripts/start-dev.sh   # runs the API + Blazor frontend together
./scripts/seed-db.sh     # resets the API's test data without restarting it
```

The API seeds itself with test users/teams/projects/integrations on every
startup (Development only — data is in-memory, so it resets on restart
anyway). `seed-db.sh` is for getting back to that clean state mid-session.

That covers most of it. If either script reports a problem, see:
- [docs/adr/0003-target-net8-and-install-the-runtime.md](docs/adr/0003-target-net8-and-install-the-runtime.md) — no matching .NET 8 runtime
- [server/TeamHub.Server/README.md](server/TeamHub.Server/README.md) — API quickstart, test credentials, endpoint list

## Docs

- [docs/PROJECT_OVERVIEW.md](docs/PROJECT_OVERVIEW.md) — product vision, features, target users
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — technical architecture, incl. a "reality check" on where the code diverges from the plan
- [docs/ROADMAP.md](docs/ROADMAP.md) — current status and next steps (check this first)
- [docs/adr/](docs/adr/) — why key decisions were made
