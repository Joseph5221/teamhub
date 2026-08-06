# 0005. Auth/Users module boundary

Date: 2026-08-06

## Status

Accepted

## Context

`Modules/Users` (see [ADR 0004](0004-modules-folder-nesting.md) for the
folder move) exists in code but was never in either planning document, and
its `README.md` had shipped as an open question rather than a decision:
`Modules/Auth` already owns login/register/JWT issuance against the single
`User` entity (`Domain/Entities/User.cs`), which currently carries both
credential fields (`Email`, `PasswordHash`) and profile-ish fields (`Name`,
`Role`) in one place. Building `Users` out without settling the boundary
first risked duplicated or conflicting user records, or two services both
claiming to own the same field.

## Decision

- **`Auth` owns**: registration, login, credential storage
  (`PasswordHash` via `IPasswordHasher`/`IPasswordValidator`), JWT/session
  issuance, and (when built) refresh tokens and OAuth provider linking.
  Auth is the only module that writes `Email` or `PasswordHash`.
- **`Users` owns**: profile data — display name, avatar, preferences — and
  role assignment that isn't team-specific. `Users` is the only module that
  writes `Name`/`Role` (or their eventual replacements/additions, e.g.
  avatar URL, notification preferences).
- **Data model**: both continue to operate on the single `User` entity for
  now (no split into separate `User`/`UserProfile` tables) — the boundary
  is a service/module ownership convention, not a schema split. `Auth`
  creates the `User` row on register with the minimal identity fields;
  `Users` reads/updates the profile-shaped fields on that same row. If
  profile data grows enough to justify its own table later, that's a
  follow-up ADR, not implied by this one.
- Cross-module access goes through `IUserService`/`IAuthService`, not
  direct `DbContext` reads of `User` from the other module — consistent
  with the "module boundaries are convention-only, don't reach into
  another module's internals" rule in the root `CLAUDE.md`.

## Consequences

- **Easier**: `Users` can now be implemented (profile endpoints, avatar
  upload, preferences) without re-litigating what belongs where, or
  colliding with `Auth`'s fields.
- **Harder**: two modules touch one entity, so a schema change to `User`
  (e.g. renaming `Name`) needs both modules' owners to agree — mitigated by
  the "who writes which field" split above being explicit rather than
  tribal knowledge.
- **Deferred, not resolved**: whether `User` should eventually split into
  separate credential/profile tables is left for later, only if profile
  data actually grows enough to need it.
