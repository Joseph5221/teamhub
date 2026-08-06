# TeamHub Server - Feature-Based Minimal API

A modern ASP.NET Core Minimal API built with feature-based architecture.

## 🏗️ Architecture

This project uses **Feature-Based Architecture**, organizing code by business features rather than technical layers:

```
Features/
├── Auth/          - Authentication & JWT tokens
├── Dashboard/     - User dashboard with integration TODO items
├── Teams/         - Team management (TODO)
├── Projects/      - Project management (TODO)
├── Integrations/  - Third-party integrations (TODO)
└── Users/         - User management (TODO)
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

### 🚧 Coming Soon (Placeholders Ready)
- Teams CRUD
- Projects CRUD
- Integrations management
- User management

## 🗄️ Database

Using EF Core's **in-memory provider**, deliberately, for now — see
[ADR 0002](../../docs/adr/0002-in-memory-database-for-now.md):
- No setup required
- Data resets on restart, and reseeds automatically (Development only)
- Pre-seeded with 3 users, 2 teams, 3 projects, 4 integrations — see
  `Infrastructure/Data/DbInitializer.cs`; reset it on demand with
  `./scripts/seed-db.sh` without restarting the process
- No migrations exist yet; `scripts/reset-db.sh` explains why there's
  nothing to migrate today (that's separate from seeding — it's about
  schema, not test data)

## 📝 API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/register` - Register new user

### Dashboard
- `GET /api/dashboard` - Get user dashboard (requires auth)

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
├── Features/              # Feature modules
│   ├── Auth/             # Authentication
│   └── Dashboard/        # Dashboard
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
1. Create Teams feature (copy Auth/Dashboard pattern)
2. Create Projects feature
3. Create Integrations feature
4. Create Users feature

### Phase 2: Enhance Auth
1. ~~Add password hashing~~ — done, see `Infrastructure/Security/PasswordHasher.cs`
2. ~~Add password validation~~ — done, see `Infrastructure/Security/PasswordValidator.cs` (8-128 chars, at least one letter and one digit)
3. Add refresh tokens
4. Add email verification

### Phase 3: Add Real Integrations
1. GitHub API integration
2. Jira API integration
3. Slack webhook integration

## 💡 Development Tips

### Adding a New Feature

1. Create folder in `Features/`
2. Add DTOs file (`FeatureDtos.cs`)
3. Add service interface (`IFeatureService.cs`)
4. Add service implementation (`FeatureService.cs`)
5. Add endpoints (`FeatureEndpoints.cs`)
6. Register service in `ServiceCollectionExtensions.cs`
7. Map endpoints in `Program.cs`

### Example: Adding Teams Feature

```csharp
// Features/Teams/TeamEndpoints.cs
public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams")
            .WithTags("Teams")
            .RequireAuthorization();
            
        group.MapGet("/", GetTeams);
        group.MapPost("/", CreateTeam);
    }
}

// Register in Program.cs
app.MapTeamEndpoints();
```

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