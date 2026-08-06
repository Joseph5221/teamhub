# Teams Module

**Status:** stub — no implementation yet (`TeamDtos.cs`, `TeamEndpoints.cs`, `TeamService.cs`, `ITeamService.cs` are all empty).

## Responsibilities

- Team configuration.
- Membership management (add/remove/invite members).
- Permission management (roles within a team).

## Notes

- Depends on `Modules/Auth` for identifying the current user.
- `Dashboard`, `Integrations`, and `Projects` all key data off `teamId` —
  this module needs to exist before those are meaningfully testable end to
  end. `Projects` in particular is team-scoped by decision, see
  [ADR 0006](../../../../docs/adr/0006-projects-definition.md).
  See [docs/ROADMAP.md](../../../../docs/ROADMAP.md).
