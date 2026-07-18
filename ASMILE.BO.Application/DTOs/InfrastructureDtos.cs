namespace ASMILE.BO.Application.DTOs;

public sealed record InfrastructureHealthReportDto(
    string Status,
    DateTime CheckedAt,
    IReadOnlyList<InfrastructureHealthCheckDto> Checks);

public sealed record InfrastructureHealthCheckDto(
    string Name,
    string Status,
    string Message);

public sealed record MigrationStatusDto(
    bool CanConnect,
    IReadOnlyList<string> PendingMigrations,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyList<string> SeedWarnings);
