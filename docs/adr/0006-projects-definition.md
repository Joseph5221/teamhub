# 0006. What "Projects" is

Date: 2026-08-06

## Status

Accepted

## Context

`Modules/Projects` (see [ADR 0004](0004-modules-folder-nesting.md)) exists
in code — a `Project` entity with `TeamId`/`Team`, `Name`, `Description`,
`Status`, `StartDate`/`EndDate` — but was never in either planning
document. Its `README.md` shipped as an open question: is it generic
project tracking independent of any integration, or should it end up
populated from Jira (redundant with the Jira integration once that
exists)? Building it out further without deciding meant risking either a
throwaway generic CRUD feature or, worse, two competing sources of truth
for "what is a project" once a real Jira/GitHub integration landed.

## Decision

A `Project` is **team-scoped and integration-aggregating**: it belongs to
exactly one `Team` (the existing `Project.TeamId`/`Team` relationship is
correct as-is) and acts as a container that surfaces data pulled in from
that team's configured integrations — Jira issues, GitHub repos/PRs,
infra-monitoring status, etc. — rather than being either a pure
Jira-mirror or a fully independent tracking system with no integration
data.

Concretely:

- `Project` stays the TeamHub-native record (name, description, status,
  dates) — it is not deleted or replaced by an integration's data model.
- A `Project` can have zero or more integration data sources attached to
  it (e.g. "this project's GitHub repo is `org/repo`", "this project's
  Jira project key is `ABC`"). The exact linking shape (a join table, a
  field on `Integration`, or something else) is an implementation detail
  for whoever builds the first real integration under
  `Modules/Integrations` — not decided by this ADR — but it must key off
  `Project`, not duplicate per-integration project concepts.
- `Dashboard`'s existing per-team project counts continue to work
  unchanged, since they only depend on `Team → Project`, not on any
  integration linkage.
- No integration is a prerequisite for creating a `Project` — a team can
  have projects with no integration data attached yet (the common case
  today, since `Integrations` is still a stub).

## Consequences

- **Easier**: `Projects` and `Integrations` can now be built in either
  order without redoing one to match the other — `Projects` doesn't wait
  on a real Jira/GitHub connector to exist, and the first real connector
  has a defined place to attach its data (a `Project`) instead of
  inventing its own.
- **Harder**: `Project` needs a (not-yet-designed) way to reference
  external identifiers per integration (a Jira key, a GitHub repo slug),
  which is more schema surface than either "pure generic tracking" or
  "pure Jira mirror" would have needed alone.
- **Deferred, not resolved**: the exact linking mechanism between
  `Project` and `Integration` data is left to whoever implements the first
  real integration — see `docs/ROADMAP.md`'s next steps.
