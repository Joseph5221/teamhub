using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Domain.Enums;
using TeamHub.Server.Infrastructure.Data;

namespace TeamHub.Server.Modules.Integrations.GitHub;

/// <summary>
/// <see cref="IModuleConnector"/> for GitHub. Auth is a personal access
/// token stored (per-team) as JSON in <see cref="Integration.ConfigurationData"/>
/// under the "personalAccessToken" key, alongside an "organization" key
/// naming the org/user whose repos to pull — see
/// docs/ARCHITECTURE.md "Resilience & Testing" for why HTTP calls go
/// through a Polly-wrapped typed client (<see cref="IGitHubApiClient"/>)
/// rather than straight to GitHub.
/// </summary>
public class GitHubConnector : IModuleConnector
{
    private const string OrganizationKey = "organization";
    private const string PersonalAccessTokenKey = "personalAccessToken";

    private readonly AppDbContext _context;
    private readonly IGitHubApiClient _apiClient;

    public GitHubConnector(AppDbContext context, IGitHubApiClient apiClient)
    {
        _context = context;
        _apiClient = apiClient;
    }

    public IntegrationType Type => IntegrationType.VersionControl;

    public async Task<ModuleConfig> GetConfigAsync(Guid teamId)
    {
        var config = await LoadConfigAsync(teamId);

        var fields = new Dictionary<string, string?>
        {
            [OrganizationKey] = config.Organization,
            ["hasPersonalAccessToken"] = (!string.IsNullOrWhiteSpace(config.PersonalAccessToken)).ToString()
        };

        return new ModuleConfig(!string.IsNullOrWhiteSpace(config.Organization), fields);
    }

    public async Task<ModuleData> GetDataAsync(Guid teamId, DateTime? since = null)
    {
        var config = await LoadConfigAsync(teamId);

        if (string.IsNullOrWhiteSpace(config.Organization))
        {
            throw new ModuleConnectorException(
                "GitHub.NotConfigured",
                "This team's GitHub integration has no organization configured yet.");
        }

        var repos = await _apiClient.GetOrganizationRepositoriesAsync(config.Organization, config.PersonalAccessToken);

        var items = repos
            .Where(r => since == null || r.UpdatedAt >= since)
            .Select(r => new ModuleDataItem(
                Id: r.Id.ToString(),
                Title: r.FullName,
                Description: r.Description,
                Url: r.HtmlUrl,
                UpdatedAt: r.UpdatedAt,
                Metadata: new Dictionary<string, string>
                {
                    ["language"] = r.Language ?? "Unknown",
                    ["stars"] = r.StargazersCount.ToString(),
                    ["openIssues"] = r.OpenIssuesCount.ToString(),
                    ["visibility"] = r.IsPrivate ? "private" : "public"
                }))
            .ToList();

        return new ModuleData(DateTime.UtcNow, items);
    }

    public async Task InvokeActionAsync(Guid teamId, string action)
    {
        if (!string.Equals(action, "sync", StringComparison.OrdinalIgnoreCase))
        {
            throw new ModuleConnectorException(
                "GitHub.UnsupportedAction",
                $"GitHub connector does not support action '{action}'. Supported actions: sync.");
        }

        // Round-trips the real API so a failure (bad org, revoked token)
        // surfaces as a thrown ModuleConnectorException before we mark the
        // integration Connected.
        await GetDataAsync(teamId);

        var integration = await FindIntegrationAsync(teamId);
        if (integration != null)
        {
            integration.LastSyncedAt = DateTime.UtcNow;
            integration.Status = IntegrationStatus.Connected;
            await _context.SaveChangesAsync();
        }
    }

    private async Task<GitHubIntegrationConfig> LoadConfigAsync(Guid teamId)
    {
        var integration = await FindIntegrationAsync(teamId);
        if (integration == null || string.IsNullOrWhiteSpace(integration.ConfigurationData))
        {
            return new GitHubIntegrationConfig(null, null);
        }

        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string?>>(integration.ConfigurationData);
            if (raw == null)
            {
                return new GitHubIntegrationConfig(null, null);
            }

            raw.TryGetValue(OrganizationKey, out var organization);
            raw.TryGetValue(PersonalAccessTokenKey, out var token);
            return new GitHubIntegrationConfig(organization, token);
        }
        catch (JsonException)
        {
            return new GitHubIntegrationConfig(null, null);
        }
    }

    private Task<Integration?> FindIntegrationAsync(Guid teamId) =>
        _context.Integrations.FirstOrDefaultAsync(i => i.TeamId == teamId && i.Type == IntegrationType.VersionControl);

    private record GitHubIntegrationConfig(string? Organization, string? PersonalAccessToken);
}
