namespace TeamHub.Server.Domain.Common;

/// <summary>
/// Represents an error in the application
/// </summary>
public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    
    // Common errors
    public static Error NotFound(string entityName, Guid id) => 
        new("NotFound", $"{entityName} with ID {id} was not found");
    
    public static Error Validation(string message) => 
        new("Validation", message);
    
    public static Error Unauthorized(string message = "Unauthorized access") => 
        new("Unauthorized", message);
    
    public static Error Conflict(string message) => 
        new("Conflict", message);
}