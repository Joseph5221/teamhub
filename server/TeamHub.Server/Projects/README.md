# Projects Module

**Status:** stub — no implementation yet (`ProjectDtos.cs`, `ProjectEndpoints.cs`, `ProjectService.cs`, `IProjectService.cs` are all empty).

## Responsibilities

Not defined in the original planning docs (`docs/PROJECT_OVERVIEW.md`,
`docs/ARCHITECTURE.md`) — this module was added during scaffolding without a
written rationale. Before implementing it, decide and document:

- Is this generic "project" tracking independent of any integration, or is
  it meant to be populated from Jira (i.e. redundant with the Jira
  integration once that exists)?
- How does it relate to `Team` (one team → many projects, presumably)?

See [docs/ROADMAP.md](../../../docs/ROADMAP.md) — clarifying this boundary
is listed as a near-term next step.
