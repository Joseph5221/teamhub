using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TeamHub.Server.Modules.Auth;
using TeamHub.Server.Modules.Teams;

public class TeamEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _client;

    public TeamEndpointsIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task CreateTeam_AsAuthenticatedUser_ReturnsCreatedWithCallerAsOwner()
    {
        var (userId, _, token) = await RegisterAndLoginAsync("owner");

        var team = await CreateTeamAsync(token, "Platform");

        team.OwnerId.Should().Be(userId);
        team.Members.Should().ContainSingle(m => m.UserId == userId && m.Role == "Owner");
    }

    [Fact]
    public async Task CreateTeam_WithoutAuth_ReturnsUnauthorized()
    {
        var content = new StringContent(
            JsonSerializer.Serialize(new { Name = "NoAuth", Description = "" }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/teams", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddMember_AsOwner_AddsUserAndListsAsMember()
    {
        var (_, _, ownerToken) = await RegisterAndLoginAsync("owner");
        var (memberId, memberEmail, _) = await RegisterAndLoginAsync("member");
        var team = await CreateTeamAsync(ownerToken, "Growth");

        var addResponse = await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/members", ownerToken,
            new { Email = memberEmail }));
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var membersResponse = await _client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/teams/{team.Id}/members", ownerToken));
        var members = JsonSerializer.Deserialize<List<TeamMemberResponse>>(
            await membersResponse.Content.ReadAsStringAsync(), JsonOptions)!;

        members.Should().Contain(m => m.UserId == memberId && m.Role == "Member");
    }

    [Fact]
    public async Task AddMember_AsNonOwnerMember_ReturnsForbidden()
    {
        var (_, _, ownerToken) = await RegisterAndLoginAsync("owner");
        var (_, memberEmail, memberToken) = await RegisterAndLoginAsync("member");
        var (_, outsiderEmail, _) = await RegisterAndLoginAsync("outsider");
        var team = await CreateTeamAsync(ownerToken, "Infra");

        await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/members", ownerToken,
            new { Email = memberEmail }));

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Post, $"/api/teams/{team.Id}/members", memberToken,
            new { Email = outsiderEmail }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTeam_AsNonMember_ReturnsForbidden()
    {
        var (_, _, ownerToken) = await RegisterAndLoginAsync("owner");
        var (_, _, outsiderToken) = await RegisterAndLoginAsync("outsider");
        var team = await CreateTeamAsync(ownerToken, "Secret");

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/teams/{team.Id}", outsiderToken));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_OwnerCannotRemoveThemselves()
    {
        var (userId, _, token) = await RegisterAndLoginAsync("owner");
        var team = await CreateTeamAsync(token, "Solo");

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Delete, $"/api/teams/{team.Id}/members/{userId}", token));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
