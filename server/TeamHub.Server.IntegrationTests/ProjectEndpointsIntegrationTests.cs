using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TeamHub.Server.Modules.Auth;
using TeamHub.Server.Modules.Projects;
using TeamHub.Server.Modules.Teams;

public class ProjectEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ProjectEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(Guid UserId, string Email, string Token)> RegisterAndLoginAsync(string emailPrefix)
    {
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@test.com";
        var registerRequest = new { Email = email, Name = "Test User", Password = "password123" };

        var response = await _client.PostAsync("/api/auth/register",
            new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var auth = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        return (auth.UserId, email, auth.Token);
    }

    private static HttpRequestMessage AuthedRequest(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };

        if (body != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task<TeamResponse> CreateTeamAsync(string token, string name)
    {
        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Post, "/api/teams", token,
            new { Name = name, Description = "" }));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return JsonSerializer.Deserialize<TeamResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    [Fact]
    public async Task CreateProject_AsOwner_ReturnsCreated()
    {
        var (_, _, token) = await RegisterAndLoginAsync("owner");
        var team = await CreateTeamAsync(token, "Platform");

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/projects", token,
            new { Name = "Dashboard Revamp", Description = "Rebuild the UI", StartDate = (DateTime?)null, EndDate = (DateTime?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = JsonSerializer.Deserialize<ProjectResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        project.Name.Should().Be("Dashboard Revamp");
        project.TeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task CreateProject_WithoutAuth_ReturnsUnauthorized()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { Name = "NoAuth", Description = "" }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync($"/api/teams/{Guid.NewGuid()}/projects", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_AsNonOwnerMember_ReturnsForbidden()
    {
        var (_, _, ownerToken) = await RegisterAndLoginAsync("owner2");
        var (_, memberEmail, memberToken) = await RegisterAndLoginAsync("member2");
        var team = await CreateTeamAsync(ownerToken, "Growth");
        await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/members", ownerToken,
            new { Email = memberEmail }));

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/projects", memberToken,
            new { Name = "Sneaky", Description = "" }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProjects_AsMember_ListsProjects()
    {
        var (_, _, ownerToken) = await RegisterAndLoginAsync("owner3");
        var (memberId, memberEmail, memberToken) = await RegisterAndLoginAsync("member3");
        var team = await CreateTeamAsync(ownerToken, "Infra");
        await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/members", ownerToken,
            new { Email = memberEmail }));
        await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/projects", ownerToken,
            new { Name = "Alpha", Description = "" }));

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/teams/{team.Id}/projects", memberToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var projects = JsonSerializer.Deserialize<List<ProjectResponse>>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        projects.Should().Contain(p => p.Name == "Alpha");
        memberId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProjects_AsNonMember_ReturnsForbidden()
    {
        var (_, _, ownerToken) = await RegisterAndLoginAsync("owner4");
        var (_, _, outsiderToken) = await RegisterAndLoginAsync("outsider4");
        var team = await CreateTeamAsync(ownerToken, "Secret");

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/teams/{team.Id}/projects", outsiderToken));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProject_AsOwner_RemovesProject()
    {
        var (_, _, ownerToken) = await RegisterAndLoginAsync("owner5");
        var team = await CreateTeamAsync(ownerToken, "Solo");
        var createResponse = await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/projects", ownerToken,
            new { Name = "Doomed", Description = "" }));
        var project = JsonSerializer.Deserialize<ProjectResponse>(await createResponse.Content.ReadAsStringAsync(), JsonOptions)!;

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Delete, $"/api/teams/{team.Id}/projects/{project.Id}", ownerToken));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
