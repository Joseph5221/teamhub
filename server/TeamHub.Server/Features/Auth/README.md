# Auth Module

**Status:** implemented, dev-mode only (added in commit `2d2bc51`). Login and
register work and issue JWTs. **Not real security yet**: `AuthService.LoginAsync`
accepts any password, and `RegisterAsync` stores the raw password string as
`PasswordHash` — both explicitly `TODO` in the code. See
[server/TeamHub.Server/README.md](../../README.md) for the full API
walkthrough (endpoints, test credentials, Swagger flow).

## Responsibilities

- User authentication (registration/login) — done, dev-mode.
- JWT issuance (`ITokenService`/`JwtTokenService`) — done.
- OAuth linking (Google, GitHub, Microsoft) — not started.
- Session/refresh-token handling — not started.

## Notes

- **Requires a JWT secret to start the app at all**: `Jwt:Secret`/`Issuer`/
  `Audience` are blank in `appsettings*.json` on purpose — set them via
  `dotnet user-secrets` before running (see root `CLAUDE.md`), or
  `AddInfrastructure` throws at startup.
- `Infrastructure/Security/IPasswordHasher` and `PasswordHasher` are still
  empty stubs — implementing and wiring those in is the next real step for
  this module (see [docs/ROADMAP.md](../../../../docs/ROADMAP.md)), before
  anything else builds on top of Auth.
- Boundary with `Users/` isn't defined yet: Auth should own credentials/
  sessions/tokens; `Users/` should own profile data. Write that down (or an
  ADR) once decided — `Users/` is still an empty stub.
- See [docs/ARCHITECTURE.md](../../../../docs/ARCHITECTURE.md) for the
  planned OAuth providers.
