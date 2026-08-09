# Teams Module

**Status:** implemented (2026-08-08) — `TeamDtos.cs`, `ITeamService.cs`,
`TeamService.cs`, `TeamEndpoints.cs` all have real behavior, verified via
`dotnet test teamhub.sln` (unit tests in
`TeamHub.Server.Tests/Services/TeamServiceTests.cs`, integration tests in
`TeamHub.Server.IntegrationTests/TeamEndpointsIntegrationTests.cs`).

## Responsibilities

- Team creation and settings (name/description).
- Membership management: add an existing user by email, remove a member,
  list members.
- Permissions: two roles, **owner** and **member**.

## Roles & permissions

- **Owner** — `Team.OwnerId` (singular; matches the existing EF
  configuration in `Infrastructure/Data/Configurations/TeamConfiguration.cs`,
  which models one owner per team, not a many-owner join table). Only the
  owner can update team settings, add members, or remove members.
- **Member** — anyone in `Team.Members` (a many-to-many collection). The
  owner is also added to `Members` on creation, matching the
  `DbInitializer` seed convention (`Members = { alice, bob }` where `alice`
  is `Owner`) — so `Members` is "everyone on the team," and role is
  derived by comparing a user's ID to `OwnerId`.
- There's no ownership-transfer feature yet, so the remove-member endpoint
  explicitly rejects removing the owner (`Team.CannotRemoveOwner`).
- Viewing a team or its member list requires being a member (owner or
  not); non-members get `Team.Forbidden` (403). Note this means a team's
  existence leaks to any authenticated user who guesses/knows its ID —
  acceptable for now, revisit if it becomes a real concern.

## Endpoints (`/api/teams`, all require a valid JWT)

- `POST /api/teams` — create a team; caller becomes owner.
- `GET /api/teams/{teamId}` — get team details (members only).
- `PUT /api/teams/{teamId}` — update name/description (owner only).
- `GET /api/teams/{teamId}/members` — list members (members only).
- `POST /api/teams/{teamId}/members` — add an existing registered user by
  email (owner only). There's no invite-token/email-verification flow yet
  (see Auth's still-missing email verification in `docs/ROADMAP.md`), so
  this only works for users who already have an account.
- `DELETE /api/teams/{teamId}/members/{memberUserId}` — remove a member
  (owner only; can't remove the owner).

## Notes

- Depends on `Modules/Auth` for identifying the current user (JWT `sub`
  claim), same pattern as `Modules/Dashboard`.
- `Dashboard`, `Integrations`, and `Projects` all key data off `teamId` —
  this module needed to exist before those are meaningfully testable end
  to end. `Projects` in particular is team-scoped by decision, see
  [ADR 0006](../../../../docs/adr/0006-projects-definition.md).
  See [docs/ROADMAP.md](../../../../docs/ROADMAP.md).
