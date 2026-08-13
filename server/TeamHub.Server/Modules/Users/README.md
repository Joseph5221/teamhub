# Users Module

**Status:** implemented — profile data, avatar, and system-wide role
assignment. Follows the `TeamService`/`IntegrationService` shape
(`Result<T>`/`Error`, `ClaimsPrincipal` → JWT `sub` claim for the caller).

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

## What's implemented

- `User.Role` is a real enum (`Domain.Enums.UserRole`: `Member`/`Admin`),
  system-wide and independent of a user's team-specific role (team
  owner/member — see `Team.OwnerId`/`Team.Members`). New registrations
  default to `Member` (`AuthService.RegisterAsync`).
- `User.AvatarUrl` (nullable `string`) was added to the `User` entity —
  just a URL the user supplies, no file upload/blob storage. If avatar
  upload becomes a real requirement later, that's a follow-up decision
  (new storage infra), not implied by this field.
- `IUserService`/`UserService`:
  - `GetProfileAsync` — any authenticated user may view any profile (a
    basic directory; this data is already visible via team member lists,
    see `TeamService.GetMembersAsync`).
  - `UpdateProfileAsync` — self only (endpoint enforces this via the
    `/api/users/me` route + JWT `sub` claim, not a userId route param).
    Validates non-empty name and, if provided, an absolute-URL-shaped
    avatar.
  - `UpdateRoleAsync` — admin only (`requestingUser.Role == Admin`);
    assigns another user's `Role`. There's no bootstrap endpoint to create
    the first admin — the seeded dev user `test@teamhub.com` is `Admin`
    (see `DbInitializer`); in a real deployment this needs an out-of-band
    way to mint the first admin (direct DB write, a future
    admin-provisioning script, etc.) since `UpdateRoleAsync` requires an
    existing admin to call it.
- Endpoints (`UserEndpoints.MapUserEndpoints`, mounted at `/api/users`,
  all require auth): `GET/PUT /me`, `GET /{userId}`, `PUT /{userId}/role`.

See [docs/ROADMAP.md](../../../../docs/ROADMAP.md) for where this sits in
the sequenced next steps.
