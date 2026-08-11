namespace TeamHub.Server.Modules.Integrations;

/// <summary>
/// Thrown by an <see cref="IModuleConnector"/> for expected, user-facing
/// failures (not configured, unsupported action, upstream API error) so
/// <see cref="IIntegrationService"/> can translate them into a
/// <c>Result</c> failure instead of a 500.
/// </summary>
public class ModuleConnectorException : Exception
{
    public string Code { get; }

    public ModuleConnectorException(string code, string message) : base(message)
    {
        Code = code;
    }
}
