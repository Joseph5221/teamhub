# 0004. Nest feature folders under /Modules

Date: 2026-08-06

## Status

Accepted

## Context

The original architecture proposal called for a `/Modules/<Domain>` folder
nesting (`/Modules/{Auth,Teams,Integrations/{Jira,Calendar,Slack},InfraWatch}`).
The code that actually got built diverged from that: `Auth`, `Dashboard`,
`Teams`, `Users`, `Projects`, and `Integrations` all lived as flat,
top-level folders directly under `server/TeamHub.Server/`, with `Auth` and
`Dashboard`'s C# namespaces (`TeamHub.Server.Features.Auth`,
`TeamHub.Server.Features.Dashboard`) not even agreeing with their own flat
physical location — a leftover from early scaffolding, not a deliberate
"vertical slice" choice. [ADR 0001](0001-modular-monolith-architecture.md)
flagged this as a real, unresolved divergence worth its own decision rather
than something to leave implicit.

Both layouts are legitimate patterns (flat feature-per-folder is a common,
reasonable style). The problem was that the repo had a foot in each: a
planning doc describing one layout, code doing another, and namespaces
disagreeing with both.

## Decision

Adopt `/Modules/<Domain>` nesting, matching the original plan. Feature
modules physically live under `server/TeamHub.Server/Modules/`:

```
server/TeamHub.Server/Modules/
  Auth/
  Dashboard/
  Teams/
  Users/
  Projects/
  Integrations/          (submodules — Jira/, GitHub/, etc. — nest here once built)
```

C# namespaces match the physical path: `TeamHub.Server.Modules.Auth`,
`TeamHub.Server.Modules.Dashboard`, etc. (previously `Features.Auth` /
`Features.Dashboard`, now corrected).

`Domain/`, `Infrastructure/`, `Extensions/`, and `Middleware/` stay at the
top level, outside `/Modules` — they aren't feature modules, they're the
shared kernel and composition root that every module depends on. Nesting
them under `/Modules` would misrepresent them as domain-specific.

Every module keeps the shape already established by `Auth`/`Dashboard`:
DTOs, `I<Module>Service`, `<Module>Service`, `<Module>Endpoints`, and a
`README.md` — this ADR only settles the folder nesting question, not the
internal module shape (already covered in the root `CLAUDE.md`
"Conventions" section).

## Consequences

- **Easier**: the repo layout now matches the planning docs and ADR 0001,
  so there's no more "check the code, not the doc" caveat for this
  specific question. Future modules (`InfraWatch`, and `Integrations`
  submodules like `Jira`/`GitHub`/`Slack`) have an unambiguous place to go —
  nested under `Modules/`, not scattered at the top level.
- **Harder**: import paths and namespaces are one level deeper
  (`TeamHub.Server.Modules.Auth` vs. `TeamHub.Server.Auth`) — a small,
  permanent cost paid once here rather than repeatedly as an open question.
- **One-time churn, already paid**: `Auth` and `Dashboard` (the two modules
  with real code) were moved via `git mv` and their namespaces corrected in
  the same change that introduced this ADR, verified via
  `dotnet build teamhub.sln` (0 errors) and `dotnet test teamhub.sln` (8/8
  passing). `Teams`, `Users`, `Projects`, `Integrations` were moved as
  empty stub folders — no namespace fix was needed since none of their
  files have content yet.
