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
- .NET 8 SDK
- Your favorite IDE (Visual Studio, VS Code, or Rider)

### Setup Steps

1. **Restore packages**
   ```bash
   dotnet restore
   ```

2. **Run the application**
   ```bash
   dotnet run
   ```

3. **Access Swagger UI**
   - Navigate to: `https://localhost:5001` (or the port shown in console)
   - Swagger UI will open automatically in development mode

## 🔐 Authentication

The API uses JWT (JSON Web Tokens) for authentication.

### Quick Start (Development)

**Test User (Pre-seeded)**
- Email: `test@teamhub.com`
- Password: Any password (validation disabled for development)

### API Flow

1. **Login** → POST `/api/auth/login`
   ```json
   {
     "email": "test@teamhub.com",
     "password": "anything"
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
  - Login (simplified - any password works)
  - Register new users
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

Currently using **In-Memory Database** for development:
- No setup required
- Data resets on restart
- Pre-seeded with test user and sample integrations

### Switch to SQLite (Optional)

1. In `ServiceCollectionExtensions.cs`, uncomment SQLite configuration
2. Run migrations:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

## 📝 API Endpoints

### Authentication
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/register` - Register new user

### Dashboard
- `GET /api/dashboard` - Get user dashboard (requires auth)

### Health
- `GET /health` - Health check endpoint

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
1. Add password hashing (bcrypt/PBKDF2)
2. Add password validation
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