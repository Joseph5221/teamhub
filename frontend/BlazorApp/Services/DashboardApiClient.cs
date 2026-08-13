using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlazorApp.Models;

namespace BlazorApp.Services;

public class DashboardApiClient
{
    private readonly HttpClient _http;

    public DashboardApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResult<DashboardResponse>> GetDashboardAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/dashboard");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>(JsonDefaults.Options);
                return ApiResult<DashboardResponse>.Ok(dashboard!);
            }

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonDefaults.Options);
            return ApiResult<DashboardResponse>.Fail(problem?.Detail ?? "Couldn't load dashboard data.");
        }
        catch (HttpRequestException)
        {
            return ApiResult<DashboardResponse>.Fail("Couldn't reach the TeamHub API. Is the server running?");
        }
    }
}
