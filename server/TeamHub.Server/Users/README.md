# Users Module

**Status:** stub — no implementation yet (`UserDtos.cs`, `UserEndpoints.cs`, `UserService.cs`, `IUserService.cs` are all empty).

## Responsibilities

Not defined in the original planning docs — the architecture doc describes
authentication and profile concerns as living entirely in `Auth`. Before
implementing this module, decide the boundary explicitly (and write it down,
e.g. as an ADR):

- **Likely split:** `Features/Auth` owns credentials/sessions/OAuth tokens;
  `Users` owns profile data (display name, avatar, preferences) and role
  assignment that isn't team-specific.
- Avoid building both in parallel without settling this first — it's an easy
  way to end up with duplicated or conflicting user records.

See [docs/ROADMAP.md](../../../docs/ROADMAP.md).
