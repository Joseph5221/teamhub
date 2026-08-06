# Users Module

**Status:** stub — no implementation yet (`UserDtos.cs`, `UserEndpoints.cs`, `UserService.cs`, `IUserService.cs` are all empty).

## Responsibilities

The boundary with `Auth` is decided — see
[ADR 0005](../../../../docs/adr/0005-auth-users-boundary.md):

- **`Auth`** owns credentials, sessions, and tokens (registration, login,
  password hashing/validation, JWT issuance, and eventually refresh tokens
  and OAuth linking).
- **`Users`** owns profile data — display name, avatar, preferences — and
  role assignment that isn't team-specific.
- Both operate on the single `User` entity for now (no split into separate
  `User`/`UserProfile` tables) — this is a module/service ownership
  convention, not a schema split. Go through `IUserService`/`IAuthService`
  rather than having one module reach into the other's writes.

See [docs/ROADMAP.md](../../../../docs/ROADMAP.md) for where this sits in
the sequenced next steps.
