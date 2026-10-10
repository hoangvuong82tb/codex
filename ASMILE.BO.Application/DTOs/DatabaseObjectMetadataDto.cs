namespace ASMILE.BO.Application.DTOs;

/// <summary>
/// Read-only metadata returned by the configured database provider.
/// </summary>
public sealed class DatabaseObjectMetadataDto
{
    public string Schema { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ObjectType { get; init; } = string.Empty;
}
