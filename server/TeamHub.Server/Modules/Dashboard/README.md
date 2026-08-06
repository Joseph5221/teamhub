# Dashboard Module

**Status:** implemented (added in commit `2d2bc51`). `GET /api/dashboard`
(JWT-protected) returns user info, per-integration status (name/type/status/
enabled), and stats (team/project/integration counts, connected-integration
count). See [server/TeamHub.Server/README.md](../../README.md) for the request/
response shape and how to call it via Swagger.

## Responsibilities

Aggregation layer for the team board: reads the current user's `Teams` and
`Integrations` (via EF navigation properties on `User`) and returns a
combined view model. Corresponds to "Example 1 — Team Board Load" in
[docs/ARCHITECTURE.md](../../../../docs/ARCHITECTURE.md) — though today it reads
directly off `AppDbContext` rather than going through separate `Teams`/
`Integrations` services, since those modules don't exist yet.

## Notes

- Currently reads against the **in-memory** database — there's no real
  `Teams`/`Integrations` data behind it yet beyond whatever seed data exists
  in `AppDbContext`/`DbInitializer` (also still a stub — check what's
  actually seeded before assuming the dashboard reflects real usage).
- Once `Teams` and `Integrations` become real modules with their own
  services, revisit whether `DashboardService` should call those services
  instead of querying `AppDbContext` directly — right now it's coupled
  straight to EF Core, which is fine for a first pass but worth refactoring
  once there's more than one consumer of that data.
- Should stay a thin orchestration/aggregation layer; avoid putting
  integration-specific logic here (that belongs in each connector).
