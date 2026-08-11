// TeamHub.Server.Tests/Services/GitHubApiClientTests.cs
using System.Net;
using System.Text;
using FluentAssertions;
using TeamHub.Server.Modules.Integrations;
using TeamHub.Server.Modules.Integrations.GitHub;

public class GitHubApiClientTests
{
    private static GitHubApiClient CreateClient(HttpStatusCode statusCode, string content)
    {
        var handler = new StubHttpMessageHandler(statusCode, content);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        return new GitHubApiClient(httpClient);
    }

    [Fact]
    public async Task GetOrganizationRepositoriesAsync_WithSuccessResponse_ReturnsMappedRepositories()
    {
        var json = """
        [
          {
            "id": 1,
            "name": "octokit.net",
            "full_name": "octokit/octokit.net",
            "description": ".NET GitHub API client",
            "html_url": "https://github.com/octokit/octokit.net",
            "language": "C#",
            "stargazers_count": 2500,
            "open_issues_count": 42,
            "updated_at": "2026-01-01T00:00:00Z",
            "private": false
          }
        ]
        """;
        var sut = CreateClient(HttpStatusCode.OK, json);

        var repos = await sut.GetOrganizationRepositoriesAsync("octokit", null);

        repos.Should().ContainSingle();
        repos[0].FullName.Should().Be("octokit/octokit.net");
        repos[0].StargazersCount.Should().Be(2500);
    }

    [Fact]
    public async Task GetOrganizationRepositoriesAsync_With404_ThrowsOrganizationNotFound()
    {
        var sut = CreateClient(HttpStatusCode.NotFound, "{}");

        var act = () => sut.GetOrganizationRepositoriesAsync("does-not-exist", null);

        var ex = await act.Should().ThrowAsync<ModuleConnectorException>();
        ex.Which.Code.Should().Be("GitHub.OrganizationNotFound");
    }

    [Fact]
    public async Task GetOrganizationRepositoriesAsync_With401_ThrowsUnauthorized()
    {
        var sut = CreateClient(HttpStatusCode.Unauthorized, "{}");

        var act = () => sut.GetOrganizationRepositoriesAsync("octokit", "bad-token");

        var ex = await act.Should().ThrowAsync<ModuleConnectorException>();
        ex.Which.Code.Should().Be("GitHub.Unauthorized");
    }

    [Fact]
    public async Task GetOrganizationRepositoriesAsync_With500_ThrowsApiError()
    {
        var sut = CreateClient(HttpStatusCode.InternalServerError, "{}");

        var act = () => sut.GetOrganizationRepositoriesAsync("octokit", null);

        var ex = await act.Should().ThrowAsync<ModuleConnectorException>();
        ex.Which.Code.Should().Be("GitHub.ApiError");
    }

    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
