# Projects Module

**Status:** implemented — team-scoped project CRUD. Follows the same
owner/member permission shape as `TeamService`/`IntegrationService`: the
owner manages projects (create/update/delete), any member can view.

## Responsibilities

What a `Project` is is decided — see
[ADR 0006](../../../../docs/adr/0006-projects-definition.md):

- A `Project` belongs to exactly one `Team` (`Project.TeamId`/`Team`,
  already modeled) — it is TeamHub-native (name, description, status,
  dates), not a mirror of an external system's data model.
- A `Project` acts as a container that can aggregate data pulled in from
  that team's configured integrations (Jira issues, GitHub repos/PRs,
  infra-monitoring status, etc.) once `Modules/Integrations` has real
  connectors. It is not required to have any integration attached — a team
  can have projects with no linked integration data yet.
- The exact `Project` ↔ integration-data linking shape (join table, field
  on `Integration`, or something else) is **still not decided** — this
  session implemented the CRUD half only; that's for whoever implements
  the first real integration to design, keyed off `Project` rather than
  inventing a parallel per-integration project concept.

## What's implemented

- `Project.Status` is a real enum (`Domain.Enums.ProjectStatus`:
  `Planned`/`Active`/`OnHold`/`Completed`/`Cancelled`), serialized as a
  string API-wide (matches `IntegrationType`/`IntegrationStatus`'s
  precedent — see `Program.cs`'s `JsonStringEnumConverter`). New projects
  default to `Planned`.
- `IProjectService`/`ProjectService`, nested under a team (`teamId` is
  always the first argument, matching `IIntegrationService`):
  - `CreateProjectAsync` — owner only.
  - `GetProjectsAsync`/`GetProjectAsync` — members only (owner or member).
  - `UpdateProjectAsync` — owner only; updates name/description/status/dates.
  - `DeleteProjectAsync` — owner only.
- Endpoints (`ProjectEndpoints.MapProjectEndpoints`, nested under
  `/api/teams/{teamId}/projects` to match `TeamEndpoints`/
  `IntegrationEndpoints`, all require auth): `POST /`, `GET /`,
  `GET /{projectId}`, `PUT /{projectId}`, `DELETE /{projectId}`.

See [docs/ROADMAP.md](../../../../docs/ROADMAP.md) for where this sits in
the sequenced next steps.
