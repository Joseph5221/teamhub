# 0001. Use a modular monolith, not microservices

Date: 2026-08-01 (backfilled — reflects the original architecture proposal;
see [docs/PROJECT_OVERVIEW.md](../PROJECT_OVERVIEW.md) source material)

## Status

Accepted

## Context

TeamHub aggregates several independent-feeling concerns — auth, teams,
Jira/Calendar/Slack/GitHub integrations, infra monitoring — for a solo/small
team learning project. A "proper" microservices architecture (separate
services, message broker, service mesh) was considered per the original
project proposal, but that adds deployment, networking, and operational
complexity that isn't justified before the product even has one working
end-to-end flow.

## Decision

Build TeamHub as a **modular monolith**: one ASP.NET Core host
(`TeamHub.Server`), one PostgreSQL database with per-module schemas, feature
folders with clear internal boundaries (aiming for one shared
`IModuleConnector`-style interface for integrations), deployed as a single
container via Docker Compose. Microservice extraction is deferred
indefinitely and only revisited if a concrete scaling or team-ownership
reason appears.

## Consequences

- **Easier**: local dev (`docker compose up`), debugging (one process, one
  set of logs), refactoring across module boundaries while they're still
  being figured out, and deployment (one container).
- **Harder**: modules can't scale independently yet, and nothing stops a
  module from reaching into another module's internals unless that's
  enforced deliberately (code review discipline, internal access modifiers,
  or a lint rule — none of this is enforced automatically today).
- **Deferred decision, since resolved**: the original plan called for a
  `/Modules/<Domain>` folder nesting; the code as of this ADR used flat
  top-level feature folders instead (see "Reality check" in
  [ARCHITECTURE.md](../ARCHITECTURE.md)). That was flagged as a smaller,
  unresolved decision worth its own ADR — see
  [ADR 0004](0004-modules-folder-nesting.md), which formally adopts
  `/Modules` nesting.
