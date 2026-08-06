# Auth Module

**Status:** implemented, dev-mode-appropriate. Login and register work and
issue JWTs, with real password hashing (`IPasswordHasher`/`PasswordHasher`,
wrapping `Microsoft.AspNetCore.Identity.PasswordHasher<T>`) and validation
(`IPasswordValidator`/`PasswordValidator`, 8-128 chars + at least one letter
and one digit). See [server/TeamHub.Server/README.md](../../README.md) for
the full API walkthrough (endpoints, test credentials, Swagger flow).

## Responsibilities

- User authentication (registration/login), including password hashing and
  validation — done.
- JWT issuance (`ITokenService`/`JwtTokenService`) — done.
- OAuth linking (Google, GitHub, Microsoft) — not started.
- Session/refresh-token handling — not started.

## Notes

- **Requires a JWT secret to start the app at all**: `Jwt:Secret`/`Issuer`/
  `Audience` are blank in `appsettings*.json` on purpose — set them via
  `dotnet user-secrets` before running (see root `CLAUDE.md`), or
  `AddInfrastructure` throws at startup.
- Boundary with `Modules/Users` is decided — see
  [ADR 0005](../../../../docs/adr/0005-auth-users-boundary.md): Auth owns
  credentials/sessions/tokens; `Users` owns profile data (display name,
  avatar, preferences). Both still operate on the single `User` entity for
  now — the split is a module/service ownership convention, not a schema
  split.
- Missing next: refresh tokens, email verification — see
  [docs/ROADMAP.md](../../../../docs/ROADMAP.md).
- See [docs/ARCHITECTURE.md](../../../../docs/ARCHITECTURE.md) for the
  planned OAuth providers.
