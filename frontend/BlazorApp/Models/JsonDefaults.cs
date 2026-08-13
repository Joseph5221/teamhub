using System.Text.Json;

namespace BlazorApp.Models;

/// <summary>
/// The API serializes with ASP.NET Core's web defaults (camelCase, case-insensitive) —
/// match that here so client-side (de)serialization doesn't depend on System.Text.Json's
/// case-sensitive, PascalCase-expecting defaults.
/// </summary>
internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
