using Polly;
using Polly.Extensions.Http;

namespace TeamHub.Server.Modules.Integrations.GitHub;

/// <summary>
/// Retry/circuit-breaker policies for the GitHub HTTP client, per
/// docs/ARCHITECTURE.md "Resilience & Testing" ("Retry/circuit-breaker via
/// Polly for external integrations"). Kept as a standalone static class so
/// they're unit-testable and reusable if another connector needs the same
/// shape later.
/// </summary>
public static class GitHubResiliencePolicies
{
    /// <summary>
    /// 3 retries with exponential backoff (2s, 4s, 8s) on transient HTTP
    /// failures (5xx, request timeout) or a 429 rate-limit response.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    /// <summary>
    /// Opens the circuit for 30s after 5 consecutive transient failures, so
    /// a struggling/rate-limited GitHub doesn't get hammered by every team
    /// dashboard load.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
