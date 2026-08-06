# Projects Module

**Status:** stub — no implementation yet (`ProjectDtos.cs`, `ProjectEndpoints.cs`, `ProjectService.cs`, `IProjectService.cs` are all empty).

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
  on `Integration`, or something else) is **not yet decided** — that's for
  whoever implements the first real integration to design, keyed off
  `Project` rather than inventing a parallel per-integration project
  concept.

See [docs/ROADMAP.md](../../../../docs/ROADMAP.md) for where this sits in
the sequenced next steps.
