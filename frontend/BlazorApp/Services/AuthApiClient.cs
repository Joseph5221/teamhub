using System.Net.Http.Json;
using BlazorApp.Models;

namespace BlazorApp.Services;

public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request)
        => PostAsync("api/auth/login", request);

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request)
        => PostAsync("api/auth/register", request);

    private async Task<ApiResult<AuthResponse>> PostAsync(string url, object request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(url, request, JsonDefaults.Options);

            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonDefaults.Options);
                return ApiResult<AuthResponse>.Ok(auth!);
            }

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonDefaults.Options);
            return ApiResult<AuthResponse>.Fail(problem?.Detail ?? "Something went wrong. Please try again.");
        }
        catch (HttpRequestException)
        {
            return ApiResult<AuthResponse>.Fail("Couldn't reach the TeamHub API. Is the server running?");
        }
    }
}
