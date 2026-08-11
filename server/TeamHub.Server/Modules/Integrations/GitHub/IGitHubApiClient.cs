namespace TeamHub.Server.Modules.Integrations.GitHub;

/// <summary>
/// Thin wrapper over the GitHub REST API. Kept separate from
/// <see cref="GitHubConnector"/> so the HTTP concerns (auth header,
/// pagination, error mapping) can be unit-tested/mocked independently of
/// the connector's config-loading logic.
/// </summary>
public interface IGitHubApiClient
{
    /// <summary>
    /// Lists an organization's repositories. <paramref name="personalAccessToken"/>
    /// is optional — GitHub allows unauthenticated reads of public repos at a
    /// lower rate limit, which is enough for the seeded dev sample data.
    /// </summary>
    Task<IReadOnlyList<GitHubRepositoryDto>> GetOrganizationRepositoriesAsync(
        string organization,
        string? personalAccessToken,
        CancellationToken cancellationToken = default);
}
