using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TeamHub.Server.Modules.Auth;
using TeamHub.Server.Modules.Users;

public class UserEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public UserEndpointsIntegrationTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task GetOwnProfile_AsAuthenticatedUser_ReturnsProfile()
    {
        var (userId, email, token) = await RegisterAndLoginAsync("me");

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Get, "/api/users/me", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = JsonSerializer.Deserialize<UserProfileResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        profile.Id.Should().Be(userId);
        profile.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetOwnProfile_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateOwnProfile_WithValidRequest_UpdatesNameAndAvatar()
    {
        var (_, _, token) = await RegisterAndLoginAsync("update");

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Put, "/api/users/me", token,
            new { Name = "Updated Name", AvatarUrl = "https://example.com/a.png" }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = JsonSerializer.Deserialize<UserProfileResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        profile.Name.Should().Be("Updated Name");
        profile.AvatarUrl.Should().Be("https://example.com/a.png");
    }

    [Fact]
    public async Task GetProfile_ByOtherAuthenticatedUser_ReturnsProfile()
    {
        var (targetId, _, _) = await RegisterAndLoginAsync("target");
        var (_, _, viewerToken) = await RegisterAndLoginAsync("viewer");

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Get, $"/api/users/{targetId}", viewerToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = JsonSerializer.Deserialize<UserProfileResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        profile.Id.Should().Be(targetId);
    }

    [Fact]
    public async Task UpdateRole_AsNonAdmin_ReturnsForbidden()
    {
        var (targetId, _, _) = await RegisterAndLoginAsync("target2");
        var (_, _, callerToken) = await RegisterAndLoginAsync("caller");

        var response = await _client.SendAsync(AuthedRequest(HttpMethod.Put, $"/api/users/{targetId}/role", callerToken,
            new { Role = "Admin" }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
