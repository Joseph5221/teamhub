using TeamHub.Server.Extensions;
using TeamHub.Server.Features.Auth;
using TeamHub.Server.Features.Dashboard;
using TeamHub.Server.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.AddApiDocumentation();

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
        await DbInitializer.SeedAsync(db);
    }

    // Dev convenience: reset + reseed without restarting the process.
    // scripts/seed-db.sh wraps this.
    app.MapPost("/api/dev/reseed", async (AppDbContext db) =>
    {
        db.Integrations.RemoveRange(db.Integrations);
        db.Projects.RemoveRange(db.Projects);
        db.Teams.RemoveRange(db.Teams);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        await DbInitializer.SeedAsync(db);
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
// app.MapTeamEndpoints();
// app.MapProjectEndpoints();
// app.MapIntegrationEndpoints();
// app.MapUserEndpoints();

app.Run();

public partial class Program { }
