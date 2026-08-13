namespace BlazorApp.Models;

public record ProblemResponse(string? Title, string? Detail);

public class ApiResult<T>
{
    public bool Success { get; private init; }
    public T? Data { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResult<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
}
