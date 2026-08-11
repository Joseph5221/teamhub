using System.Text.Json.Serialization;

namespace TeamHub.Server.Modules.Integrations.GitHub;

/// <summary>
/// Subset of the GitHub REST API repository object we care about.
/// See https://docs.github.com/en/rest/repos/repos#list-organization-repositories
/// </summary>
public record GitHubRepositoryDto(
    long Id,
    string Name,
    [property: JsonPropertyName("full_name")] string FullName,
    string? Description,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    string? Language,
    [property: JsonPropertyName("stargazers_count")] int StargazersCount,
    [property: JsonPropertyName("open_issues_count")] int OpenIssuesCount,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt,
    [property: JsonPropertyName("private")] bool IsPrivate);
