using TeamHub.Server.Infrastructure.Data;

namespace TeamHub.Server.Extensions;

/// <summary>
/// Extension methods for configuring the web application
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Initializes the database with seed data
    /// </summary>
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure database is created (for in-memory or file-based databases)
        await context.Database.EnsureCreatedAsync();
        
        return app;
    }

    /// <summary>
    /// Configures the middleware pipeline
    /// </summary>
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "TeamHub API v1");
                options.RoutePrefix = string.Empty; // Swagger at root
            });
        }

        app.UseHttpsRedirection();
        
        // CORS - configure as needed
        app.UseCors(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}