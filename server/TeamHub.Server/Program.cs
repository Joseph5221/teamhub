using System.Text.Json.Serialization;
using TeamHub.Server.Extensions;
using TeamHub.Server.Modules.Auth;
using TeamHub.Server.Modules.Dashboard;
using TeamHub.Server.Modules.Integrations;
using TeamHub.Server.Modules.Teams;
using TeamHub.Server.Infrastructure.Data;
using TeamHub.Server.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.AddApiDocumentation();

// Enums (e.g. IntegrationType/IntegrationStatus on IntegrationResponse) go
// over the wire as their string names ("VersionControl"), not raw ints —
// friendlier for API consumers and Swagger docs.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seed dev test data (in-memory DB is empty on every process start —
    // see docs/adr/0002-in-memory-database-for-now.md). Idempotent.
    using (var seedScope = app.Services.CreateScope())
    {
        var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbInitializer.SeedAsync(db, passwordHasher);
    }

    // Dev convenience: reset + reseed without restarting the process.
    // scripts/seed-db.sh wraps this.
    app.MapPost("/api/dev/reseed", async (AppDbContext db, IPasswordHasher passwordHasher) =>
    {
        db.Integrations.RemoveRange(db.Integrations);
        db.Projects.RemoveRange(db.Projects);
        db.Teams.RemoveRange(db.Teams);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        await DbInitializer.SeedAsync(db, passwordHasher);
        return Results.Ok(new { message = "Database reseeded." });
    })
    .WithTags("Dev")
    .WithOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map feature endpoints
app.MapAuthEndpoints();
app.MapDashboardEndpoints();
app.MapTeamEndpoints();
app.MapIntegrationEndpoints();
// app.MapProjectEndpoints();
// app.MapUserEndpoints();

app.Run();

public partial class Program { }
