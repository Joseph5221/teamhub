# TeamHub Server - Feature-Based Minimal API

A modern ASP.NET Core Minimal API built with feature-based architecture.

## 🏗️ Architecture

This project uses **Feature-Based Architecture**, organizing code by business features rather than technical layers, nested under `Modules/` (see [ADR 0004](../../docs/adr/0004-modules-folder-nesting.md)):

```
Modules/
├── Auth/          - Authentication & JWT tokens
├── Dashboard/     - User dashboard with integration TODO items
├── Teams/         - Team creation & membership (owner/member roles)
├── Projects/      - Team-scoped, integration-aggregating project records — see ADR 0006
├── Integrations/  - Third-party integrations; GitHub connector implemented, see Modules/Integrations/README.md
└── Users/         - User profile data & system-wide role assignment, split from Auth per ADR 0005
```

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK **and** a .NET 8 runtime — check with `dotnet --list-runtimes`
  for a `Microsoft.AspNetCore.App 8.x` line. If it's missing, see
  [ADR 0003](../../docs/adr/0003-target-net8-and-install-the-runtime.md);
  `dotnet build` can succeed on a newer SDK alone but `dotnet run`/`dotnet
  test` need the matching runtime.
- Your favorite IDE (Visual Studio, VS Code, or Rider)

### Setup Steps

The fastest path is the repo-root setup script, which checks your runtime
and sets dev JWT secrets for you:

```bash
./scripts/setup-dev.sh
```

Or by hand:

1. **JWT secrets** (the app throws at startup without these — see
   [Configuration](#-configuration)):
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Secret" "<a long random dev value>"
   dotnet user-secrets set "Jwt:Issuer" "TeamHub-Dev"
   dotnet user-secrets set "Jwt:Audience" "TeamHub-Dev"
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access Swagger UI**
   - Navigate to: `https://localhost:7073` (http fallback `http://localhost:5069`
     — see `Properties/launchSettings.json` if these ever change)
   - Swagger UI will open automatically in development mode

## 🔐 Authentication

The API uses JWT (JSON Web Tokens) for authentication.

### Quick Start (Development)

**Test Users (Pre-seeded)** — password `password123` for all of them (see
`DbInitializer.SeedUserPassword`):
- `test@teamhub.com` (Admin, owns the "Platform" team)
- `bob@teamhub.com` (member of both seeded teams)
- `carol@teamhub.com` (owns the "Growth" team)

Seeded automatically on startup (`DbInitializer.SeedAsync`, called from
`Program.cs`, Development only) with 2 teams, 3 projects, and 4 integrations
spread across them. Reset to a clean slate without restarting the process:
`./scripts/seed-db.sh` (or `POST /api/dev/reseed` directly).

### API Flow

1. **Login** → POST `/api/auth/login`
   ```json
   {
     "email": "test@teamhub.com",
     "password": "password123"
   }
   ```
   
   Response includes JWT token:
   ```json
   {
     "userId": "...",
     "email": "test@teamhub.com",
     "name": "Test User",
     "token": "eyJhbGc...",
     "role": "Admin"
   }
   ```

2. **Use Token** → Add to requests:
   - Header: `Authorization: Bearer <token>`
   - In Swagger: Click "Authorize" button, enter: `Bearer <token>`

3. **Get Dashboard** → GET `/api/dashboard`
   - Requires authentication
   - Returns user info, integration TODO items, and stats

## 📊 Current Features

### ✅ Implemented
- **Auth Feature**
  - Login (password verified against a PBKDF2 hash via `IPasswordHasher`)
  - Register new users (password validated via `IPasswordValidator`, then hashed on write)
  - JWT token generation

- **Dashboard Feature**
  - User information
  - Integration TODO list (GitHub, Jira, Slack placeholders)
  - Basic stats (teams, projects, integrations)

- **Teams Feature** (see `Modules/Teams/README.md` for full detail)
  - Create a team (caller becomes owner)
  - Get team details / list members (members only)
  - Update team settings (owner only)
  - Add an existing user by email / remove a member (owner only)
  - Two roles: owner (`Team.OwnerId`) and member (`Team.Members`, includes the owner)

- **Integrations Feature** (see `Modules/Integrations/README.md` for full detail)
  - `IModuleConnector` — shared connector contract every integration submodule implements
  - CRUD for a team's configured integrations, owner manages config / any member can view + trigger data/action calls
  - **GitHub connector implemented** (`Modules/Integrations/GitHub/`): lists an org's repos via the real GitHub REST API, personal-access-token auth (optional — unauthenticated works for public orgs), Polly retry + circuit-breaker around the HTTP calls
  - Jira/Slack/Calendar types are accepted by the CRUD endpoints but have no connector yet — data/action calls on them return `501 Integration.NotSupported` until one is built (copy the GitHub pattern)

- **Projects Feature** (see `Modules/Projects/README.md` for full detail)
  - Team-scoped project CRUD — create/list/get/update/delete, same owner-manages/member-views permission shape as Teams/Integrations
  - `Project.Status` is a real enum (`ProjectStatus`) — Planned/Active/OnHold/Completed/Cancelled
  - Integration-data linking still not decided (see [ADR 0006](../../docs/adr/0006-projects-definition.md)) — left for whoever attaches the first real integration's data to a project

- **Users Feature** (see `Modules/Users/README.md` for full detail)
  - `GET/PUT /api/users/me` — view/update the caller's own profile (display name, avatar URL)
  - `GET /api/users/{userId}` — view any user's public profile (basic directory, already-visible data via team member lists)
  - `PUT /api/users/{userId}/role` — assign a user's system-wide role (`UserRole`: Member/Admin), admin only
  - Avatar is a URL field the user supplies — no file upload/blob storage yet

### 🚧 Coming Soon (Placeholders Ready)
- Jira/Slack/Calendar connectors — copy the GitHub connector's shape (see `Modules/Integrations/GitHub/README.md`)

## 🗄️ Database

Using EF Core's **in-memory provider**, deliberately, for now — see
[ADR 0002](../../docs/adr/0002-in-memory-database-for-now.md):
- No setup required
- Data resets on restart, and reseeds automatically (Development only)
- Pre-seeded with 3 users, 2 teams, 3 projects, 4 integrations — see
  `Infrastructure/Data/DbInitializer.cs`; reset it on demand with
  `./scripts/seed-db.sh` without restarting the process. The Growth team's
  GitHub integration is pre-configured with a real public org
  (`{"organization":"octokit"}`, no token) so its
  `GET /api/teams/{teamId}/integrations/{integrationId}/data` endpoint
  returns real sample repo data immediately — useful for frontend widget
  work without minting a personal access token first.
- No migrations exist yet; `scripts/reset-db.sh` explains why there's
  nothing to migrate today (that's separate from seeding — it's about
  schema, not test data)

## 📝 API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/register` - Register new user

### Dashboard
- `GET /api/dashboard` - Get user dashboard (requires auth)

### Teams (all require auth)
- `POST /api/teams` - Create a team (caller becomes owner)
- `GET /api/teams/{teamId}` - Get team details (members only)
- `PUT /api/teams/{teamId}` - Update team settings (owner only)
- `GET /api/teams/{teamId}/members` - List members (members only)
- `POST /api/teams/{teamId}/members` - Add an existing user by email (owner only)
- `DELETE /api/teams/{teamId}/members/{memberUserId}` - Remove a member (owner only)

### Integrations (all require auth; see `Modules/Integrations/README.md`)
- `POST /api/teams/{teamId}/integrations` - Configure a new integration (owner only)
- `GET /api/teams/{teamId}/integrations` - List a team's integrations (members only)
- `GET /api/teams/{teamId}/integrations/{integrationId}` - Get integration details (members only)
- `PUT /api/teams/{teamId}/integrations/{integrationId}` - Update settings/configuration (owner only)
- `DELETE /api/teams/{teamId}/integrations/{integrationId}` - Remove an integration (owner only)
- `GET /api/teams/{teamId}/integrations/{integrationId}/data?since=` - Fetch normalized data from the connector (members only; `501` if no connector exists for that type yet)
- `POST /api/teams/{teamId}/integrations/{integrationId}/actions` - Invoke a connector action, e.g. `{"action":"sync"}` (members only)

### Projects (all require auth; see `Modules/Projects/README.md`)
- `POST /api/teams/{teamId}/projects` - Create a project (owner only)
- `GET /api/teams/{teamId}/projects` - List a team's projects (members only)
- `GET /api/teams/{teamId}/projects/{projectId}` - Get project details (members only)
- `PUT /api/teams/{teamId}/projects/{projectId}` - Update a project (owner only)
- `DELETE /api/teams/{teamId}/projects/{projectId}` - Remove a project (owner only)

### Users (all require auth; see `Modules/Users/README.md`)
- `GET /api/users/me` - Get the caller's own profile
- `PUT /api/users/me` - Update the caller's own profile (display name, avatar URL)
- `GET /api/users/{userId}` - Get a user's public profile
- `PUT /api/users/{userId}/role` - Assign a user's system-wide role (admin only)

### Dev-only (Development environment only, not authenticated)
- `POST /api/dev/reseed` - Clear and reseed the in-memory database with test data

(No `/health` endpoint exists yet — it's not wired up in `Program.cs`.)

## 🧪 Testing with Swagger

1. Start the application
2. Open Swagger UI (opens automatically)
3. Click "Authorize" button
4. Login to get a token
5. Copy the token from response
6. Click "Authorize" again, enter: `Bearer <token>`
7. Try the dashboard endpoint

## 📂 Project Structure

```
TeamHub.Server/
├── Modules/               # Feature modules (see ADR 0004)
│   ├── Auth/             # Authentication
│   ├── Dashboard/        # Dashboard
│   ├── Teams/            # Team management (owner/member roles)
│   ├── Users/            # User profile data & role assignment (see ADR 0005)
│   ├── Projects/         # Team-scoped project records (see ADR 0006)
│   └── Integrations/     # Third-party integrations — GitHub connector implemented
│       └── GitHub/       # GitHub REST API connector (Polly retry/circuit-breaker)
├── Domain/               # Domain models
│   ├── Entities/         # Database entities
│   ├── Common/           # Shared domain logic
│   └── Enums/            # Enumerations
├── Infrastructure/       # Infrastructure concerns
│   ├── Data/            # Database context
│   ├── Security/        # JWT, password hashing
│   └── Services/        # Cross-cutting services
├── Extensions/          # Service registration
└── Program.cs          # Application entry point
```

## 🛠️ Next Steps

### Phase 1: Build More Features
1. ~~Create Teams feature~~ — done, see `Modules/Teams/README.md`
2. ~~Create Projects feature~~ — done, see `Modules/Projects/README.md`
3. ~~Create Integrations feature~~ — done (GitHub connector), see `Modules/Integrations/README.md`
4. ~~Create Users feature~~ — done, see `Modules/Users/README.md`

### Phase 2: Enhance Auth
1. ~~Add password hashing~~ — done, see `Infrastructure/Security/PasswordHasher.cs`
2. ~~Add password validation~~ — done, see `Infrastructure/Security/PasswordValidator.cs` (8-128 chars, at least one letter and one digit)
3. Add refresh tokens
4. Add email verification

### Phase 3: Add Real Integrations
1. ~~GitHub API integration~~ — done, see `Modules/Integrations/GitHub/README.md`
2. Jira API integration — copy the GitHub connector's shape
3. Slack webhook integration — copy the GitHub connector's shape

## 💡 Development Tips

### Adding a New Feature

1. Create folder in `Modules/`
2. Add DTOs file (`FeatureDtos.cs`)
3. Add service interface (`IFeatureService.cs`)
4. Add service implementation (`FeatureService.cs`)
5. Add endpoints (`FeatureEndpoints.cs`)
6. Register service in `ServiceCollectionExtensions.cs`
7. Map endpoints in `Program.cs`

### Example: `Modules/Teams` as a reference implementation

`Modules/Teams` (alongside `Modules/Auth`/`Modules/Dashboard`) is now a real,
tested implementation of the pattern above — copy its shape
(`TeamDtos.cs`/`ITeamService.cs`/`TeamService.cs`/`TeamEndpoints.cs`, the
`Result<T>`/`Error` pattern, `ClaimsPrincipal` → JWT `sub` claim for the
current user) when building `Projects` or `Users` next.

### Example: `Modules/Integrations/GitHub` as the reference connector

When building the next connector (Jira, Slack, Calendar), copy
`Modules/Integrations/GitHub/`'s shape: an `I<X>ApiClient`/`<X>ApiClient`
typed HTTP client wrapping the third-party REST API (registered with Polly
retry/circuit-breaker in `ServiceCollectionExtensions.AddFeatures`), a
`<X>Connector : IModuleConnector` that loads per-team config from
`Integration.ConfigurationData` and translates API/config errors into
`ModuleConnectorException`, and DTOs matching the external API's JSON shape.
`IntegrationService` picks up any `IModuleConnector` registered in DI
automatically — no dispatch code to touch. See
`Modules/Integrations/GitHub/README.md` for the full breakdown.

## 🔧 Configuration

See `appsettings.json` for:
- JWT settings
- Database connection strings
- Logging levels

## 📚 Learning Resources

- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Feature-Based Organization](https://www.tessferrandez.com/blog/2023/10/31/organizing-minimal-apis.html)
- [JWT Authentication in .NET](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)

## 🤝 Contributing

This is a learning project! Feel free to:
- Add new features
- Improve existing code
- Add tests
- Update documentation

## 📄 License

MIT License - feel free to use this as a template for your own projects!