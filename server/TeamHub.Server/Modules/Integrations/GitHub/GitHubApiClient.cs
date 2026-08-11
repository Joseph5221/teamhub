using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TeamHub.Server.Modules.Integrations.GitHub;

/// <summary>
/// Calls the real GitHub REST API. Registered as a typed <see cref="HttpClient"/>
/// (base address + Polly retry/circuit-breaker configured in
/// ServiceCollectionExtensions.AddFeatures) — see docs/ARCHITECTURE.md
/// "Resilience & Testing".
/// </summary>
public class GitHubApiClient : IGitHubApiClient
{
    private readonly HttpClient _httpClient;

    public GitHubApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<GitHubRepositoryDto>> GetOrganizationRepositoriesAsync(
        string organization,
        string? personalAccessToken,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"orgs/{Uri.EscapeDataString(organization)}/repos?per_page=100&sort=updated&type=all");

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        if (!string.IsNullOrWhiteSpace(personalAccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ModuleConnectorException(
                "GitHub.OrganizationNotFound",
                $"GitHub organization '{organization}' was not found.");
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new ModuleConnectorException(
                "GitHub.Unauthorized",
                "GitHub rejected the request — check the configured personal access token and its scopes.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ModuleConnectorException(
                "GitHub.ApiError",
                $"GitHub API returned {(int)response.StatusCode} for organization '{organization}'.");
        }

        var repos = await response.Content.ReadFromJsonAsync<List<GitHubRepositoryDto>>(cancellationToken: cancellationToken);
        return repos ?? new List<GitHubRepositoryDto>();
    }
}
