namespace ASMILE.BO.Application.DTOs;

public sealed class DatabaseConnectionConfigurationDto
{
    public string Server { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string AuthenticationMode { get; init; } = "Windows";
    public string UserName { get; init; } = string.Empty;
    public bool HasPassword { get; init; }
}

public sealed record DatabaseConnectionConfigurationRequest(
    string Server,
    string Database,
    string AuthenticationMode,
    string UserName,
    string Password);

public sealed record DatabaseConnectionTestResult(bool Succeeded, string Message);
